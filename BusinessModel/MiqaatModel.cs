using System.ComponentModel.DataAnnotations.Schema;

namespace BurhaniGuards.Api.BusinessModel;

[Table("local_miqaat")]
public class MiqaatModel : BaseModel
{
    public string MiqaatName { get; set; } = string.Empty;
    public string Jamaat { get; set; } = string.Empty;
    public string Jamiyat { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime TillDate { get; set; }
    public int MiqaatDays { get; set; }
    public int VolunteerLimit { get; set; }
    public string? AboutMiqaat { get; set; }
    public AdminApprovalStatus AdminApproval { get; set; } = AdminApprovalStatus.Pending;
    public string CaptainName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    // Member-specific statuses when joined from miqaat_members
    public string? MemberStatus { get; set; }
    public string? FinalStatus { get; set; }
    // Day-wise attendance fields (when joined from miqaat_members)
    public int? MiqaatDay { get; set; }
    public bool? IsAttended { get; set; }
    public int Points { get; set; }
    // Report fields
    public string? MiqaatImage1 { get; set; }
    public string? MiqaatImage2 { get; set; }
    public string? Notes { get; set; }
}

