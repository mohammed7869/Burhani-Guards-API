namespace BurhaniGuards.Api.Contracts.Requests;

/// <summary>
/// Request to submit a survey for Ashara Mubaraka Poona 1448 Khidmat Allocation
/// </summary>
public class CreateSurveyRequest
{
    public int Department { get; set; }
    public int Zone { get; set; }
}
