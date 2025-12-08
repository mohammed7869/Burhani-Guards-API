using System.ComponentModel.DataAnnotations.Schema;

namespace BurhaniGuards.Api.BusinessModel;

[Table("captains")]
public class CaptainModel : BaseModel
{
    public string ItsNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? NewPasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

