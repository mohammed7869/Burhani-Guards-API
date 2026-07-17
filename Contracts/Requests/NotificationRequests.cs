namespace BurhaniGuards.Api.Contracts.Requests;

/// <summary>
/// DTO sent via SignalR and returned from REST API.
/// </summary>
public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// Request to send a notification from admin panel.
/// </summary>
public class SendNotificationRequest
{
    /// <summary>
    /// Target user IDs. If empty and Broadcast is true, sends to all users.
    /// </summary>
    public List<int> UserIds { get; set; } = new();

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Notification category: miqaat, qardan, admin, survey, member, general
    /// </summary>
    public string Type { get; set; } = "general";

    /// <summary>
    /// Optional reference to a related entity.
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// If true, sends to all users (ignores UserIds).
    /// </summary>
    public bool Broadcast { get; set; }

    /// <summary>
    /// Optional: target by jamaat name (e.g., send to all members of a specific jamaat).
    /// </summary>
    public string? TargetJamaat { get; set; }
}

/// <summary>
/// Response for paginated notification list.
/// </summary>
public class NotificationListResponse
{
    public List<NotificationDto> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Response for unread count.
/// </summary>
public class UnreadCountResponse
{
    public int Count { get; set; }
}
