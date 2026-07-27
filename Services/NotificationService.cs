using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Domain;
using BurhaniGuards.Api.Hubs;
using BurhaniGuards.Api.Repositories;
using BurhaniGuards.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BurhaniGuards.Api.Services;

/// <summary>
/// Notification service that persists notifications to SQL Server and
/// delivers them in real-time via SignalR hub.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IUserRepository _userRepo;
    private readonly IFcmPushService _fcmPushService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepo,
        IHubContext<NotificationHub> hubContext,
        IUserRepository userRepo,
        IFcmPushService fcmPushService,
        ILogger<NotificationService> logger)
    {
        _notificationRepo = notificationRepo;
        _hubContext = hubContext;
        _userRepo = userRepo;
        _fcmPushService = fcmPushService;
        _logger = logger;
    }

    public async Task SendToUserAsync(int userId, string title, string body, string type, string? referenceId = null, string? imageUrl = null, string? linkUrl = null)
    {
        try
        {
            // 1. Persist to database
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                ReferenceId = referenceId,
                ImageUrl = imageUrl,
                LinkUrl = linkUrl
            };

            var id = await _notificationRepo.CreateAsync(notification);

            // 2. Push via SignalR to the connected user
            var dto = new NotificationDto
            {
                Id = id,
                Title = title,
                Body = body,
                Type = type,
                ReferenceId = referenceId,
                ImageUrl = imageUrl,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            // Send to the user's SignalR group (user_{userId})
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReceiveNotification", dto);

            // Fetch FCM token BEFORE Task.Run to avoid ObjectDisposedException
            var fcmToken = await _userRepo.GetFcmTokenAsync(userId);

            // Also send via FCM for background/killed state delivery
            _ = Task.Run(async () =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(fcmToken))
                    {
                        var data = new Dictionary<string, string>
                        {
                            ["type"] = type,
                            ["referenceId"] = referenceId ?? ""
                        };
                        if (!string.IsNullOrEmpty(linkUrl))
                        {
                            data["linkUrl"] = linkUrl;
                        }
                        await _fcmPushService.SendAsync(fcmToken, title, body, data, imageUrl);
                    }
                }
                catch (Exception fcmEx)
                {
                    _logger.LogWarning(fcmEx, "FCM push failed for user {UserId}", userId);
                }
            });

            _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            throw;
        }
    }

    public async Task SendToUsersAsync(IEnumerable<int> userIds, string title, string body, string type, string? referenceId = null, string? imageUrl = null, string? linkUrl = null)
    {
        var userIdList = userIds.ToList();
        
        try
        {
            // 1. Persist all notifications in batch
            var notifications = userIdList.Select(uid => new Notification
            {
                UserId = uid,
                Title = title,
                Body = body,
                Type = type,
                ReferenceId = referenceId,
                ImageUrl = imageUrl,
                LinkUrl = linkUrl
            });

            await _notificationRepo.BulkCreateAsync(notifications);

            // 2. Push via SignalR to each connected user
            var dto = new NotificationDto
            {
                Title = title,
                Body = body,
                Type = type,
                ReferenceId = referenceId,
                ImageUrl = imageUrl,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var tasks = userIdList.Select(uid =>
                _hubContext.Clients.Group($"user_{uid}")
                    .SendAsync("ReceiveNotification", dto));

            await Task.WhenAll(tasks);

            // Fetch FCM tokens BEFORE Task.Run to avoid ObjectDisposedException
            var fcmTokens = await _userRepo.GetFcmTokensAsync(userIdList);

            // Also send via FCM for background/killed state delivery
            _ = Task.Run(async () =>
            {
                try
                {
                    if (fcmTokens.Count > 0)
                    {
                        var data = new Dictionary<string, string>
                        {
                            ["type"] = type,
                            ["referenceId"] = referenceId ?? ""
                        };
                        if (!string.IsNullOrEmpty(linkUrl))
                        {
                            data["linkUrl"] = linkUrl;
                        }
                        await _fcmPushService.SendToMultipleAsync(fcmTokens.Values, title, body, data, imageUrl);
                    }
                }
                catch (Exception fcmEx)
                {
                    _logger.LogWarning(fcmEx, "FCM multicast push failed");
                }
            });

            _logger.LogInformation("Notification sent to {Count} users: {Title}", userIdList.Count, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to {Count} users", userIdList.Count);
            throw;
        }
    }

    public async Task BroadcastAsync(string title, string body, string type, string? referenceId = null, string? imageUrl = null, string? linkUrl = null)
    {
        try
        {
            // Get all user IDs from the database
            var allUsers = await _userRepo.GetAllUserIdsAsync();
            var userIdList = allUsers.ToList();

            if (userIdList.Count == 0)
            {
                _logger.LogWarning("No users found for broadcast notification");
                return;
            }

            // Persist for all users
            var notifications = userIdList.Select(uid => new Notification
            {
                UserId = uid,
                Title = title,
                Body = body,
                Type = type,
                ReferenceId = referenceId,
                ImageUrl = imageUrl,
                LinkUrl = linkUrl
            });

            await _notificationRepo.BulkCreateAsync(notifications);

            // Push to all connected clients
            var dto = new NotificationDto
            {
                Title = title,
                Body = body,
                Type = type,
                ReferenceId = referenceId,
                ImageUrl = imageUrl,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", dto);

            // Fetch FCM tokens BEFORE Task.Run to avoid ObjectDisposedException
            var fcmTokens = await _userRepo.GetFcmTokensAsync(userIdList);

            // Also send via FCM for background/killed state delivery
            _ = Task.Run(async () =>
            {
                try
                {
                    if (fcmTokens.Count > 0)
                    {
                        var data = new Dictionary<string, string>
                        {
                            ["type"] = type,
                            ["referenceId"] = referenceId ?? ""
                        };
                        if (!string.IsNullOrEmpty(linkUrl))
                        {
                            data["linkUrl"] = linkUrl;
                        }
                        await _fcmPushService.SendToMultipleAsync(fcmTokens.Values, title, body, data, imageUrl);
                    }
                }
                catch (Exception fcmEx)
                {
                    _logger.LogWarning(fcmEx, "FCM broadcast push failed");
                }
            });

            _logger.LogInformation("Broadcast notification sent to {Count} users: {Title}", userIdList.Count, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification");
            throw;
        }
    }

    public async Task SendToJamaatAsync(string jamaat, string title, string body, string type, string? referenceId = null, string? imageUrl = null, string? linkUrl = null)
    {
        try
        {
            var jamaats = jamaat.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(j => j.Trim());
            var userIdList = new List<int>();

            foreach (var j in jamaats)
            {
                var jamaatUsers = await _userRepo.GetUserIdsByJamaatAsync(j);
                userIdList.AddRange(jamaatUsers);
            }

            userIdList = userIdList.Distinct().ToList();

            if (userIdList.Count == 0)
            {
                _logger.LogWarning("No users found for jamaat {Jamaat}", jamaat);
                return;
            }

            await SendToUsersAsync(userIdList, title, body, type, referenceId, imageUrl, linkUrl);

            _logger.LogInformation("Notification sent to jamaat {Jamaat} ({Count} users): {Title}", 
                jamaat, userIdList.Count, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to jamaat {Jamaat}", jamaat);
            throw;
        }
    }

    public async Task SendToJamiyatAsync(string jamiyat, string title, string body, string type, string? referenceId = null, string? imageUrl = null, string? linkUrl = null)
    {
        try
        {
            var jamiyats = jamiyat.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(j => j.Trim());
            var userIdList = new List<int>();

            foreach (var j in jamiyats)
            {
                var jamiyatUsers = await _userRepo.GetUserIdsByJamiyatAsync(j);
                userIdList.AddRange(jamiyatUsers);
            }

            userIdList = userIdList.Distinct().ToList();

            if (userIdList.Count == 0)
            {
                _logger.LogWarning("No users found for jamiyat {Jamiyat}", jamiyat);
                return;
            }

            await SendToUsersAsync(userIdList, title, body, type, referenceId, imageUrl, linkUrl);

            _logger.LogInformation("Notification sent to jamiyat {Jamiyat} ({Count} users): {Title}", 
                jamiyat, userIdList.Count, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to jamiyat {Jamiyat}", jamiyat);
            throw;
        }
    }

    public async Task<NotificationListResponse> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20)
    {
        var notifications = await _notificationRepo.GetByUserIdAsync(userId, page, pageSize);
        var totalCount = await _notificationRepo.GetTotalCountAsync(userId);
        var unreadCount = await _notificationRepo.GetUnreadCountAsync(userId);

        return new NotificationListResponse
        {
            Notifications = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Body = n.Body,
                Type = n.Type,
                ReferenceId = n.ReferenceId,
                ImageUrl = n.ImageUrl,
                LinkUrl = n.LinkUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt
            }).ToList(),
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _notificationRepo.GetUnreadCountAsync(userId);
    }

    public async Task<bool> MarkAsReadAsync(int userId, int notificationId)
    {
        return await _notificationRepo.MarkAsReadAsync(userId, notificationId);
    }

    public async Task<int> MarkAllAsReadAsync(int userId)
    {
        return await _notificationRepo.MarkAllAsReadAsync(userId);
    }

    public async Task<bool> DeleteAsync(int userId, int notificationId)
    {
        return await _notificationRepo.DeleteAsync(userId, notificationId);
    }

    public async Task<IEnumerable<NotificationDto>> GetAllLogsAsync()
    {
        var notifications = await _notificationRepo.GetAllLogsAsync();
        return notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            UserId = n.UserId,
            Title = n.Title,
            Body = n.Body,
            Type = n.Type,
            ReferenceId = n.ReferenceId,
            ImageUrl = n.ImageUrl,
            LinkUrl = n.LinkUrl,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            ReadAt = n.ReadAt,
            // Assuming NotificationDto might have UserId if needed, but since it doesn't currently, we might need to add it or return a different DTO.
            // Oh wait, NotificationDto probably doesn't have UserId based on the GetUserNotificationsAsync method, let's map it anyway. Wait, the frontend needs `user_id`.
        }).ToList();
    }
}
