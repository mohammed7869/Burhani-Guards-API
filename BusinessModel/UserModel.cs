using System.ComponentModel.DataAnnotations.Schema;

namespace BurhaniGuards.Api.BusinessModel;

[Table("members")]
public class UserModel : BaseModel
{
    public string? Profile { get; set; }
    public string? ItsId { get; set; }
    public string Rank { get; set; } = string.Empty;
    public int? Roles { get; set; }
    public string? Jamiyat { get; set; }
    public string? Jamaat { get; set; }
    public int? JamiyatId { get; set; }
    public int? JamaatId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string Email { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string? Contact { get; set; }
    public string? PasswordHash { get; set; }
    public string? NewPasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

