namespace BurhaniGuards.Api.Contracts.Responses;

public sealed record MemberPointsResponse(
    long MemberId,
    string FullName,
    string? ItsId,
    int TotalPoints
);

public sealed record AdminMemberPointsResponse(
    long MemberId,
    string FullName,
    string? ItsId,
    string? Jamaat,
    int TotalPoints
);
