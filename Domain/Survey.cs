namespace BurhaniGuards.Api.Domain;

/// <summary>
/// Survey entity from MySQL survey table
/// Ashara Mubaraka Poona 1448 - Khidmat Allocation Status
/// </summary>
public sealed class Survey
{
    public int Id { get; init; }
    public int MemberId { get; init; }
    public int Department { get; init; }
    public int Zone { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
