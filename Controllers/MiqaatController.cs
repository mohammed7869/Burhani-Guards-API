using Asp.Versioning;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BurhaniGuards.Api.Controllers;

[Route("api/{v:apiVersion}/miqaat")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class MiqaatController : BaseController
{
    private readonly IMiqaatService _miqaatService;

    public MiqaatController(IMiqaatService miqaatService)
    {
        _miqaatService = miqaatService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMiqaatRequest request)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        // Check if user is Captain (role = 2)
        if (CurrentUser.roles != 2)
        {
            return Forbid("Only Captains can create miqaats");
        }

        try
        {
            var response = await _miqaatService.Create(request, CurrentUser.fullName);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var miqaats = await _miqaatService.GetAll();
            return Ok(miqaats);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        try
        {
            var miqaat = await _miqaatService.GetById(id);
            if (miqaat == null)
            {
                return NotFound(new { message = "Miqaat not found" });
            }
            return Ok(miqaat);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateMiqaatRequest request)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            await _miqaatService.Update(id, request);
            return Ok(new { message = "Miqaat updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/approval")]
    public async Task<IActionResult> UpdateApprovalStatus(long id, [FromBody] UpdateApprovalRequest request)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            await _miqaatService.UpdateApprovalStatus(id, request.Status);
            return Ok(new { message = "Approval status updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            await _miqaatService.Delete(id);
            return Ok(new { message = "Miqaat deleted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("member/{memberId}")]
    public async Task<IActionResult> GetMiqaatsByMemberId(int memberId)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            // Use CurrentUser to determine if Captain or Member
            // If Captain: show all miqaats created by them
            // If Member: show miqaats from miqaat_members table
            var miqaats = await _miqaatService.GetMiqaatsForCurrentUser(
                CurrentUser.id, 
                CurrentUser.roles, 
                CurrentUser.fullName
            );
            return Ok(miqaats);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{miqaatId}/member/{memberId}/status")]
    public async Task<IActionResult> UpdateMemberMiqaatStatus(long miqaatId, int memberId, [FromBody] UpdateMemberMiqaatStatusRequest request)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        // Ensure the member can only update their own status
        if (CurrentUser.id != memberId)
        {
            return Forbid("You can only update your own miqaat status");
        }

        try
        {
            await _miqaatService.UpdateMemberMiqaatStatus(memberId, miqaatId, request.Status);
            return Ok(new { message = "Miqaat status updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

