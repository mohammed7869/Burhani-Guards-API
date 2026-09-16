using BurhaniGuards.Api.Domain;

namespace BurhaniGuards.Api.Repositories;

/// <summary>
/// Repository interface for notification CRUD operations.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Insert a new notification record.
    /// </summary>
    Task<int> CreateAsync(Notification notification);

    /// <summary>
    /// Insert multiple notifications in a single batch (for broadcast).
    /// </summary>
    Task BulkCreateAsync(IEnumerable<Notification> notifications);

    /// <summary>
    /// Get paginated notifications for a user, newest first.
    /// </summary>
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId, int page, int pageSize);

    /// <summary>
    /// Get total notification count for a user.
    /// </summary>
    Task<int> GetTotalCountAsync(int userId);

    /// <summary>
    /// Get unread notification count for a user.
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
    /// Delete a notification (optional, for cleanup).
    /// </summary>
    Task<bool> DeleteAsync(int userId, int notificationId);

    /// <summary>
    /// Get all notification logs (admin only).
    /// </summary>
    Task<IEnumerable<Notification>> GetAllLogsAsync();

    /// <summary>
    /// Get distinct notifications (by title+body) created since a UTC datetime,
    /// along with all user IDs that should receive them. Used for resending missed FCM pushes.
    /// </summary>
    Task<IEnumerable<(Notification Notification, List<int> UserIds)>> GetDistinctNotificationsSinceAsync(DateTime sinceUtc);
}
