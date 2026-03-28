namespace BurhaniGuards.Api.Contracts.Responses;

public sealed record MemberTrackingRowResponse(
    long MemberId,
    string FullName,
    string ItsId,
    string? Rank,
    string? Jamaat,
    string? Contact,
    int Day,
    string Status,
    string? FinalStatus,
    string? AdminStatus,
    bool IsAttended
);
