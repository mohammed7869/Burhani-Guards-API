using BurhaniGuards.Api.BusinessModel;
using Dapper;

namespace BurhaniGuards.Api.Repositories;

public interface IActivityLogRepository
{
    Task AddAsync(ActivityLogModel log);
    Task AddBatchAsync(List<ActivityLogModel> logs);
    Task<(List<ActivityLogModel> Items, int TotalCount)> GetAllAsync(
        string? entityType, string? action, long? miqaatId, int? memberId,
        DateTime? fromDate, DateTime? toDate, string? search,
        int page, int pageSize);
    Task<(List<ActivityLogModel> Items, int TotalCount)> GetByMiqaatIdAsync(long miqaatId, int page, int pageSize);
    Task<(List<ActivityLogModel> Items, int TotalCount)> GetByMemberIdAsync(int memberId, int page, int pageSize);
    Task<List<ActivityLogModel>> GetRecentAsync(int count);
}

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly DapperContext _context;

    public ActivityLogRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ActivityLogModel log)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            INSERT INTO `activity_logs` 
                (`entity_type`, `entity_id`, `action`, `performed_by`, `performed_by_id`,
                 `performed_by_role`, `target_member_id`, `target_member_name`, `miqaat_id`,
                 `miqaat_day`, `old_value`, `new_value`, `details`, `ip_address`, `created_at`)
            VALUES 
                (@EntityType, @EntityId, @Action, @PerformedBy, @PerformedById,
                 @PerformedByRole, @TargetMemberId, @TargetMemberName, @MiqaatId,
                 @MiqaatDay, @OldValue, @NewValue, @Details, @IpAddress, @CreatedAt)
        """;

        await connection.ExecuteAsync(sql, log);
    }

    public async Task AddBatchAsync(List<ActivityLogModel> logs)
    {
        if (logs == null || logs.Count == 0) return;

        using var connection = _context.CreateConnection();

        const string sql = """
            INSERT INTO `activity_logs` 
                (`entity_type`, `entity_id`, `action`, `performed_by`, `performed_by_id`,
                 `performed_by_role`, `target_member_id`, `target_member_name`, `miqaat_id`,
                 `miqaat_day`, `old_value`, `new_value`, `details`, `ip_address`, `created_at`)
            VALUES 
                (@EntityType, @EntityId, @Action, @PerformedBy, @PerformedById,
                 @PerformedByRole, @TargetMemberId, @TargetMemberName, @MiqaatId,
                 @MiqaatDay, @OldValue, @NewValue, @Details, @IpAddress, @CreatedAt)
        """;

        await connection.ExecuteAsync(sql, logs);
    }

    public async Task<(List<ActivityLogModel> Items, int TotalCount)> GetAllAsync(
        string? entityType, string? action, long? miqaatId, int? memberId,
        DateTime? fromDate, DateTime? toDate, string? search,
        int page, int pageSize)
    {
        using var connection = _context.CreateConnection();

        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            whereClauses.Add("a.`entity_type` = @EntityType");
            parameters.Add("EntityType", entityType);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            whereClauses.Add("a.`action` = @Action");
            parameters.Add("Action", action);
        }

        if (miqaatId.HasValue)
        {
            whereClauses.Add("a.`miqaat_id` = @MiqaatId");
            parameters.Add("MiqaatId", miqaatId.Value);
        }

        if (memberId.HasValue)
        {
            whereClauses.Add("(a.`performed_by_id` = @MemberId OR a.`target_member_id` = @MemberId)");
            parameters.Add("MemberId", memberId.Value);
        }

        if (fromDate.HasValue)
        {
            whereClauses.Add("a.`created_at` >= @FromDate");
            parameters.Add("FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            whereClauses.Add("a.`created_at` <= @ToDate");
            parameters.Add("ToDate", toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClauses.Add("(a.`performed_by` LIKE @Search OR a.`target_member_name` LIKE @Search OR a.`details` LIKE @Search OR a.`action` LIKE @Search OR m.`miqaat_name` LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $"SELECT COUNT(*) FROM `activity_logs` a LEFT JOIN `local_miqaat` m ON a.`miqaat_id` = m.`id` {whereClause}";
        var totalCount = await connection.QueryFirstOrDefaultAsync<int>(countSql, parameters);

        var offset = (page - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var dataSql = $"""
            SELECT 
                a.`id` AS Id, a.`entity_type` AS EntityType, a.`entity_id` AS EntityId,
                a.`action` AS `Action`, a.`performed_by` AS PerformedBy, a.`performed_by_id` AS PerformedById,
                a.`performed_by_role` AS PerformedByRole, a.`target_member_id` AS TargetMemberId,
                a.`target_member_name` AS TargetMemberName, a.`miqaat_id` AS MiqaatId,
                m.`miqaat_name` AS MiqaatName,
                a.`miqaat_day` AS MiqaatDay, a.`old_value` AS OldValue, a.`new_value` AS NewValue,
                a.`details` AS Details, a.`ip_address` AS IpAddress, a.`created_at` AS CreatedAt
            FROM `activity_logs` a
            LEFT JOIN `local_miqaat` m ON a.`miqaat_id` = m.`id`
            {whereClause}
            ORDER BY a.`created_at` DESC
            LIMIT @PageSize OFFSET @Offset
        """;

        var items = (await connection.QueryAsync<ActivityLogModel>(dataSql, parameters)).ToList();
        return (items, totalCount);
    }

    public async Task<(List<ActivityLogModel> Items, int TotalCount)> GetByMiqaatIdAsync(long miqaatId, int page, int pageSize)
    {
        return await GetAllAsync(null, null, miqaatId, null, null, null, null, page, pageSize);
    }

    public async Task<(List<ActivityLogModel> Items, int TotalCount)> GetByMemberIdAsync(int memberId, int page, int pageSize)
    {
        return await GetAllAsync(null, null, null, memberId, null, null, null, page, pageSize);
    }

    public async Task<List<ActivityLogModel>> GetRecentAsync(int count)
    {
        using var connection = _context.CreateConnection();

        const string sql = """
            SELECT 
                a.`id` AS Id, a.`entity_type` AS EntityType, a.`entity_id` AS EntityId,
                a.`action` AS `Action`, a.`performed_by` AS PerformedBy, a.`performed_by_id` AS PerformedById,
                a.`performed_by_role` AS PerformedByRole, a.`target_member_id` AS TargetMemberId,
                a.`target_member_name` AS TargetMemberName, a.`miqaat_id` AS MiqaatId,
                m.`miqaat_name` AS MiqaatName,
                a.`miqaat_day` AS MiqaatDay, a.`old_value` AS OldValue, a.`new_value` AS NewValue,
                a.`details` AS Details, a.`ip_address` AS IpAddress, a.`created_at` AS CreatedAt
            FROM `activity_logs` a
            LEFT JOIN `local_miqaat` m ON a.`miqaat_id` = m.`id`
            ORDER BY a.`created_at` DESC
            LIMIT @Count
        """;

        var items = (await connection.QueryAsync<ActivityLogModel>(sql, new { Count = count })).ToList();
        return items;
    }
}
