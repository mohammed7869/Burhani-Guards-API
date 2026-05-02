using BurhaniGuards.Api.Contracts.Responses;
using Dapper;

namespace BurhaniGuards.Api.Repositories;

public class SurveyRepository : ISurveyRepository
{
    private readonly DapperContext _context;

    public SurveyRepository(DapperContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the current date/time in India Standard Time (IST = UTC+5:30)
    /// </summary>
    private static DateTime GetIstNow()
    {
        var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
    }

    public async Task<int> Create(int memberId, int department, int zone)
    {
        var sql = @"
            INSERT INTO survey (
                member_id, department, zone, created_at, updated_at
            ) VALUES (
                @MemberId, @Department, @Zone, @Now, @Now
            );
            SELECT LAST_INSERT_ID();
        ";

        using var connection = _context.CreateConnection();
        var id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            MemberId = memberId,
            Department = department,
            Zone = zone,
            Now = GetIstNow()
        });

        return id;
    }

    public async Task<bool> Update(int memberId, int department, int zone)
    {
        var sql = @"
            UPDATE survey SET 
                department = @Department, 
                zone = @Zone, 
                updated_at = @Now
            WHERE member_id = @MemberId
        ";

        using var connection = _context.CreateConnection();
        var rows = await connection.ExecuteAsync(sql, new
        {
            MemberId = memberId,
            Department = department,
            Zone = zone,
            Now = GetIstNow()
        });

        return rows > 0;
    }

    public async Task<SurveyResponse?> GetByMemberId(int memberId)
    {
        var sql = @"
            SELECT 
                s.id AS Id,
                s.member_id AS MemberId,
                m.its_id AS ItsId,
                m.full_name AS FullName,
                m.contact AS Contact,
                m.profile AS Profile,
                s.department AS Department,
                s.zone AS Zone,
                s.created_at AS CreatedAt,
                s.updated_at AS UpdatedAt
            FROM survey s
            INNER JOIN members m ON m.id = s.member_id
            WHERE s.member_id = @MemberId
        ";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<SurveyResponse>(sql, new { MemberId = memberId });
    }

    public async Task<List<SurveyResponse>> GetAll()
    {
        var sql = @"
            SELECT 
                s.id AS Id,
                s.member_id AS MemberId,
                m.its_id AS ItsId,
                m.full_name AS FullName,
                m.contact AS Contact,
                m.profile AS Profile,
                s.department AS Department,
                s.zone AS Zone,
                s.created_at AS CreatedAt,
                s.updated_at AS UpdatedAt
            FROM survey s
            INNER JOIN members m ON m.id = s.member_id
            ORDER BY s.created_at DESC
        ";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<SurveyResponse>(sql);
        return result.ToList();
    }

    public async Task<bool> HasSubmitted(int memberId)
    {
        var sql = @"SELECT COUNT(*) FROM survey WHERE member_id = @MemberId";
        using var connection = _context.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { MemberId = memberId });
        return count > 0;
    }
}
