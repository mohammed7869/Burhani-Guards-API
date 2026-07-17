namespace BurhaniGuards.Api.Domain;

/// <summary>
/// Domain entity representing a notification sent to a user.
/// Maps to the Notifications table in SQL Server.
/// </summary>
public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Notification category: miqaat, qardan, admin, survey, member, general
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Optional reference to a related entity (e.g., miqaat ID, loan ID).
    /// Used for deep-linking in the Flutter app.
    /// </summary>
    public string? ReferenceId { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
