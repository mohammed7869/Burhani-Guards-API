namespace BurhaniGuards.Api.BusinessModel;

public class MemberPointsModel
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ItsId { get; set; }
    public string? Jamaat { get; set; }
    public int TotalPoints { get; set; }
}
