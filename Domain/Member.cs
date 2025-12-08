namespace BurhaniGuards.Api.Domain;

/// <summary>
/// Member entity from MySQL members table
/// </summary>
public sealed class Member
{
    public int Id { get; init; }
    public string? Profile { get; init; }
    public string ItsId { get; init; } = string.Empty;
    public string Rank { get; init; } = string.Empty;
    public int? Roles { get; init; }
    public string? Jamiyat { get; init; }
    public string? Jamaat { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Gender { get; init; }
    public string Email { get; init; } = string.Empty;
    public int? Age { get; init; }
    public string? Contact { get; init; }
    public string? PasswordHash { get; init; }
    public string? NewPasswordHash { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

