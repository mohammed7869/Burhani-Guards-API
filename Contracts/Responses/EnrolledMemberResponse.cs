namespace BurhaniGuards.Api.Contracts.Responses;

public sealed record EnrolledMemberResponse(
    long Id,
    string FullName,
    string? Email,
    string? Contact,
    string? Rank,
    string? Jamaat,
    string? Jamiyat,
    string? FinalStatus,
    string? ItsId,
    bool? IsAttended
);

