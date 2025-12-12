using BurhaniGuards.Api.BusinessModel;
using Dapper;
using System.Linq;

namespace BurhaniGuards.Api.Repositories;

public interface IMiqaatMemberRepository
{
    Task UpsertMembersForMiqaat(long miqaatId, string jamaat, AdminApprovalStatus status);
    Task<List<MiqaatModel>> GetMiqaatsByMemberId(int memberId);
    Task UpdateMemberMiqaatStatus(int memberId, long miqaatId, string status);
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
                m.`volunteer_limit` AS VolunteerLimit,
                m.`about_miqaat` AS AboutMiqaat,
                m.`admin_approval` AS AdminApproval,
                m.`captain_name` AS CaptainName,
                m.`created_at` AS CreatedAt,
                m.`updated_at` AS UpdatedAt
            FROM `local_miqaat` m
            INNER JOIN `miqaat_members` mm ON m.`id` = mm.`miqaat_id`
            WHERE mm.`member_id` = @MemberId
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
}

