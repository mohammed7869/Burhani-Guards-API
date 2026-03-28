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

    [HttpPost("admin-create")]
    public async Task<IActionResult> CreateByAdmin([FromBody] CreateMiqaatRequest request)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            var response = await _miqaatService.CreateByAdmin(request, CurrentUser.fullName);
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

    [HttpGet("insights")]
    public async Task<IActionResult> GetInsights()
    {
        try
        {
            var insights = await _miqaatService.GetInsights();
            return Ok(insights);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:long}/detailed-insights")]
    public async Task<IActionResult> GetDetailedInsights(long id)
    {
        try
        {
            var insights = await _miqaatService.GetMiqaatDetailedInsights(id);
            return Ok(insights);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:long}")]
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

    [HttpPut("{id:long}")]
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

    [HttpPatch("{id:long}/approval")]
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

    [HttpDelete("{id:long}")]
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

    [HttpGet("member/{memberId}/attendance-history")]
    public async Task<IActionResult> GetMemberAttendanceHistory(int memberId)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        // Only Captains can view member attendance/points history, unless it's the member themselves
        if (CurrentUser.roles != 2 && CurrentUser.id != memberId)
        {
            return Forbid("Only Captains can view member attendance history");
        }

        try
        {
            var history = await _miqaatService.GetMemberAttendanceHistory(memberId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("points")]
    public async Task<IActionResult> GetPoints()
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            if (CurrentUser.roles == 2)
            {
                if (string.IsNullOrWhiteSpace(CurrentUser.jamaat))
                {
                    return BadRequest(new { message = "Jamaat not found for current user" });
                }

                var members = await _miqaatService.GetMemberPointsByJamaat(CurrentUser.jamaat);
                return Ok(members);
            }

            var memberPoints = await _miqaatService.GetMemberPointsByMemberId(CurrentUser.id);
            return Ok(new[] { memberPoints });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("admin-points")]
    public async Task<IActionResult> GetAdminPoints()
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            var members = await _miqaatService.GetAllMemberPointsForAdmin();
            return Ok(members);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{miqaatId:long}/member/{memberId:int}/status")]
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
            var days = request.Days?
                .Where(d => d > 0)
                .Distinct()
                .ToList();

            if ((days == null || days.Count == 0) && request.Day.HasValue && request.Day.Value > 0)
            {
                days = new List<int> { request.Day.Value };
            }

            await _miqaatService.UpdateMemberMiqaatStatus(memberId, miqaatId, request.Status, days);
            return Ok(new { message = "Miqaat status updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{miqaatId:long}/enrolled-members")]
    public async Task<IActionResult> GetEnrolledMembers(long miqaatId)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        // Only Captains can view enrolled members
        if (CurrentUser.roles != 2)
        {
            return Forbid("Only Captains can view enrolled members");
        }

        try
        {
            var members = await _miqaatService.GetEnrolledMembersByMiqaatId(miqaatId);
            return Ok(members);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{miqaatId:long}/all-members")]
    public async Task<IActionResult> GetAllMembersByMiqaatId(long miqaatId)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        // Only Captains can view all members
        if (CurrentUser.roles != 2)
        {
            return Forbid("Only Captains can view all members");
        }

        try
        {
            var members = await _miqaatService.GetAllMembersByMiqaatId(miqaatId);
            return Ok(members);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{miqaatId:long}/approved-members-for-attendance")]
    public async Task<IActionResult> GetApprovedMembersForAttendance(long miqaatId, [FromQuery] int day = 1)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        // Check miqaat type to enforce role-based access
        var miqaat = await _miqaatService.GetById(miqaatId);
        if (miqaat == null)
        {
            return NotFound(new { message = "Miqaat not found" });
        }

        if (miqaat.MiqaatType == "International")
        {
            // Only Admin can view attendance for International miqaats
            if (CurrentUser.roles != 7)
            {
                return Forbid("Only Admin can view approved members for attendance on International miqaats");
            }
        }
        else
        {
            // Only Captains can view attendance for Local miqaats
            if (CurrentUser.roles != 2)
            {
                return Forbid("Only Captains can view approved members for attendance");
            }
        }

        try
        {
            var members = await _miqaatService.GetApprovedMembersForAttendance(miqaatId, day);
            return Ok(members);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{miqaatId:long}/member/{memberId:int}/final-status")]
    public async Task<IActionResult> UpdateFinalStatus(long miqaatId, int memberId, [FromBody] UpdateMemberMiqaatStatusRequest request)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        // Only Captains can update final status
        if (CurrentUser.roles != 2)
        {
            return Forbid("Only Captains can update final status");
        }

        try
        {
            var days = request.Days?
                .Where(d => d > 0)
                .Distinct()
                .ToList();

            if ((days == null || days.Count == 0) && request.Day.HasValue && request.Day.Value > 0)
            {
                days = new List<int> { request.Day.Value };
            }

            await _miqaatService.UpdateFinalStatus(memberId, miqaatId, request.Status, days);
            return Ok(new { message = "Final status updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin approves/rejects captain-approved members for International miqaats
    /// </summary>
    [HttpPatch("{miqaatId:long}/member/{memberId:int}/admin-status")]
    public async Task<IActionResult> UpdateAdminStatus(long miqaatId, int memberId, [FromBody] UpdateMemberMiqaatStatusRequest request)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        // Only Admin (role 7) can update admin status
        if (CurrentUser.roles != 7)
        {
            return Forbid("Only Admin can update admin status for international miqaats");
        }

        try
        {
            var days = request.Days?
                .Where(d => d > 0)
                .Distinct()
                .ToList();

            if ((days == null || days.Count == 0) && request.Day.HasValue && request.Day.Value > 0)
            {
                days = new List<int> { request.Day.Value };
            }

            await _miqaatService.UpdateAdminStatus(memberId, miqaatId, request.Status, days);
            return Ok(new { message = "Admin status updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get captain-approved members pending admin review for International miqaats
    /// </summary>
    [HttpGet("{miqaatId:long}/captain-approved-members")]
    public async Task<IActionResult> GetCaptainApprovedMembersForIntlMiqaat(long miqaatId, [FromQuery] int? day = null)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            var members = await _miqaatService.GetCaptainApprovedMembersForIntlMiqaat(miqaatId, day);
            return Ok(members);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{miqaatId:long}/member/{memberId:int}/enrollment-days")]
    public async Task<IActionResult> GetMemberEnrollmentDays(long miqaatId, int memberId)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        // Allow both Captains and the member themselves to view enrollment days
        if (CurrentUser.roles != 2 && CurrentUser.id != memberId)
        {
            return Forbid("You can only view your own enrollment days");
        }

        try
        {
            var enrollmentDays = await _miqaatService.GetMemberEnrollmentDays(miqaatId, memberId);
            return Ok(enrollmentDays);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{miqaatId:long}/mark-attendance")]
    public async Task<IActionResult> MarkAttendance(long miqaatId, [FromBody] MarkAttendanceRequest request)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        // Check miqaat type to enforce role-based access
        var miqaat = await _miqaatService.GetById(miqaatId);
        if (miqaat == null)
        {
            return NotFound(new { message = "Miqaat not found" });
        }

        if (miqaat.MiqaatType == "International")
        {
            // Only Admin can mark attendance for International miqaats
            if (CurrentUser.roles != 7)
            {
                return Forbid("Only Admin can mark attendance for International miqaats");
            }
        }
        else
        {
            // Only Captains can mark attendance for Local miqaats
            if (CurrentUser.roles != 2)
            {
                return Forbid("Only Captains can mark attendance");
            }
        }

        try
        {
            await _miqaatService.MarkAttendanceBatch(miqaatId, request.Day, request.MemberIds);
            return Ok(new { message = "Attendance marked successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{miqaatId:long}/report")]
    public async Task<IActionResult> SubmitMiqaatReport(long miqaatId, [FromForm] SubmitMiqaatReportRequest request)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        // Only Captains can submit miqaat reports
        if (CurrentUser.roles != 2)
        {
            return Forbid("Only Captains can submit miqaat reports");
        }

        // Validate mandatory fields
        if (request.Image1 == null || request.Image1.Length == 0)
        {
            return BadRequest(new { message = "Image 1 is required" });
        }
        if (request.Image2 == null || request.Image2.Length == 0)
        {
            return BadRequest(new { message = "Image 2 is required" });
        }
        if (string.IsNullOrWhiteSpace(request.Notes))
        {
            return BadRequest(new { message = "Notes is required" });
        }
        if (string.IsNullOrWhiteSpace(request.KhidmatDone))
        {
            return BadRequest(new { message = "At least one Khidmat is required" });
        }

        try
        {
            // Check if report already exists
            var existingMiqaat = await _miqaatService.GetById(miqaatId);
            if (existingMiqaat == null)
            {
                return NotFound(new { message = "Miqaat not found" });
            }

            // Check if report already submitted by checking if images or notes exist
            var hasExistingReport = await _miqaatService.HasExistingReport(miqaatId);
            if (hasExistingReport)
            {
                return BadRequest(new { message = "Report already submitted for this miqaat" });
            }

            string? image1FileName = null;
            string? image2FileName = null;

            // Save images
            image1FileName = await SaveUploadedImage(request.Image1, miqaatId, 1);
            image2FileName = await SaveUploadedImage(request.Image2, miqaatId, 2);

            await _miqaatService.UpdateMiqaatReport(miqaatId, image1FileName, image2FileName, request.Notes, request.KhidmatDone);
            return Ok(new { message = "Report submitted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<string> SaveUploadedImage(IFormFile file, long miqaatId, int imageNumber)
    {
        // Get file extension from the uploaded file
        var extension = Path.GetExtension(file.FileName)?.ToLower() ?? ".jpg";
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var fileName = $"miqaat_{miqaatId}_img{imageNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        
        // Save to the specified path
        var uploadsPath = @"C:\var\www\bgp_uploads\miqaat_images";
        Directory.CreateDirectory(uploadsPath);
        
        var filePath = Path.Combine(uploadsPath, fileName);
        
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        
        return fileName;
    }
}

