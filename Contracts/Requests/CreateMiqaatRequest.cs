namespace BurhaniGuards.Api.Contracts.Requests;

public sealed record CreateMiqaatRequest(
    string MiqaatName,
    string Jamaat,
    string Jamiyat,
    DateTime FromDate,
    DateTime TillDate,
    int VolunteerLimit,
    string? AboutMiqaat
);

