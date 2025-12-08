namespace BurhaniGuards.Api.Contracts.Responses;

public sealed record AuthResponse(
    int Id,
    string? Profile,
    string? ItsId,
    string FullName,
    string Email,
    string Rank,
    int? Roles,
    string? Jamiyat,
    string? Jamaat,
    string? Gender,
    int? Age,
    string? Contact,
    string Role,
    string Token,
    bool RequiresPasswordChange = false
);
