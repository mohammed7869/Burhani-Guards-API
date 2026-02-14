namespace BurhaniGuards.Api.Contracts.Requests;

public sealed record CreateMiqaatRequest(
    string MiqaatName,
    string? MiqaatType, // "Local" or "International", defaults to "Local"
    string Jamaat,
    string Jamiyat,
    DateTime FromDate,
    DateTime TillDate,
    int VolunteerLimit,
    string? AboutMiqaat
);

