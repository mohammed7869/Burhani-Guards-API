using BurhaniGuards.Api.Contracts.Requests;

namespace BurhaniGuards.Api.Services;

/// <summary>
/// Service interface for notification operations.
/// Combines database persistence with real-time SignalR delivery.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send a notification to a specific user (persists + pushes via SignalR).
    /// </summary>
    Task SendToUserAsync(int userId, string title, string body, string type, string? referenceId = null, string? imageUrl = null);

    /// <summary>
    /// Send a notification to multiple users.
    /// </summary>
    Task SendToUsersAsync(IEnumerable<int> userIds, string title, string body, string type, string? referenceId = null, string? imageUrl = null);

    /// <summary>
    /// Broadcast a notification to all users.
    /// </summary>
    Task BroadcastAsync(string title, string body, string type, string? referenceId = null, string? imageUrl = null);

    /// <summary>
    /// Send notification to all members of a specific jamaat.
    /// </summary>
    Task SendToJamaatAsync(string jamaat, string title, string body, string type, string? referenceId = null, string? imageUrl = null);

    /// <summary>
    /// Get paginated notifications for a user.
    /// </summary>
    Task<NotificationListResponse> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get unread notification count.
    /// </summary>
    Task<int> GetUnreadCountAsync(int userId);

    /// <summary>
    /// Mark a single notification as read.
    /// </summary>
    Task<bool> MarkAsReadAsync(int userId, int notificationId);

    /// <summary>
    /// Mark all notifications as read for a user.
    /// </summary>
    Task<int> MarkAllAsReadAsync(int userId);

    /// <summary>
    /// Delete a notification.
    /// </summary>
    Task<bool> DeleteAsync(int userId, int notificationId);
}
