namespace BurhaniGuards.Api.Contracts.Requests;

public sealed record UpdateMiqaatRequest(
    string MiqaatName,
    string? MiqaatType, // "Local" or "International"
    string Jamaat,
    string Jamiyat,
    DateTime FromDate,
    DateTime TillDate,
    int VolunteerLimit,
    string? AboutMiqaat,
    string? AdminApproval = null
);

