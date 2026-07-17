using BurhaniGuards.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BurhaniGuards.Api.Hubs;

/// <summary>
/// SignalR hub for real-time notification delivery.
/// 
/// Clients connect with their Bearer token. On connection, each user is
/// automatically added to a personal group "user_{userId}" so the server
/// can push notifications to specific users.
///
/// Hub URL: /hubs/notification
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects. Adds the user to their personal group.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId > 0)
        {
            // Add user to their personal notification group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to NotificationHub (ConnectionId: {ConnectionId})",
                userId, Context.ConnectionId);
        }
        else
        {
            _logger.LogWarning("User connected without valid userId (ConnectionId: {ConnectionId})", 
                Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects. Removes from group.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId > 0)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from NotificationHub (ConnectionId: {ConnectionId})",
                userId, Context.ConnectionId);
        }

        if (exception != null)
        {
            _logger.LogError(exception, "User {UserId} disconnected with error", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client can call this to acknowledge receipt of a notification.
    /// This marks it as read on the server side.
    /// </summary>
    public async Task MarkAsRead(int notificationId)
    {
        var userId = GetUserId();
        if (userId <= 0) return;

        // Resolve the service from the hub's service provider
        var notificationService = Context.GetHttpContext()?.RequestServices
            .GetService<INotificationService>();

        if (notificationService != null)
        {
            await notificationService.MarkAsReadAsync(userId, notificationId);
            _logger.LogInformation("User {UserId} marked notification {NotificationId} as read via SignalR",
                userId, notificationId);
        }
    }

    /// <summary>
    /// Extracts the user ID from the authenticated claims.
    /// </summary>
    private int GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
