namespace BurhaniGuards.Api.Contracts.Responses;

public sealed record MiqaatResponse(
    long Id,
    string MiqaatName,
    string Jamaat,
    string Jamiyat,
    DateTime FromDate,
    DateTime TillDate,
    int VolunteerLimit,
    string? AboutMiqaat,
    string AdminApproval,
    string CaptainName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

