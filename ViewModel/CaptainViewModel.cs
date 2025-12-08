using System.ComponentModel.DataAnnotations.Schema;

namespace BurhaniGuards.Api.ViewModel;

[Table("captains")]
public class CaptainViewModel
{
    public int id { get; set; }
    public string itsNumber { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string passwordHash { get; set; } = string.Empty;
    public string? newPasswordHash { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
}

public class CurrentCaptainViewModel
{
    public int id { get; set; }
    public string itsNumber { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public bool requiresPasswordChange { get; set; }
}

