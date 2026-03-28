using Asp.Versioning;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BurhaniGuards.Api.Controllers;

[Route("api/{v:apiVersion}/activity-logs")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class ActivityLogController : BaseController
{
    private readonly IActivityLogService _activityLogService;
    private static readonly TimeZoneInfo IndiaTimeZone = GetIndiaTimeZone();

    public ActivityLogController(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromHours(5.5), "India Standard Time", "India Standard Time");
            }
        }
    }

    private static DateTime ConvertUtcToIst(DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, IndiaTimeZone);
    }

    /// <summary>
    /// Get all activity logs with optional filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] long? miqaatId = null,
        [FromQuery] int? memberId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var (items, totalCount) = await _activityLogService.GetAllAsync(
                entityType, action, miqaatId, memberId, fromDate, toDate, search, page, pageSize);

            var response = new ActivityLogPagedResponse(
                items.Select(MapToResponse).ToList(),
                totalCount,
                page,
                pageSize,
                (int)Math.Ceiling((double)totalCount / pageSize)
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get activity logs for a specific miqaat
    /// </summary>
    [HttpGet("miqaat/{miqaatId:long}")]
    public async Task<IActionResult> GetByMiqaatId(long miqaatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var (items, totalCount) = await _activityLogService.GetByMiqaatIdAsync(miqaatId, page, pageSize);

            var response = new ActivityLogPagedResponse(
                items.Select(MapToResponse).ToList(),
                totalCount,
                page,
                pageSize,
                (int)Math.Ceiling((double)totalCount / pageSize)
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get activity logs for a specific member
    /// </summary>
    [HttpGet("member/{memberId:int}")]
    public async Task<IActionResult> GetByMemberId(int memberId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var (items, totalCount) = await _activityLogService.GetByMemberIdAsync(memberId, page, pageSize);

            var response = new ActivityLogPagedResponse(
                items.Select(MapToResponse).ToList(),
                totalCount,
                page,
                pageSize,
                (int)Math.Ceiling((double)totalCount / pageSize)
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get recent activity logs (for dashboard widget)
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 20)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            if (count < 1) count = 10;
            if (count > 100) count = 100;

            var items = await _activityLogService.GetRecentAsync(count);
            return Ok(items.Select(MapToResponse).ToList());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static ActivityLogResponse MapToResponse(BurhaniGuards.Api.BusinessModel.ActivityLogModel log)
    {
        return new ActivityLogResponse(
            log.Id,
            log.EntityType,
            log.EntityId,
            log.Action,
            log.PerformedBy,
            log.PerformedById,
            log.PerformedByRole,
            log.TargetMemberId,
            log.TargetMemberName,
            log.MiqaatId,
            log.MiqaatName,
            log.MiqaatDay,
            log.OldValue,
            log.NewValue,
            log.Details,
            ConvertUtcToIst(log.CreatedAt)
        );
    }
}
