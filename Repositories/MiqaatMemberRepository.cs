using BurhaniGuards.Api.BusinessModel;
using Dapper;
using System.Linq;

namespace BurhaniGuards.Api.Repositories;

public interface IMiqaatMemberRepository
{
    Task UpsertMembersForMiqaat(long miqaatId, string jamaat, AdminApprovalStatus status);
    Task<List<MiqaatModel>> GetMiqaatsByMemberId(int memberId);
    Task UpdateMemberMiqaatStatus(int memberId, long miqaatId, string status);
    Task<List<MemberModel>> GetEnrolledMembersByMiqaatId(long miqaatId);
    Task<List<MemberModel>> GetApprovedMembersForAttendance(long miqaatId);
    Task<Dictionary<long, string?>> GetFinalStatusesByMiqaatId(long miqaatId);
    Task<Dictionary<long, bool>> GetAttendanceStatusesByMiqaatId(long miqaatId);
    Task UpdateFinalStatus(int memberId, long miqaatId, string finalStatus);
    Task MarkAttendanceBatch(long miqaatId, List<int> memberIds);
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
            INSERT INTO `miqaat_members` (`member_id`, `miqaat_id`, `status`)
            VALUES (@MemberId, @MiqaatId, @Status)
            ON DUPLICATE KEY UPDATE `status` = VALUES(`status`);
        """;

        var parameters = memberIds.Select(id => new
        {
            MemberId = id,
            MiqaatId = miqaatId,
            Status = status.ToString()
        });

        await connection.ExecuteAsync(insertSql, parameters);
    }

    public async Task<List<MiqaatModel>> GetMiqaatsByMemberId(int memberId)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT 
                m.`id` AS Id,
                m.`miqaat_name` AS MiqaatName,
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
                mm.`status` AS MemberStatus,
                mm.`final_status` AS FinalStatus
            FROM `local_miqaat` m
            INNER JOIN `miqaat_members` mm ON m.`id` = mm.`miqaat_id`
            WHERE mm.`member_id` = @MemberId
                AND m.`admin_approval` = 'Approved'
            ORDER BY m.`created_at` DESC
        """;

        var miqaats = await connection.QueryAsync<MiqaatModel>(sql, new { MemberId = memberId });
        return miqaats.ToList();
    }

    public async Task UpdateMemberMiqaatStatus(int memberId, long miqaatId, string status)
    {
        using var connection = _context.CreateConnection();

        const string updateSql = """
            UPDATE `miqaat_members`
            SET `status` = @Status
            WHERE `member_id` = @MemberId AND `miqaat_id` = @MiqaatId
        """;

        var rowsAffected = await connection.ExecuteAsync(updateSql, new 
        { 
            MemberId = memberId, 
            MiqaatId = miqaatId, 
            Status = status 
        });

        if (rowsAffected == 0)
        {
            throw new Exception("Miqaat member record not found");
        }
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

    public async Task<List<MemberModel>> GetApprovedMembersForAttendance(long miqaatId)
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
                AND mm.`final_status` = 'Approved'
                AND m.`is_active` = 1
            ORDER BY m.`full_name` ASC
        """;

        var result = await connection.QueryAsync(sql, new { MiqaatId = miqaatId });
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
        """;

        var result = await connection.QueryAsync(sql, new { MiqaatId = miqaatId });
        return result.ToDictionary(r => (long)r.member_id, r => r.final_status as string);
    }

    public async Task<Dictionary<long, bool>> GetAttendanceStatusesByMiqaatId(long miqaatId)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT `member_id`, `is_attended`
            FROM `miqaat_members`
            WHERE `miqaat_id` = @MiqaatId
        """;

        var result = await connection.QueryAsync(sql, new { MiqaatId = miqaatId });
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

    public async Task MarkAttendanceBatch(long miqaatId, List<int> memberIds)
    {
        using var connection = _context.CreateConnection();

        const string updateSql = """
            UPDATE `miqaat_members`
            SET `is_attended` = 1
            WHERE `miqaat_id` = @MiqaatId 
                AND `member_id` IN @MemberIds
        """;

        var rowsAffected = await connection.ExecuteAsync(updateSql, new 
        { 
            MiqaatId = miqaatId,
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
}

