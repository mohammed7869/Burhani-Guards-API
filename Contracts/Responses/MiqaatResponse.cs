namespace BurhaniGuards.Api.Contracts.Responses;

public sealed record MiqaatResponse(
    long Id,
    string MiqaatName,
    string Jamaat,
    string Jamiyat,
    DateTime FromDate,
    DateTime TillDate,
    int MiqaatDays,
    int VolunteerLimit,
    string? AboutMiqaat,
    string AdminApproval,
    string CaptainName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? Status,
    string? FinalStatus,
    string? MiqaatImage1,
    string? MiqaatImage2,
    string? Notes
);


