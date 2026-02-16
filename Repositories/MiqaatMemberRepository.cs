using BurhaniGuards.Api.BusinessModel;
using Dapper;
using System.Linq;

namespace BurhaniGuards.Api.Repositories;

public interface IMiqaatMemberRepository
{
    Task UpsertMembersForMiqaat(long miqaatId, string jamaat, AdminApprovalStatus status);
    Task<List<MiqaatModel>> GetMiqaatsByMemberId(int memberId);
    Task UpdateMemberMiqaatStatus(int memberId, long miqaatId, string status, IReadOnlyCollection<int>? days);
    Task<List<MemberPointsModel>> GetMemberPointsByJamaat(string jamaat);
    Task<MemberPointsModel> GetMemberPointsByMemberId(int memberId);
    Task<List<MemberPointsModel>> GetAllMemberPoints();
    Task<List<MemberModel>> GetEnrolledMembersByMiqaatId(long miqaatId);
    Task<List<(MemberModel Member, string StatusCategory)>> GetAllMembersByMiqaatId(long miqaatId);
    Task<List<MemberModel>> GetApprovedMembersForAttendance(long miqaatId, int day);
    Task<Dictionary<long, string?>> GetFinalStatusesByMiqaatId(long miqaatId);
    Task<Dictionary<long, bool>> GetAttendanceStatusesByMiqaatId(long miqaatId, int day);
    Task UpdateFinalStatus(int memberId, long miqaatId, string finalStatus);
    Task MarkAttendanceBatch(long miqaatId, int day, List<int> memberIds);
    Task<(MemberModel Member, List<MiqaatModel> Items, int TotalPoints)> GetMemberAttendanceHistory(int memberId);
    Task<List<MemberEnrollmentDayModel>> GetMemberEnrollmentDays(long miqaatId, int memberId);
}

public class MiqaatMemberRepository : IMiqaatMemberRepository
{
    private readonly DapperContext _context;

    public MiqaatMemberRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task UpsertMembersForMiqaat(long miqaatId, string jamaat, AdminApprovalStatus status)
    {
        using var connection = _context.CreateConnection();

        // Determine miqaat duration (inclusive) so we can create one row per day
        const string miqaatDaysSql = """
            SELECT IFNULL(`miqaat_days`, DATEDIFF(`till_date`, `from_date`) + 1) AS MiqaatDays
            FROM `local_miqaat`
            WHERE `id` = @MiqaatId
        """;

        var miqaatDays = await connection.QueryFirstOrDefaultAsync<int>(miqaatDaysSql, new { MiqaatId = miqaatId });
        if (miqaatDays < 1)
        {
            miqaatDays = 1;
        }

        const string memberQuery = """
            SELECT id 
            FROM `members` 
            WHERE `jamaat` = @Jamaat AND `is_active` = 1
        """;

        var memberIds = await connection.QueryAsync<int>(memberQuery, new { Jamaat = jamaat });
        if (!memberIds.Any())
        {
            return;
        }

        const string insertSql = """
            INSERT INTO `miqaat_members` (`member_id`, `miqaat_id`, `miqaat_day`, `status`)
            VALUES (@MemberId, @MiqaatId, @Day, @Status)
            ON DUPLICATE KEY UPDATE `status` = VALUES(`status`);
        """;

        var parameters = memberIds
            .SelectMany(id => Enumerable.Range(1, miqaatDays).Select(day => new
            {
                MemberId = id,
                MiqaatId = miqaatId,
                Day = day,
                Status = status.ToString()
            }));

        await connection.ExecuteAsync(insertSql, parameters);
    }

    public async Task<List<MiqaatModel>> GetMiqaatsByMemberId(int memberId)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT 
                m.`id` AS Id,
                m.`miqaat_name` AS MiqaatName,
                m.`miqaat_type` AS MiqaatType,
                m.`jamaat` AS Jamaat,
                m.`jamiyat` AS Jamiyat,
                m.`from_date` AS FromDate,
                m.`till_date` AS TillDate,
                IFNULL(m.`miqaat_days`, DATEDIFF(m.`till_date`, m.`from_date`) + 1) AS MiqaatDays,
                m.`volunteer_limit` AS VolunteerLimit,
                m.`about_miqaat` AS AboutMiqaat,
                m.`admin_approval` AS AdminApproval,
                m.`captain_name` AS CaptainName,
                m.`created_at` AS CreatedAt,
                m.`updated_at` AS UpdatedAt,
                mm.`MemberStatus` AS MemberStatus,
                mm.`FinalStatus` AS FinalStatus
            FROM `local_miqaat` m
            INNER JOIN (
                SELECT 
                    `miqaat_id`,
                    CASE 
                        WHEN SUM(CASE WHEN `status` = 'Approved' THEN 1 ELSE 0 END) > 0 THEN 'Approved'
                        WHEN SUM(CASE WHEN `status` = 'Rejected' THEN 1 ELSE 0 END) > 0 THEN 'Rejected'
                        ELSE 'Pending'
                    END AS `MemberStatus`,
                    CASE 
                        WHEN SUM(CASE WHEN `final_status` = 'Approved' THEN 1 ELSE 0 END) > 0 THEN 'Approved'
                        WHEN SUM(CASE WHEN `final_status` = 'Rejected' THEN 1 ELSE 0 END) > 0 THEN 'Rejected'
                        ELSE NULL
                    END AS `FinalStatus`
                FROM `miqaat_members`
                WHERE `member_id` = @MemberId
                GROUP BY `miqaat_id`
            ) mm ON m.`id` = mm.`miqaat_id`
            WHERE m.`admin_approval` = 'Approved'
            ORDER BY m.`created_at` DESC
        """;

        var miqaats = await connection.QueryAsync<MiqaatModel>(sql, new { MemberId = memberId });
        return miqaats.ToList();
    }

    public async Task UpdateMemberMiqaatStatus(int memberId, long miqaatId, string status, IReadOnlyCollection<int>? days)
    {
        using var connection = _context.CreateConnection();

        var updateSql = """
            UPDATE `miqaat_members`
            SET `status` = @Status
            WHERE `member_id` = @MemberId AND `miqaat_id` = @MiqaatId
        """;

        if (days != null && days.Count > 0)
        {
            updateSql += " AND `miqaat_day` IN @Days";
        }

        if (status != "Pending")
        {
            updateSql += " AND `status` = 'Pending'";
        }

        var rowsAffected = await connection.ExecuteAsync(updateSql, new 
        { 
            MemberId = memberId, 
            MiqaatId = miqaatId, 
            Status = status,
            Days = days
        });

        if (rowsAffected == 0)
        {
            throw new Exception("Miqaat member record not found");
        }
    }

    public async Task<List<MemberPointsModel>> GetMemberPointsByJamaat(string jamaat)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT
                m.`id` AS Id,
                m.`full_name` AS FullName,
                m.`its_id` AS ItsId,
                IFNULL(SUM(CASE 
                    WHEN lm.`admin_approval` = 'Approved'
                     AND mm.`status` = 'Approved'
                     AND mm.`final_status` = 'Approved'
                    THEN IFNULL(mm.`points`, 0)
                    ELSE 0
                END), 0) AS TotalPoints
            FROM `members` m
            LEFT JOIN `miqaat_members` mm ON m.`id` = mm.`member_id`
            LEFT JOIN `local_miqaat` lm ON lm.`id` = mm.`miqaat_id`
            WHERE m.`jamaat` = @Jamaat
                AND m.`is_active` = 1
            GROUP BY m.`id`, m.`full_name`, m.`its_id`
            ORDER BY m.`full_name` ASC
        """;

        var result = await connection.QueryAsync<MemberPointsModel>(sql, new { Jamaat = jamaat });
        return result.ToList();
    }

    public async Task<MemberPointsModel> GetMemberPointsByMemberId(int memberId)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT
                m.`id` AS Id,
                m.`full_name` AS FullName,
                m.`its_id` AS ItsId,
                IFNULL(SUM(CASE 
                    WHEN lm.`admin_approval` = 'Approved'
                     AND mm.`status` = 'Approved'
                     AND mm.`final_status` = 'Approved'
                    THEN IFNULL(mm.`points`, 0)
                    ELSE 0
                END), 0) AS TotalPoints
            FROM `members` m
            LEFT JOIN `miqaat_members` mm ON m.`id` = mm.`member_id`
            LEFT JOIN `local_miqaat` lm ON lm.`id` = mm.`miqaat_id`
            WHERE m.`id` = @MemberId
                AND m.`is_active` = 1
            GROUP BY m.`id`, m.`full_name`, m.`its_id`
            LIMIT 1
        """;

        var member = await connection.QueryFirstOrDefaultAsync<MemberPointsModel>(sql, new { MemberId = memberId });
        if (member == null)
        {
            throw new Exception("Member not found");
        }

        return member;
    }

    public async Task<List<MemberPointsModel>> GetAllMemberPoints()
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT
                m.`id` AS Id,
                m.`full_name` AS FullName,
                m.`its_id` AS ItsId,
                m.`jamaat` AS Jamaat,
                IFNULL(SUM(CASE 
                    WHEN lm.`admin_approval` = 'Approved'
                     AND mm.`status` = 'Approved'
                     AND mm.`final_status` = 'Approved'
                    THEN IFNULL(mm.`points`, 0)
                    ELSE 0
                END), 0) AS TotalPoints
            FROM `members` m
            LEFT JOIN `miqaat_members` mm ON m.`id` = mm.`member_id`
            LEFT JOIN `local_miqaat` lm ON lm.`id` = mm.`miqaat_id`
            WHERE m.`is_active` = 1
            GROUP BY m.`id`, m.`full_name`, m.`its_id`, m.`jamaat`
            ORDER BY TotalPoints DESC, m.`full_name` ASC
        """;

        var result = await connection.QueryAsync<MemberPointsModel>(sql);
        return result.ToList();
    }

    public async Task<List<MemberModel>> GetEnrolledMembersByMiqaatId(long miqaatId)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT 
                m.`id` AS Id,
                m.`profile` AS Profile,
                m.`its_id` AS ItsId,
                m.`rank` AS `Rank`,
                m.`roles` AS Roles,
                m.`jamiyat` AS Jamiyat,
                m.`jamaat` AS Jamaat,
                m.`jamiyat_id` AS JamiyatId,
                m.`jamaat_id` AS JamaatId,
                m.`full_name` AS FullName,
                m.`gender` AS Gender,
                m.`email` AS Email,
                m.`age` AS Age,
                m.`contact` AS Contact,
                m.`is_active` AS IsActive,
                m.`created_at` AS CreatedAt,
                m.`updated_at` AS UpdatedAt
            FROM `members` m
            INNER JOIN `miqaat_members` mm ON m.`id` = mm.`member_id`
            WHERE mm.`miqaat_id` = @MiqaatId 
                AND mm.`status` = 'Approved'
                AND m.`is_active` = 1
            ORDER BY m.`full_name` ASC
        """;

        var result = await connection.QueryAsync(sql, new { MiqaatId = miqaatId });
        var members = new List<MemberModel>();
        
        // Deduplicate because miqaat_members can have multiple rows per member (one per day)
        var seen = new HashSet<long>();
        foreach (var row in result)
        {
            var id = (long)row.Id;
            if (!seen.Add(id))
            {
                continue;
            }

            var member = new MemberModel
            {
                Id = id,
                Profile = row.Profile as string,
                ItsId = row.ItsId as string ?? string.Empty,
                Rank = row.Rank as string ?? string.Empty,
                Roles = row.Roles as int?,
                Jamiyat = row.Jamiyat as string,
                Jamaat = row.Jamaat as string,
                JamiyatId = row.JamiyatId as int?,
                JamaatId = row.JamaatId as int?,
                FullName = row.FullName as string ?? string.Empty,
                Gender = row.Gender as string,
                Email = row.Email as string ?? string.Empty,
                Age = row.Age as int?,
                Contact = row.Contact as string,
                IsActive = row.IsActive as bool? ?? true,
                CreatedAt = row.CreatedAt as DateTime? ?? DateTime.UtcNow,
                UpdatedAt = row.UpdatedAt as DateTime? ?? DateTime.UtcNow
            };
            members.Add(member);
        }
        
        return members;
    }

    public async Task<List<(MemberModel Member, string StatusCategory)>> GetAllMembersByMiqaatId(long miqaatId)
    {
        using var connection = _context.CreateConnection();

        // Query to get all members with their aggregated status for this miqaat
        const string sql = """
            SELECT 
                m.`id` AS Id,
                m.`profile` AS Profile,
                m.`its_id` AS ItsId,
                m.`rank` AS `Rank`,
                m.`roles` AS Roles,
                m.`jamiyat` AS Jamiyat,
                m.`jamaat` AS Jamaat,
                m.`jamiyat_id` AS JamiyatId,
                m.`jamaat_id` AS JamaatId,
                m.`full_name` AS FullName,
                m.`gender` AS Gender,
                m.`email` AS Email,
                m.`age` AS Age,
                m.`contact` AS Contact,
                m.`is_active` AS IsActive,
                m.`created_at` AS CreatedAt,
                m.`updated_at` AS UpdatedAt,
                SUM(CASE WHEN mm.`status` = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
                SUM(CASE WHEN mm.`status` = 'Rejected' THEN 1 ELSE 0 END) AS RejectedCount,
                SUM(CASE WHEN mm.`status` = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,
                COUNT(*) AS TotalDays
            FROM `members` m
            INNER JOIN `miqaat_members` mm ON m.`id` = mm.`member_id`
            WHERE mm.`miqaat_id` = @MiqaatId 
                AND m.`is_active` = 1
            GROUP BY m.`id`, m.`profile`, m.`its_id`, m.`rank`, m.`roles`, m.`jamiyat`, m.`jamaat`, 
                     m.`jamiyat_id`, m.`jamaat_id`, m.`full_name`, m.`gender`, m.`email`, m.`age`, 
                     m.`contact`, m.`is_active`, m.`created_at`, m.`updated_at`
            ORDER BY m.`full_name` ASC
        """;

        var result = await connection.QueryAsync(sql, new { MiqaatId = miqaatId });
        var members = new List<(MemberModel Member, string StatusCategory)>();
        
        foreach (var row in result)
        {
            var member = new MemberModel
            {
                Id = (long)row.Id,
                Profile = row.Profile as string,
                ItsId = row.ItsId as string ?? string.Empty,
                Rank = row.Rank as string ?? string.Empty,
                Roles = row.Roles as int?,
                Jamiyat = row.Jamiyat as string,
                Jamaat = row.Jamaat as string,
                JamiyatId = row.JamiyatId as int?,
                JamaatId = row.JamaatId as int?,
                FullName = row.FullName as string ?? string.Empty,
                Gender = row.Gender as string,
                Email = row.Email as string ?? string.Empty,
                Age = row.Age as int?,
                Contact = row.Contact as string,
                IsActive = row.IsActive as bool? ?? true,
                CreatedAt = row.CreatedAt as DateTime? ?? DateTime.UtcNow,
                UpdatedAt = row.UpdatedAt as DateTime? ?? DateTime.UtcNow
            };

            // Determine status category
            var approvedCount = Convert.ToInt32(row.ApprovedCount);
            var rejectedCount = Convert.ToInt32(row.RejectedCount);
            var pendingCount = Convert.ToInt32(row.PendingCount);

            string statusCategory;
            if (approvedCount > 0)
            {
                statusCategory = "Enrolled";
            }
            else if (rejectedCount > 0)
            {
                statusCategory = "Rejected";
            }
            else
            {
                statusCategory = "Pending";
            }

            members.Add((member, statusCategory));
        }
        
        return members;
    }

    public async Task<List<MemberModel>> GetApprovedMembersForAttendance(long miqaatId, int day)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT 
                m.`id` AS Id,
                m.`profile` AS Profile,
                m.`its_id` AS ItsId,
                m.`rank` AS `Rank`,
                m.`roles` AS Roles,
                m.`jamiyat` AS Jamiyat,
                m.`jamaat` AS Jamaat,
                m.`jamiyat_id` AS JamiyatId,
                m.`jamaat_id` AS JamaatId,
                m.`full_name` AS FullName,
                m.`gender` AS Gender,
                m.`email` AS Email,
                m.`age` AS Age,
                m.`contact` AS Contact,
                m.`is_active` AS IsActive,
                m.`created_at` AS CreatedAt,
                m.`updated_at` AS UpdatedAt
            FROM `members` m
            INNER JOIN `miqaat_members` mm ON m.`id` = mm.`member_id`
            WHERE mm.`miqaat_id` = @MiqaatId 
                AND mm.`miqaat_day` = @Day
                AND mm.`status` = 'Approved'
                AND mm.`final_status` = 'Approved'
                AND m.`is_active` = 1
            ORDER BY m.`full_name` ASC
        """;

        var result = await connection.QueryAsync(sql, new { MiqaatId = miqaatId, Day = day });
        var members = new List<MemberModel>();
        
        foreach (var row in result)
        {
            var member = new MemberModel
            {
                Id = (long)row.Id,
                Profile = row.Profile as string,
                ItsId = row.ItsId as string ?? string.Empty,
                Rank = row.Rank as string ?? string.Empty,
                Roles = row.Roles as int?,
                Jamiyat = row.Jamiyat as string,
                Jamaat = row.Jamaat as string,
                JamiyatId = row.JamiyatId as int?,
                JamaatId = row.JamaatId as int?,
                FullName = row.FullName as string ?? string.Empty,
                Gender = row.Gender as string,
                Email = row.Email as string ?? string.Empty,
                Age = row.Age as int?,
                Contact = row.Contact as string,
                IsActive = row.IsActive as bool? ?? true,
                CreatedAt = row.CreatedAt as DateTime? ?? DateTime.UtcNow,
                UpdatedAt = row.UpdatedAt as DateTime? ?? DateTime.UtcNow
            };
            members.Add(member);
        }
        
        return members;
    }
    
    public async Task<Dictionary<long, string?>> GetFinalStatusesByMiqaatId(long miqaatId)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT `member_id`, `final_status`
            FROM `miqaat_members`
            WHERE `miqaat_id` = @MiqaatId
                AND `miqaat_day` = 1
        """;

        var result = await connection.QueryAsync(sql, new { MiqaatId = miqaatId });
        return result.ToDictionary(r => (long)r.member_id, r => r.final_status as string);
    }

    public async Task<Dictionary<long, bool>> GetAttendanceStatusesByMiqaatId(long miqaatId, int day)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT `member_id`, `is_attended`
            FROM `miqaat_members`
            WHERE `miqaat_id` = @MiqaatId
                AND `miqaat_day` = @Day
        """;

        var result = await connection.QueryAsync(sql, new { MiqaatId = miqaatId, Day = day });
        var attendanceDict = new Dictionary<long, bool>();
        
        foreach (var row in result)
        {
            var memberId = (long)row.member_id;
            var isAttended = false;
            
            if (row.is_attended != null)
            {
                // Handle different possible types (bool, int, byte, etc.)
                if (row.is_attended is bool boolValue)
                {
                    isAttended = boolValue;
                }
                else if (row.is_attended is int intValue)
                {
                    isAttended = intValue != 0;
                }
                else if (row.is_attended is byte byteValue)
                {
                    isAttended = byteValue != 0;
                }
                else
                {
                    isAttended = Convert.ToBoolean(row.is_attended);
                }
            }
            
            attendanceDict[memberId] = isAttended;
        }
        
        return attendanceDict;
    }

    public async Task MarkAttendanceBatch(long miqaatId, int day, List<int> memberIds)
    {
        using var connection = _context.CreateConnection();

        const string updateSql = """
            UPDATE `miqaat_members`
            INNER JOIN `local_miqaat` lm ON lm.`id` = `miqaat_members`.`miqaat_id`
            SET 
                `miqaat_members`.`is_attended` = 1,
                `miqaat_members`.`points` = 2
            WHERE `miqaat_id` = @MiqaatId 
                AND `miqaat_day` = @Day
                AND `member_id` IN @MemberIds
                AND IFNULL(`is_attended`, 0) = 0
        """;

        var rowsAffected = await connection.ExecuteAsync(updateSql, new 
        { 
            MiqaatId = miqaatId,
            Day = day,
            MemberIds = memberIds
        });

        if (rowsAffected == 0)
        {
            throw new Exception("No miqaat member records found to update");
        }
    }

    public async Task UpdateFinalStatus(int memberId, long miqaatId, string finalStatus)
    {
        using var connection = _context.CreateConnection();

        // Use parameterized query that works across database types
        var updateSql = @"
            UPDATE miqaat_members
            SET final_status = @FinalStatus
            WHERE member_id = @MemberId AND miqaat_id = @MiqaatId
        ";

        var rowsAffected = await connection.ExecuteAsync(updateSql, new 
        { 
            MemberId = memberId, 
            MiqaatId = miqaatId, 
            FinalStatus = finalStatus 
        });

        if (rowsAffected == 0)
        {
            throw new Exception("Miqaat member record not found");
        }
    }

    public async Task<(MemberModel Member, List<MiqaatModel> Items, int TotalPoints)> GetMemberAttendanceHistory(int memberId)
    {
        using var connection = _context.CreateConnection();

        // Basic member info
        const string memberSql = """
            SELECT 
                `id` AS Id,
                `its_id` AS ItsId,
                `full_name` AS FullName,
                `email` AS Email,
                `contact` AS Contact,
                `rank` AS `Rank`,
                `jamaat` AS Jamaat,
                `jamiyat` AS Jamiyat
            FROM `members`
            WHERE `id` = @MemberId
            LIMIT 1
        """;

        var member = await connection.QueryFirstOrDefaultAsync<MemberModel>(memberSql, new { MemberId = memberId });
        if (member == null)
        {
            throw new Exception("Member not found");
        }

        // Attendance history: approved miqaats (including not-attended)
        const string historySql = """
            SELECT
                m.`id` AS Id,
                m.`miqaat_name` AS MiqaatName,
                m.`miqaat_type` AS MiqaatType,
                m.`jamaat` AS Jamaat,
                m.`jamiyat` AS Jamiyat,
                m.`from_date` AS FromDate,
                m.`till_date` AS TillDate,
                IFNULL(m.`miqaat_days`, DATEDIFF(m.`till_date`, m.`from_date`) + 1) AS MiqaatDays,
                m.`captain_name` AS CaptainName,
                m.`admin_approval` AS AdminApproval,
                m.`created_at` AS CreatedAt,
                m.`updated_at` AS UpdatedAt,
                mm.`miqaat_day` AS MiqaatDay,
                mm.`is_attended` AS IsAttended,
                IFNULL(mm.`points`, 0) AS Points,
                mm.`status` AS MemberStatus,
                mm.`final_status` AS FinalStatus
            FROM `miqaat_members` mm
            INNER JOIN `local_miqaat` m ON m.`id` = mm.`miqaat_id`
            WHERE mm.`member_id` = @MemberId
                AND m.`admin_approval` = 'Approved'
                AND mm.`status` = 'Approved'
                AND mm.`final_status` = 'Approved'
            ORDER BY m.`from_date` DESC, mm.`miqaat_day` ASC
        """;

        var items = (await connection.QueryAsync<MiqaatModel>(historySql, new { MemberId = memberId })).ToList();

        const string totalSql = """
            SELECT IFNULL(SUM(IFNULL(`points`, 0)), 0)
            FROM `miqaat_members` mm
            INNER JOIN `local_miqaat` m ON m.`id` = mm.`miqaat_id`
            WHERE mm.`member_id` = @MemberId
                AND m.`admin_approval` = 'Approved'
                AND mm.`status` = 'Approved'
                AND mm.`final_status` = 'Approved'
        """;

        var totalPoints = await connection.QueryFirstOrDefaultAsync<int>(totalSql, new { MemberId = memberId });

        return (member, items, totalPoints);
    }

    public async Task<List<MemberEnrollmentDayModel>> GetMemberEnrollmentDays(long miqaatId, int memberId)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT 
                mm.`miqaat_day` AS MiqaatDay,
                mm.`status` AS Status,
                mm.`final_status` AS FinalStatus,
                DATE_ADD(m.`from_date`, INTERVAL (mm.`miqaat_day` - 1) DAY) AS MiqaatDate
            FROM `miqaat_members` mm
            INNER JOIN `local_miqaat` m ON m.`id` = mm.`miqaat_id`
            WHERE mm.`miqaat_id` = @MiqaatId 
                AND mm.`member_id` = @MemberId
            ORDER BY mm.`miqaat_day` ASC
        """;

        var result = await connection.QueryAsync<MemberEnrollmentDayModel>(sql, new { MiqaatId = miqaatId, MemberId = memberId });
        return result.ToList();
    }
}

