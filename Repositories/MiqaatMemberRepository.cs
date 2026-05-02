using BurhaniGuards.Api.BusinessModel;
using Dapper;
using System.Linq;

namespace BurhaniGuards.Api.Repositories;

public interface IMiqaatMemberRepository
{
    Task UpsertMembersForMiqaat(long miqaatId, string jamaat, AdminApprovalStatus status);
    Task UpsertCaptainForMiqaat(int captainMemberId, long miqaatId, AdminApprovalStatus status);
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
    Task UpdateFinalStatus(int memberId, long miqaatId, string finalStatus, IReadOnlyCollection<int>? days);
    Task UpdateAdminStatus(int memberId, long miqaatId, string adminStatus, IReadOnlyCollection<int>? days);
    Task<List<MemberModel>> GetCaptainApprovedMembersForIntlMiqaat(long miqaatId, int? day = null);
    Task MarkAttendanceBatch(long miqaatId, int day, List<int> memberIds);
    Task<(MemberModel Member, List<MiqaatModel> Items, int TotalPoints)> GetMemberAttendanceHistory(int memberId);
    Task<List<MemberEnrollmentDayModel>> GetMemberEnrollmentDays(long miqaatId, int memberId);
    Task<(
        int TotalMiqaats, int ApprovedMiqaats, int PendingMiqaats, int RejectedMiqaats,
        int TotalEnrolled, int TotalAttended,
        int TotalLocal, int TotalInternational, int ReportsSubmitted
    )> GetMiqaatOverallStatsAsync();
    Task<List<(long Id, string MiqaatName, string MiqaatType, string Jamaat, string Jamiyat,
        DateTime FromDate, DateTime TillDate, int MiqaatDays, int VolunteerLimit,
        string AdminApproval, string CaptainName, bool IsReportSubmitted,
        int TotalEnrolled, int TotalApproved, int TotalAttended, int TotalPending, int TotalRejected)>> GetMiqaatDetailStatsAsync();
    Task<List<(string Jamaat, int TotalMiqaats, int ApprovedMiqaats, int TotalEnrolled, int TotalAttended)>> GetJamaatInsightsAsync();
    Task<List<(string MonthLabel, int Count)>> GetMonthlyTrendAsync();

    /// <summary>
    /// Returns every miqaat_members row for a specific miqaat joined with member info.
    /// Used to build day-wise stats and member-day-matrix.
    /// </summary>
    Task<List<(
        long MemberId, string FullName, string ItsId, string Rank, string Jamaat, string Contact,
        int Day, string Status, string? FinalStatus, string? AdminStatus, bool IsAttended
    )>> GetAllMemberDayRowsForMiqaatAsync(long miqaatId);
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

        var jamaatList = jamaat.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(j => j.Trim())
                               .ToList();

        const string memberQuery = """
            SELECT id 
            FROM `members` 
            WHERE `jamaat` IN @JamaatList AND `is_active` = 1
        """;

        var memberIds = await connection.QueryAsync<int>(memberQuery, new { JamaatList = jamaatList });
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

    public async Task UpsertCaptainForMiqaat(int captainMemberId, long miqaatId, AdminApprovalStatus status)
    {
        using var connection = _context.CreateConnection();

        // Get the miqaat duration so we can create one row per day for the captain
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

        // INSERT ... ON DUPLICATE KEY UPDATE so existing rows get their status promoted to Approved,
        // and missing rows (captain added after initial seed) are inserted fresh.
        const string upsertSql = """
            INSERT INTO `miqaat_members` (`member_id`, `miqaat_id`, `miqaat_day`, `status`)
            VALUES (@MemberId, @MiqaatId, @Day, @Status)
            ON DUPLICATE KEY UPDATE `status` = VALUES(`status`);
        """;

        var parameters = Enumerable.Range(1, miqaatDays).Select(day => new
        {
            MemberId = captainMemberId,
            MiqaatId = miqaatId,
            Day = day,
            Status = status.ToString()
        });

        await connection.ExecuteAsync(upsertSql, parameters);
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
                m.`is_admin_created` AS IsAdminCreated,
                m.`created_at` AS CreatedAt,
                m.`updated_at` AS UpdatedAt,
                mm.`MemberStatus` AS MemberStatus,
                mm.`FinalStatus` AS FinalStatus,
                mm.`AdminStatus` AS AdminStatus
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
                    END AS `FinalStatus`,
                    CASE 
                        WHEN SUM(CASE WHEN `admin_status` = 'Approved' THEN 1 ELSE 0 END) > 0 THEN 'Approved'
                        WHEN SUM(CASE WHEN `admin_status` = 'Rejected' THEN 1 ELSE 0 END) > 0 THEN 'Rejected'
                        ELSE NULL
                    END AS `AdminStatus`
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

        // Allow re-enrollment: members can change status unless captain has ACTUALLY finalized that day
        // (finalized = captain set final_status to 'Approved' or 'Rejected')
        // Rows seeded by UpsertMembersForMiqaat have final_status = 'Pending', which is NOT truly finalized.
        var updateSql = """
            UPDATE `miqaat_members`
            SET `status` = @Status
            WHERE `member_id` = @MemberId AND `miqaat_id` = @MiqaatId
                AND (`final_status` IS NULL OR `final_status` = '' OR `final_status` = 'Pending')
        """;

        if (days != null && days.Count > 0)
        {
            updateSql += " AND `miqaat_day` IN @Days";
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
            throw new Exception("No eligible days found to update. Captain may have already finalized.");
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
                     AND ((lm.`miqaat_type` != 'International' AND lm.`is_admin_created` = 0) OR mm.`admin_status` = 'Approved')
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
                     AND ((lm.`miqaat_type` != 'International' AND lm.`is_admin_created` = 0) OR mm.`admin_status` = 'Approved')
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
                     AND ((lm.`miqaat_type` != 'International' AND lm.`is_admin_created` = 0) OR mm.`admin_status` = 'Approved')
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
                lm.`miqaat_type` AS MiqaatType,
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
            INNER JOIN `local_miqaat` lm ON lm.`id` = mm.`miqaat_id`
            WHERE mm.`miqaat_id` = @MiqaatId 
                AND mm.`miqaat_day` = @Day
                AND mm.`status` = 'Approved'
                AND mm.`final_status` = 'Approved'
                AND (
                    (lm.`miqaat_type` != 'International' AND lm.`is_admin_created` = 0)
                    OR mm.`admin_status` = 'Approved'
                )
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

    public async Task UpdateFinalStatus(int memberId, long miqaatId, string finalStatus, IReadOnlyCollection<int>? days)
    {
        using var connection = _context.CreateConnection();

        // Support day-wise captain approval
        // For miqaats created after Feb 19, 2026: Captain can only approve/reject after member has enrolled (status = 'Approved')
        // For existing/ongoing miqaats (from_date <= '2026-02-19'): Keep old behavior (no status restriction)
        var updateSql = @"
            UPDATE miqaat_members mm
            INNER JOIN local_miqaat lm ON lm.id = mm.miqaat_id
            SET mm.final_status = @FinalStatus
            WHERE mm.member_id = @MemberId AND mm.miqaat_id = @MiqaatId
                AND (lm.from_date <= '2026-02-19' OR mm.status = 'Approved')
        ";

        if (days != null && days.Count > 0)
        {
            updateSql += " AND mm.miqaat_day IN @Days";
        }

        var rowsAffected = await connection.ExecuteAsync(updateSql, new 
        { 
            MemberId = memberId, 
            MiqaatId = miqaatId, 
            FinalStatus = finalStatus,
            Days = days
        });

        if (rowsAffected == 0)
        {
            throw new Exception("No eligible days found. Member may not have enrolled yet, or record not found.");
        }
    }

    public async Task UpdateAdminStatus(int memberId, long miqaatId, string adminStatus, IReadOnlyCollection<int>? days)
    {
        using var connection = _context.CreateConnection();

        // Admin can only approve/reject members whose final_status is already 'Approved' (captain approved)
        var updateSql = @"
            UPDATE miqaat_members mm
            INNER JOIN local_miqaat lm ON lm.id = mm.miqaat_id
            SET mm.admin_status = @AdminStatus
            WHERE mm.member_id = @MemberId AND mm.miqaat_id = @MiqaatId
                AND (lm.miqaat_type = 'International' OR lm.is_admin_created = 1)
                AND mm.final_status = 'Approved'
        ";

        if (days != null && days.Count > 0)
        {
            updateSql += " AND mm.miqaat_day IN @Days";
        }

        var rowsAffected = await connection.ExecuteAsync(updateSql, new 
        { 
            MemberId = memberId, 
            MiqaatId = miqaatId, 
            AdminStatus = adminStatus,
            Days = days
        });

        if (rowsAffected == 0)
        {
            throw new Exception("No eligible days found. Member may not have been captain-approved, or miqaat is not admin-managed.");
        }
    }

    public async Task<List<MemberModel>> GetCaptainApprovedMembersForIntlMiqaat(long miqaatId, int? day = null)
    {
        using var connection = _context.CreateConnection();

        // Build the query dynamically based on whether a day filter is provided
        var sql = $@"
            SELECT 
                m.`id` AS Id,
                m.`profile` AS Profile,
                m.`its_id` AS ItsId,
                m.`rank` AS `Rank`,
                m.`roles` AS Roles,
                m.`jamiyat` AS Jamiyat,
                m.`jamaat` AS Jamaat,
                m.`full_name` AS FullName,
                m.`gender` AS Gender,
                m.`email` AS Email,
                m.`age` AS Age,
                m.`contact` AS Contact,
                m.`is_active` AS IsActive,
                mm.`admin_status` AS AdminStatus,
                mm.`miqaat_day` AS MiqaatDay,
                mm.`is_attended` AS IsAttended
            FROM `members` m
            INNER JOIN `miqaat_members` mm ON m.`id` = mm.`member_id`
            INNER JOIN `local_miqaat` lm ON lm.`id` = mm.`miqaat_id`
            WHERE mm.`miqaat_id` = @MiqaatId
                AND (lm.`miqaat_type` = 'International' OR lm.`is_admin_created` = 1)
                AND mm.`status` = 'Approved'
                AND mm.`final_status` = 'Approved'
                AND m.`is_active` = 1
                {(day.HasValue ? "AND mm.`miqaat_day` = @Day" : "")}
            ORDER BY m.`full_name` ASC
        ";

        var result = await connection.QueryAsync(sql, new { MiqaatId = miqaatId, Day = day ?? 0 });
        var members = new List<MemberModel>();

        if (day.HasValue)
        {
            // Day-specific: return one entry per member for that day
            foreach (var row in result)
            {
                var isAttended = false;
                if (row.IsAttended != null)
                {
                    if (row.IsAttended is bool boolVal) isAttended = boolVal;
                    else if (row.IsAttended is int intVal) isAttended = intVal != 0;
                    else if (row.IsAttended is byte byteVal) isAttended = byteVal != 0;
                    else isAttended = Convert.ToBoolean(row.IsAttended);
                }

                members.Add(new MemberModel
                {
                    Id = (long)row.Id,
                    Profile = row.Profile as string,
                    ItsId = row.ItsId as string ?? string.Empty,
                    Rank = row.Rank as string ?? string.Empty,
                    Roles = row.Roles as int?,
                    Jamiyat = row.Jamiyat as string,
                    Jamaat = row.Jamaat as string,
                    FullName = row.FullName as string ?? string.Empty,
                    Gender = row.Gender as string,
                    Email = row.Email as string ?? string.Empty,
                    Age = row.Age as int?,
                    Contact = row.Contact as string,
                    IsActive = row.IsActive as bool? ?? true,
                    AdminStatus = row.AdminStatus as string,
                    IsAttended = isAttended
                });
            }
        }
        else
        {
            // No day filter: return distinct members (backward compatible)
            var seen = new HashSet<long>();
            foreach (var row in result)
            {
                var id = (long)row.Id;
                if (!seen.Add(id)) continue;

                members.Add(new MemberModel
                {
                    Id = id,
                    Profile = row.Profile as string,
                    ItsId = row.ItsId as string ?? string.Empty,
                    Rank = row.Rank as string ?? string.Empty,
                    Roles = row.Roles as int?,
                    Jamiyat = row.Jamiyat as string,
                    Jamaat = row.Jamaat as string,
                    FullName = row.FullName as string ?? string.Empty,
                    Gender = row.Gender as string,
                    Email = row.Email as string ?? string.Empty,
                    Age = row.Age as int?,
                    Contact = row.Contact as string,
                    IsActive = row.IsActive as bool? ?? true,
                    AdminStatus = row.AdminStatus as string
                });
            }
        }
        
        return members;
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
                mm.`final_status` AS FinalStatus,
                mm.`admin_status` AS AdminStatus
            FROM `miqaat_members` mm
            INNER JOIN `local_miqaat` m ON m.`id` = mm.`miqaat_id`
            WHERE mm.`member_id` = @MemberId
                AND m.`admin_approval` = 'Approved'
                AND mm.`status` = 'Approved'
                AND mm.`final_status` = 'Approved'
                AND (m.`miqaat_type` != 'International' OR mm.`admin_status` = 'Approved')
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
                AND (m.`miqaat_type` != 'International' OR mm.`admin_status` = 'Approved')
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
                mm.`admin_status` AS AdminStatus,
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

    public async Task<(
        int TotalMiqaats, int ApprovedMiqaats, int PendingMiqaats, int RejectedMiqaats,
        int TotalEnrolled, int TotalAttended,
        int TotalLocal, int TotalInternational, int ReportsSubmitted
    )> GetMiqaatOverallStatsAsync()
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT
                COUNT(*) AS TotalMiqaats,
                SUM(CASE WHEN `admin_approval` = 'Approved' THEN 1 ELSE 0 END) AS ApprovedMiqaats,
                SUM(CASE WHEN `admin_approval` = 'Pending' THEN 1 ELSE 0 END) AS PendingMiqaats,
                SUM(CASE WHEN `admin_approval` = 'Rejected' THEN 1 ELSE 0 END) AS RejectedMiqaats,
                SUM(CASE WHEN `miqaat_type` = 'Local' THEN 1 ELSE 0 END) AS TotalLocal,
                SUM(CASE WHEN `miqaat_type` = 'International' THEN 1 ELSE 0 END) AS TotalInternational,
                SUM(CASE WHEN `is_report_submitted` = 1 OR (`miqaat_image_1` IS NOT NULL AND `miqaat_image_1` != '') THEN 1 ELSE 0 END) AS ReportsSubmitted
            FROM `local_miqaat`
        """;

        const string memberSql = """
            SELECT
                COUNT(DISTINCT CONCAT(mm.`member_id`, '-', mm.`miqaat_id`)) AS TotalEnrolled,
                SUM(CASE WHEN mm.`is_attended` = 1 THEN 1 ELSE 0 END) AS TotalAttended
            FROM `miqaat_members` mm
            WHERE mm.`status` = 'Approved'
        """;

        var stats = await connection.QueryFirstOrDefaultAsync(sql);
        var memberStats = await connection.QueryFirstOrDefaultAsync(memberSql);

        int totalEnrolled = 0, totalAttended = 0;
        if (memberStats != null)
        {
            totalEnrolled = Convert.ToInt32(memberStats.TotalEnrolled ?? 0);
            totalAttended = Convert.ToInt32(memberStats.TotalAttended ?? 0);
        }

        if (stats == null)
            return (0, 0, 0, 0, totalEnrolled, totalAttended, 0, 0, 0);

        return (
            Convert.ToInt32(stats.TotalMiqaats ?? 0),
            Convert.ToInt32(stats.ApprovedMiqaats ?? 0),
            Convert.ToInt32(stats.PendingMiqaats ?? 0),
            Convert.ToInt32(stats.RejectedMiqaats ?? 0),
            totalEnrolled,
            totalAttended,
            Convert.ToInt32(stats.TotalLocal ?? 0),
            Convert.ToInt32(stats.TotalInternational ?? 0),
            Convert.ToInt32(stats.ReportsSubmitted ?? 0)
        );
    }

    public async Task<List<(long Id, string MiqaatName, string MiqaatType, string Jamaat, string Jamiyat,
        DateTime FromDate, DateTime TillDate, int MiqaatDays, int VolunteerLimit,
        string AdminApproval, string CaptainName, bool IsReportSubmitted,
        int TotalEnrolled, int TotalApproved, int TotalAttended, int TotalPending, int TotalRejected)>> GetMiqaatDetailStatsAsync()
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT
                m.`id` AS Id,
                m.`miqaat_name` AS MiqaatName,
                m.`miqaat_type` AS MiqaatType,
                IFNULL(m.`jamaat`, '') AS Jamaat,
                IFNULL(m.`jamiyat`, '') AS Jamiyat,
                m.`from_date` AS FromDate,
                m.`till_date` AS TillDate,
                IFNULL(m.`miqaat_days`, DATEDIFF(m.`till_date`, m.`from_date`) + 1) AS MiqaatDays,
                IFNULL(m.`volunteer_limit`, 0) AS VolunteerLimit,
                m.`admin_approval` AS AdminApproval,
                IFNULL(m.`captain_name`, '') AS CaptainName,
                CASE WHEN m.`is_report_submitted` = 1 OR (m.`miqaat_image_1` IS NOT NULL AND m.`miqaat_image_1` != '') THEN 1 ELSE 0 END AS IsReportSubmitted,
                COUNT(DISTINCT CASE WHEN mm.`status` IN ('Approved','Pending','Rejected') THEN mm.`member_id` END) AS TotalEnrolled,
                COUNT(DISTINCT CASE WHEN mm.`status` = 'Approved' THEN mm.`member_id` END) AS TotalApproved,
                SUM(CASE WHEN mm.`is_attended` = 1 THEN 1 ELSE 0 END) AS TotalAttended,
                COUNT(DISTINCT CASE WHEN mm.`status` = 'Pending' THEN mm.`member_id` END) AS TotalPending,
                COUNT(DISTINCT CASE WHEN mm.`status` = 'Rejected' THEN mm.`member_id` END) AS TotalRejected
            FROM `local_miqaat` m
            LEFT JOIN `miqaat_members` mm ON mm.`miqaat_id` = m.`id` AND mm.`miqaat_day` = 1
            GROUP BY m.`id`, m.`miqaat_name`, m.`miqaat_type`, m.`jamaat`, m.`jamiyat`,
                     m.`from_date`, m.`till_date`, m.`miqaat_days`, m.`volunteer_limit`,
                     m.`admin_approval`, m.`captain_name`, m.`is_report_submitted`, m.`miqaat_image_1`
            ORDER BY m.`created_at` DESC
        """;

        var rows = await connection.QueryAsync(sql);
        var list = new List<(long, string, string, string, string, DateTime, DateTime, int, int, string, string, bool, int, int, int, int, int)>();

        foreach (var r in rows)
        {
            list.Add((
                Convert.ToInt64(r.Id),
                r.MiqaatName as string ?? "",
                r.MiqaatType as string ?? "Local",
                r.Jamaat as string ?? "",
                r.Jamiyat as string ?? "",
                r.FromDate is DateTime fd ? fd : Convert.ToDateTime(r.FromDate),
                r.TillDate is DateTime td ? td : Convert.ToDateTime(r.TillDate),
                Convert.ToInt32(r.MiqaatDays ?? 1),
                Convert.ToInt32(r.VolunteerLimit ?? 0),
                r.AdminApproval as string ?? "Pending",
                r.CaptainName as string ?? "",
                Convert.ToInt32(r.IsReportSubmitted ?? 0) == 1,
                Convert.ToInt32(r.TotalEnrolled ?? 0),
                Convert.ToInt32(r.TotalApproved ?? 0),
                Convert.ToInt32(r.TotalAttended ?? 0),
                Convert.ToInt32(r.TotalPending ?? 0),
                Convert.ToInt32(r.TotalRejected ?? 0)
            ));
        }

        return list;
    }

    public async Task<List<(string Jamaat, int TotalMiqaats, int ApprovedMiqaats, int TotalEnrolled, int TotalAttended)>> GetJamaatInsightsAsync()
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT
                IFNULL(m.`jamaat`, 'International') AS Jamaat,
                COUNT(DISTINCT m.`id`) AS TotalMiqaats,
                COUNT(DISTINCT CASE WHEN m.`admin_approval` = 'Approved' THEN m.`id` END) AS ApprovedMiqaats,
                COUNT(DISTINCT CASE WHEN mm.`status` = 'Approved' THEN mm.`member_id` END) AS TotalEnrolled,
                SUM(CASE WHEN mm.`is_attended` = 1 THEN 1 ELSE 0 END) AS TotalAttended
            FROM `local_miqaat` m
            LEFT JOIN `miqaat_members` mm ON mm.`miqaat_id` = m.`id` AND mm.`miqaat_day` = 1
            WHERE m.`jamaat` IS NOT NULL AND m.`jamaat` != ''
            GROUP BY m.`jamaat`
            ORDER BY TotalMiqaats DESC
        """;

        var rows = await connection.QueryAsync(sql);
        var jamaatList = new List<(string Jamaat, int TotalMiqaats, int ApprovedMiqaats, int TotalEnrolled, int TotalAttended)>();
        foreach (var r in rows)
        {
            jamaatList.Add((
                r.Jamaat as string ?? "",
                Convert.ToInt32(r.TotalMiqaats ?? 0),
                Convert.ToInt32(r.ApprovedMiqaats ?? 0),
                Convert.ToInt32(r.TotalEnrolled ?? 0),
                Convert.ToInt32(r.TotalAttended ?? 0)
            ));
        }
        return jamaatList;
    }

    public async Task<List<(string MonthLabel, int Count)>> GetMonthlyTrendAsync()
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT
                DATE_FORMAT(`created_at`, '%b %Y') AS MonthLabel,
                MIN(`created_at`) AS MonthSort,
                COUNT(*) AS Count
            FROM `local_miqaat`
            GROUP BY DATE_FORMAT(`created_at`, '%b %Y')
            ORDER BY MonthSort ASC
            LIMIT 12
        """;

        var rows = await connection.QueryAsync(sql);
        var trendList = new List<(string MonthLabel, int Count)>();
        foreach (var r in rows)
        {
            trendList.Add((
                r.MonthLabel as string ?? "",
                Convert.ToInt32(r.Count ?? 0)
            ));
        }
        return trendList;
    }

    public async Task<List<(
        long MemberId, string FullName, string ItsId, string Rank, string Jamaat, string Contact,
        int Day, string Status, string? FinalStatus, string? AdminStatus, bool IsAttended
    )>> GetAllMemberDayRowsForMiqaatAsync(long miqaatId)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT
                m.`id`          AS MemberId,
                m.`full_name`   AS FullName,
                m.`its_id`      AS ItsId,
                IFNULL(m.`rank`, '')    AS `Rank`,
                IFNULL(m.`jamaat`, '') AS Jamaat,
                IFNULL(m.`contact`, '') AS Contact,
                mm.`miqaat_day` AS Day,
                mm.`status`     AS Status,
                mm.`final_status` AS FinalStatus,
                mm.`admin_status` AS AdminStatus,
                IFNULL(mm.`is_attended`, 0) AS IsAttended
            FROM `miqaat_members` mm
            INNER JOIN `members` m ON m.`id` = mm.`member_id`
            WHERE mm.`miqaat_id` = @MiqaatId
                AND m.`is_active` = 1
            ORDER BY m.`full_name` ASC, mm.`miqaat_day` ASC
        """;

        var rows = await connection.QueryAsync(sql, new { MiqaatId = miqaatId });
        var result = new List<(long, string, string, string, string, string, int, string, string?, string?, bool)>();

        foreach (var r in rows)
        {
            var isAttendedRaw = r.IsAttended;
            bool isAttended = false;
            if (isAttendedRaw is bool b) isAttended = b;
            else if (isAttendedRaw is int i) isAttended = i != 0;
            else if (isAttendedRaw is byte by) isAttended = by != 0;
            else if (isAttendedRaw != null) isAttended = Convert.ToBoolean(isAttendedRaw);

            result.Add((
                Convert.ToInt64(r.MemberId),
                r.FullName as string ?? "",
                r.ItsId as string ?? "",
                r.Rank as string ?? "",
                r.Jamaat as string ?? "",
                r.Contact as string ?? "",
                Convert.ToInt32(r.Day),
                r.Status as string ?? "Pending",
                r.FinalStatus as string,
                r.AdminStatus as string,
                isAttended
            ));
        }

        return result;
    }
}

