namespace BurhaniGuards.Api.Domain;

public sealed class CaptainCredential
{
    public string ItsNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public string? NewPasswordHash { get; init; }
}

