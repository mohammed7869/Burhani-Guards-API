namespace BurhaniGuards.Api.Contracts.Responses;

/// <summary>
/// Unified login response containing all member data
/// </summary>
public sealed record MemberAuthResponse(
    int Id,
    string? Profile,
    string ItsId,
    string Rank,
    int? Roles,
    string? Jamiyat,
    string? Jamaat,
    string FullName,
    string? Gender,
    string Email,
    int? Age,
    string? Contact,
    string Role,
    string Token,
    bool RequiresPasswordChange = false
);



