using BurhaniGuards.Api.Domain;
using Dapper;

namespace BurhaniGuards.Api.Repositories;

/// <summary>
/// Dapper-based repository for notification persistence in MySQL.
/// Follows the same pattern as UserRepository (using DapperContext for MySQL).
/// </summary>
public class NotificationRepository : INotificationRepository
{
    private readonly DapperContext _context;
    private readonly ILogger<NotificationRepository> _logger;

    public NotificationRepository(DapperContext context, ILogger<NotificationRepository> logger)
    {
        _context = context;
        _logger = logger;
        
        EnsureSchemaUpdated();
    }

    private void EnsureSchemaUpdated()
    {
        try
        {
            using var connection = _context.CreateConnection();
            connection.Execute(@"
                ALTER TABLE `notifications`
                ADD COLUMN IF NOT EXISTS `image_url` VARCHAR(500) NULL DEFAULT NULL,
                ADD COLUMN IF NOT EXISTS `link_url` VARCHAR(500) NULL DEFAULT NULL;
            ");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update notifications schema (columns might already exist)");
        }
    }

    public async Task<int> CreateAsync(Notification notification)
    {
        const string sql = @"
            INSERT INTO `notifications` (`user_id`, `title`, `body`, `type`, `reference_id`, `image_url`, `link_url`, `is_read`, `created_at`)
            VALUES (@UserId, @Title, @Body, @Type, @ReferenceId, @ImageUrl, @LinkUrl, 0, UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        try
        {
            using var connection = _context.CreateConnection();
            return await connection.QuerySingleAsync<int>(sql, new
            {
                notification.UserId,
                notification.Title,
                notification.Body,
                notification.Type,
                notification.ReferenceId,
                notification.ImageUrl,
                notification.LinkUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification for user {UserId}", notification.UserId);
            throw;
        }
    }

    public async Task BulkCreateAsync(IEnumerable<Notification> notifications)
    {
        const string sql = @"
            INSERT INTO `notifications` (`user_id`, `title`, `body`, `type`, `reference_id`, `image_url`, `link_url`, `is_read`, `created_at`)
            VALUES (@UserId, @Title, @Body, @Type, @ReferenceId, @ImageUrl, @LinkUrl, 0, UTC_TIMESTAMP())";

        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            foreach (var notification in notifications)
            {
                await connection.ExecuteAsync(sql, new
                {
                    notification.UserId,
                    notification.Title,
                    notification.Body,
                    notification.Type,
                    notification.ReferenceId,
                    notification.ImageUrl,
                    notification.LinkUrl
                }, transaction);
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating notifications");
            throw;
        }
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId, int page, int pageSize)
    {
        const string sql = @"
            SELECT 
                `id` AS Id, 
                `user_id` AS UserId, 
                `title` AS Title, 
                `body` AS Body, 
                `type` AS Type, 
                `reference_id` AS ReferenceId,
                `image_url` AS ImageUrl,
                `link_url` AS LinkUrl,
                `is_read` AS IsRead, 
                `created_at` AS CreatedAt, 
                `read_at` AS ReadAt
            FROM `notifications`
            WHERE `user_id` = @UserId
            ORDER BY `created_at` DESC
            LIMIT @PageSize OFFSET @Offset";

        try
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Notification>(sql, new
            {
                UserId = userId,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetTotalCountAsync(int userId)
    {
        const string sql = "SELECT COUNT(*) FROM `notifications` WHERE `user_id` = @UserId";

        try
        {
            using var connection = _context.CreateConnection();
            return await connection.QuerySingleAsync<int>(sql, new { UserId = userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification count for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        const string sql = "SELECT COUNT(*) FROM `notifications` WHERE `user_id` = @UserId AND `is_read` = 0";

        try
        {
            using var connection = _context.CreateConnection();
            return await connection.QuerySingleAsync<int>(sql, new { UserId = userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> MarkAsReadAsync(int userId, int notificationId)
    {
        const string sql = @"
            UPDATE `notifications` 
            SET `is_read` = 1, `read_at` = UTC_TIMESTAMP() 
            WHERE `id` = @NotificationId AND `user_id` = @UserId AND `is_read` = 0";

        try
        {
            using var connection = _context.CreateConnection();
            var affected = await connection.ExecuteAsync(sql, new
            {
                NotificationId = notificationId,
                UserId = userId
            });
            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            throw;
        }
    }

    public async Task<int> MarkAllAsReadAsync(int userId)
    {
        const string sql = @"
            UPDATE `notifications` 
            SET `is_read` = 1, `read_at` = UTC_TIMESTAMP() 
            WHERE `user_id` = @UserId AND `is_read` = 0";

        try
        {
            using var connection = _context.CreateConnection();
            return await connection.ExecuteAsync(sql, new { UserId = userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int userId, int notificationId)
    {
        const string sql = "DELETE FROM `notifications` WHERE `id` = @NotificationId AND `user_id` = @UserId";

        try
        {
            using var connection = _context.CreateConnection();
            var affected = await connection.ExecuteAsync(sql, new
            {
                NotificationId = notificationId,
                UserId = userId
            });
            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task<IEnumerable<Notification>> GetAllLogsAsync()
    {
        const string sql = @"
            SELECT 
                `id` AS Id, 
                `user_id` AS UserId, 
                `title` AS Title, 
                `body` AS Body, 
                `type` AS Type, 
                `reference_id` AS ReferenceId,
                `image_url` AS ImageUrl,
                `link_url` AS LinkUrl,
                `is_read` AS IsRead, 
                `created_at` AS CreatedAt, 
                `read_at` AS ReadAt
            FROM `notifications`
            ORDER BY `created_at` DESC
            LIMIT 1000";

        try
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Notification>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all notification logs");
            throw;
        }
    }
}
