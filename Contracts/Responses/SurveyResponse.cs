namespace BurhaniGuards.Api.Contracts.Responses;

/// <summary>
/// Response DTO for Survey (includes member info from members table)
/// </summary>
public class SurveyResponse
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string ItsId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string? Profile { get; set; }
    public int Department { get; set; }
    public int Zone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
