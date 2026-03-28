namespace BurhaniGuards.Api.Contracts.Responses;

public record ActivityLogResponse(
    long Id,
    string EntityType,
    long EntityId,
    string Action,
    string? PerformedBy,
    int? PerformedById,
    string? PerformedByRole,
    int? TargetMemberId,
    string? TargetMemberName,
    long? MiqaatId,
    string? MiqaatName,
    int? MiqaatDay,
    string? OldValue,
    string? NewValue,
    string? Details,
    DateTime CreatedAt
);

public record ActivityLogPagedResponse(
    List<ActivityLogResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
