using System.ComponentModel.DataAnnotations.Schema;

namespace BurhaniGuards.Api.BusinessModel;

[Dapper.Contrib.Extensions.Table("members")]
public class UserModel : BaseModel
{
    [Column("profile")]
    public string? Profile { get; set; }

    [Column("its_id")]
    public string? ItsId { get; set; }

    [Column("rank")]
    public string Rank { get; set; } = string.Empty;

    [Column("roles")]
    public int? Roles { get; set; }

    [Column("jamiyat")]
    public string? Jamiyat { get; set; }

    [Column("jamaat")]
    public string? Jamaat { get; set; }

    [Column("jamiyat_id")]
    public int? JamiyatId { get; set; }

    [Column("jamaat_id")]
    public int? JamaatId { get; set; }

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("gender")]
    public string? Gender { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("age")]
    public int? Age { get; set; }

    [Column("contact")]
    public string? Contact { get; set; }

    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }

    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    [Column("new_password_hash")]
    public string? NewPasswordHash { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_approved")]
    public bool IsApproved { get; set; } = true;

    [Column("badge")]
    public string? Badge { get; set; } = "BGI";

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

