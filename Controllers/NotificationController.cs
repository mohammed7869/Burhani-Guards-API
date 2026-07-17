using Asp.Versioning;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Services;
using BurhaniGuards.Api.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BurhaniGuards.Api.Controllers;

/// <summary>
/// REST API controller for notification management.
/// Provides endpoints for fetching, reading, and sending notifications.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/{version:apiVersion}/notifications")]
[Authorize]
public class NotificationController : BaseController
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated notifications for the authenticated user.
    /// GET /api/1/notifications?page=1&pageSize=20
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var user = GetCurrentUser();
        if (user == null) return Unauthorized("User not authenticated");

        try
        {
            // Clamp page size
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            var result = await _notificationService.GetUserNotificationsAsync(user.id, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications for user {UserId}", user.id);
            return StatusCode(500, new { message = "Error fetching notifications" });
        }
    }

    /// <summary>
    /// Get unread notification count for the authenticated user.
    /// GET /api/1/notifications/unread-count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var user = GetCurrentUser();
        if (user == null) return Unauthorized("User not authenticated");

        try
        {
            var count = await _notificationService.GetUnreadCountAsync(user.id);
            return Ok(new UnreadCountResponse { Count = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", user.id);
            return StatusCode(500, new { message = "Error getting unread count" });
        }
    }

    /// <summary>
    /// Mark a single notification as read.
    /// POST /api/1/notifications/mark-read/{id}
    /// </summary>
    [HttpPost("mark-read/{id}")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var user = GetCurrentUser();
        if (user == null) return Unauthorized("User not authenticated");

        try
        {
            var result = await _notificationService.MarkAsReadAsync(user.id, id);
            if (!result) return NotFound(new { message = "Notification not found or already read" });
            return Ok(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return StatusCode(500, new { message = "Error marking notification as read" });
        }
    }

    /// <summary>
    /// Mark all notifications as read for the authenticated user.
    /// POST /api/1/notifications/mark-all-read
    /// </summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var user = GetCurrentUser();
        if (user == null) return Unauthorized("User not authenticated");

        try
        {
            var count = await _notificationService.MarkAllAsReadAsync(user.id);
            return Ok(new { message = $"{count} notifications marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", user.id);
            return StatusCode(500, new { message = "Error marking all notifications as read" });
        }
    }

    /// <summary>
    /// Delete a notification.
    /// DELETE /api/1/notifications/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = GetCurrentUser();
        if (user == null) return Unauthorized("User not authenticated");

        try
        {
            var result = await _notificationService.DeleteAsync(user.id, id);
            if (!result) return NotFound(new { message = "Notification not found" });
            return Ok(new { message = "Notification deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
            return StatusCode(500, new { message = "Error deleting notification" });
        }
    }

    /// <summary>
    /// Send a notification (admin only).
    /// POST /api/1/notifications/send
    /// Supports: single user, multiple users, jamaat-based, and broadcast.
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        var user = GetCurrentUser();
        if (user == null) return Unauthorized("User not authenticated");

        // Check if user has admin role (roles bitmask: admin = 1)
        if (user.roles == null || (user.roles & 1) == 0)
        {
            return Forbid();
        }

        try
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { message = "Title is required" });

            if (string.IsNullOrWhiteSpace(request.Body))
                return BadRequest(new { message = "Body is required" });

            if (request.Broadcast)
            {
                await _notificationService.BroadcastAsync(
                    request.Title, request.Body, request.Type, request.ReferenceId);
                return Ok(new { message = "Broadcast notification sent" });
            }

            if (!string.IsNullOrWhiteSpace(request.TargetJamaat))
            {
                await _notificationService.SendToJamaatAsync(
                    request.TargetJamaat, request.Title, request.Body, request.Type, request.ReferenceId);
                return Ok(new { message = $"Notification sent to jamaat: {request.TargetJamaat}" });
            }

            if (request.UserIds.Count > 0)
            {
                await _notificationService.SendToUsersAsync(
                    request.UserIds, request.Title, request.Body, request.Type, request.ReferenceId);
                return Ok(new { message = $"Notification sent to {request.UserIds.Count} users" });
            }

            return BadRequest(new { message = "Specify UserIds, TargetJamaat, or set Broadcast to true" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return StatusCode(500, new { message = "Error sending notification" });
        }
    }

    /// <summary>
    /// Helper to get current user from HttpContext.Items (set by UserContextMiddleware).
    /// </summary>
    private CurrentUserViewModel? GetCurrentUser()
    {
        return HttpContext.Items["User"] as CurrentUserViewModel;
    }
}
