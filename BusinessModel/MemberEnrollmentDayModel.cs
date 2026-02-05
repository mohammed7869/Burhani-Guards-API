namespace BurhaniGuards.Api.BusinessModel;

public class MemberEnrollmentDayModel
{
    public int MiqaatDay { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FinalStatus { get; set; }
    public DateTime MiqaatDate { get; set; }
}
