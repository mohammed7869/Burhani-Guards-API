using BurhaniGuards.Api.ViewModel;

namespace BurhaniGuards.Api.Services;

public sealed class AuthTokenData
{
    public int Id { get; init; }
    public string? ItsId { get; init; }
    public string? Email { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Rank { get; init; } = string.Empty;
    public int? Roles { get; init; }
    public string? Jamiyat { get; init; }
    public string? Jamaat { get; init; }
    public string Role { get; init; } = "member";
    public bool RequiresPasswordChange { get; init; }
    public long IssuedAtUnixSeconds { get; init; }
    public long ExpiresAtUnixSeconds { get; init; }
}

public interface ITokenService
{
    string GenerateToken(string subject, string role);
    string GenerateToken(CurrentUserViewModel user, string role);
    AuthTokenData? ValidateToken(string token);
}

