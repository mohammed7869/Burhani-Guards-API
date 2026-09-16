using Asp.Versioning;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BurhaniGuards.Api.Controllers;

[Route("api/{v:apiVersion}/jamaat-transfers")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class JamaatTransferController : BaseController
{
    private readonly IJamaatTransferService _transferService;

    public JamaatTransferController(IJamaatTransferService transferService)
    {
        _transferService = transferService;
    }

    [HttpPost]
    public async Task<IActionResult> InitiateTransfer([FromBody] InitiateJamaatTransferRequest request)
    {
        if (CurrentUser == null) return Unauthorized();
        try
        {
            var id = await _transferService.InitiateTransferAsync(request, CurrentUser.id);
            return Ok(new { id, message = "Transfer request initiated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("pending/from/{jamaatId}")]
    public async Task<IActionResult> GetPendingByFromJamaat(int jamaatId)
    {
        try
        {
            var transfers = await _transferService.GetPendingByFromJamaatIdAsync(jamaatId);
            return Ok(transfers);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("pending/to/{jamaatId}")]
    public async Task<IActionResult> GetPendingByToJamaat(int jamaatId)
    {
        try
        {
            var transfers = await _transferService.GetPendingByToJamaatIdAsync(jamaatId);
            return Ok(transfers);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetAllByStatus(string status)
    {
        try
        {
            var transfers = await _transferService.GetAllByStatusAsync(status.ToUpper());
            return Ok(transfers);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/accept")]
    public async Task<IActionResult> AcceptTransfer(int id)
    {
        if (CurrentUser == null) return Unauthorized();
        try
        {
            await _transferService.AcceptTransferAsync(id, CurrentUser.id);
            return Ok(new { message = "Transfer request accepted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveTransfer(int id)
    {
        if (CurrentUser == null) return Unauthorized();
        try
        {
            await _transferService.ApproveTransferAsync(id, CurrentUser.id);
            return Ok(new { message = "Transfer request officially approved" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectTransfer(int id, [FromBody] RejectJamaatTransferRequest request)
    {
        if (CurrentUser == null) return Unauthorized();
        try
        {
            await _transferService.RejectTransferAsync(id, request, CurrentUser.id);
            return Ok(new { message = "Transfer request rejected" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("history/jamaat/{jamaatId}")]
    public async Task<IActionResult> GetHistoryByJamaatId(int jamaatId)
    {
        try
        {
            var transfers = await _transferService.GetHistoryByJamaatIdAsync(jamaatId);
            return Ok(transfers);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        try
        {
            var transfers = await _transferService.GetHistoryAsync();
            return Ok(transfers);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
