using Asp.Versioning;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BurhaniGuards.Api.Controllers;

[Route("api/{v:apiVersion}/survey")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class SurveyController : BaseController
{
    private readonly ISurveyService _service;

    public SurveyController(ISurveyService service)
    {
        _service = service;
    }

    /// <summary>
    /// Submit survey form (Member)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSurveyRequest request)
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            var response = await _service.Create(request, CurrentUser);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Check if current user has already submitted survey
    /// </summary>
    [HttpGet("has-submitted")]
    public async Task<IActionResult> HasSubmitted()
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            var hasSubmitted = await _service.HasSubmitted(CurrentUser.id);
            return Ok(new { hasSubmitted });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get current user's survey response (for preview)
    /// </summary>
    [HttpGet("my-survey")]
    public async Task<IActionResult> GetMySurvey()
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            var survey = await _service.GetByMemberId(CurrentUser.id);
            if (survey == null)
                return NotFound(new { message = "Survey not found" });

            return Ok(survey);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all survey responses (Admin only)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (CurrentUser == null)
            return Unauthorized();

        // Only Resource Admin (7) can see all
        if (CurrentUser.roles != 7)
            return Forbid("Only Admin can view all survey responses");

        try
        {
            var surveys = await _service.GetAll();
            return Ok(surveys);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a member's survey (Admin only)
    /// </summary>
    [HttpPut("{memberId}")]
    public async Task<IActionResult> Update(int memberId, [FromBody] CreateSurveyRequest request)
    {
        if (CurrentUser == null)
            return Unauthorized();

        // Only Resource Admin (7) can update
        if (CurrentUser.roles != 7)
            return Forbid("Only Admin can update survey responses");

        try
        {
            var response = await _service.Update(memberId, request, CurrentUser);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
