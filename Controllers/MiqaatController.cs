using Asp.Versioning;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Repositories;
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
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepo;

    public MiqaatController(IMiqaatService miqaatService, INotificationService notificationService, IUserRepository userRepo)
    {
        _miqaatService = miqaatService;
        _notificationService = notificationService;
        _userRepo = userRepo;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateMiqaatRequest request)
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
            var notificationImage = await SaveNotificationImage(request.ImageFile);
            var response = await _miqaatService.Create(request, CurrentUser.fullName, notificationImage);

            // Send notification to all members of the jamaat
            try
            {
                await _sendMiqaatCreatedNotification(request, response.Id, notificationImage);
            }
            catch { /* Don't fail miqaat creation if notification fails */ }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("admin-create")]
    public async Task<IActionResult> CreateByAdmin([FromForm] CreateMiqaatRequest request)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            var notificationImage = await SaveNotificationImage(request.ImageFile);
            var response = await _miqaatService.CreateByAdmin(request, CurrentUser.fullName, notificationImage);

            // Send notification to all members of the target jamaat(s)
            try
            {
                await _sendMiqaatCreatedNotification(request, response.Id, notificationImage);
            }
            catch { /* Don't fail miqaat creation if notification fails */ }

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

    [HttpPost("{id:long}/resend-email")]
    public async Task<IActionResult> ResendEmail(long id)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            await _miqaatService.ResendMiqaatEmail(id);
            return Ok(new { message = "Email resent successfully" });
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

        // Captains (2), Admins (4,5,6,7) can view any member's history; members can view their own
        var isAllowedRole = CurrentUser.roles is 2 or 4 or 5 or 6 or 7;
        if (!isAllowedRole && CurrentUser.id != memberId)
        {
            return StatusCode(403, new { message = "You do not have permission to view member attendance history" });
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
            var miqaat = await _miqaatService.GetById(miqaatId);
            if (miqaat != null && miqaat.IsEnrollmentStopped)
            {
                return BadRequest(new { message = "Enrollment for this have been stopped." });
            }

            var days = request.Days?
                .Where(d => d > 0)
                .Distinct()
                .ToList();

            if ((days == null || days.Count == 0) && request.Day.HasValue && request.Day.Value > 0)
            {
                days = new List<int> { request.Day.Value };
            }

            await _miqaatService.UpdateMemberMiqaatStatus(memberId, miqaatId, request.Status, days);

            // Notify Captain when a member enrolls
            if (request.Status?.ToLower() == "enrolled" || request.Status?.ToLower() == "approved")
            {
                try
                {
                    if (miqaat != null)
                    {
                        var captain = await _userRepo.GetCaptainByJamaatAsync(CurrentUser.jamaat ?? "");
                        if (captain != null)
                        {
                            await _notificationService.SendToUserAsync(
                                (int)captain.Id,
                                "Member Enrolled: " + miqaat.MiqaatName,
                                $"{CurrentUser.fullName} has enrolled for '{miqaat.MiqaatName}'. Review their enrollment.",
                                "miqaat",
                                miqaatId.ToString());
                        }
                    }
                }
                catch { /* Don't fail enrollment if notification fails */ }
            }

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

        // Only Captains and Admins can view enrolled members
        if (CurrentUser.roles != 2 && CurrentUser.roles != 7)
        {
            return Forbid("Only Captains or Admins can view enrolled members");
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

        // Only Captains and Admins can view all members
        if (CurrentUser.roles != 2 && CurrentUser.roles != 7)
        {
            return Forbid("Only Captains or Admins can view all members");
        }

        try
        {
            // Captains only see members from their own jamaat; Admins see all
            string? captainJamaat = CurrentUser.roles == 2 ? CurrentUser.jamaat : null;
            var members = await _miqaatService.GetAllMembersByMiqaatId(miqaatId, captainJamaat);
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

        if (miqaat.MiqaatType == "International" || miqaat.IsAdminCreated)
        {
            // Only Admin can view attendance for International / multi-jamaat Local miqaats
            if (CurrentUser.roles != 7)
            {
                return Forbid("Only Admin can view approved members for attendance on this miqaat");
            }
        }
        else
        {
            // Only Captains can view attendance for single-jamaat Local miqaats
            if (CurrentUser.roles != 2)
            {
                return Forbid("Only Captains can view approved members for attendance");
            }
        }

        try
        {
            var members = await _miqaatService.GetApprovedMembersForAttendance(miqaatId, day);
            var windowInfo = _miqaatService.GetAttendanceWindowInfo(miqaatId, miqaat.FromDate, miqaat.TillDate, miqaat.MiqaatDays, day);
            return Ok(new { members, attendanceWindow = windowInfo });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{miqaatId:long}/attendance-window-info")]
    public async Task<IActionResult> GetAttendanceWindowInfo(long miqaatId, [FromQuery] int day = 1)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            var miqaat = await _miqaatService.GetById(miqaatId);
            if (miqaat == null)
            {
                return NotFound(new { message = "Miqaat not found" });
            }

            var windowInfo = _miqaatService.GetAttendanceWindowInfo(miqaatId, miqaat.FromDate, miqaat.TillDate, miqaat.MiqaatDays, day);
            return Ok(windowInfo);
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

            // Notify the member about captain's decision
            try
            {
                var miqaat = await _miqaatService.GetById(miqaatId);
                if (miqaat != null)
                {
                    var statusText = request.Status?.ToLower() == "approved" ? "approved" : "updated";
                    await _notificationService.SendToUserAsync(
                        memberId,
                        $"Enrollment {statusText}: {miqaat.MiqaatName}",
                        $"Your enrollment for '{miqaat.MiqaatName}' has been {statusText} by the Captain.",
                        "miqaat",
                        miqaatId.ToString());
                }
            }
            catch { /* Don't fail status update if notification fails */ }

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

            // Notify the member about admin's decision for international miqaat
            try
            {
                var miqaat = await _miqaatService.GetById(miqaatId);
                if (miqaat != null)
                {
                    var statusText = request.Status?.ToLower() == "approved" ? "approved" : "rejected";
                    await _notificationService.SendToUserAsync(
                        memberId,
                        $"Admin {statusText}: {miqaat.MiqaatName}",
                        $"Your enrollment for '{miqaat.MiqaatName}' has been {statusText} by Admin.",
                        "miqaat",
                        miqaatId.ToString());
                }
            }
            catch { /* Don't fail status update if notification fails */ }

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

        if (miqaat.MiqaatType == "International" || miqaat.IsAdminCreated)
        {
            // Only Admin can mark attendance for International / multi-jamaat Local miqaats
            if (CurrentUser.roles != 7)
            {
                return Forbid("Only Admin can mark attendance for this miqaat");
            }
        }
        else
        {
            // Only Captains can mark attendance for single-jamaat Local miqaats
            if (CurrentUser.roles != 2)
            {
                return Forbid("Only Captains can mark attendance");
            }
        }

        try
        {
            await _miqaatService.MarkAttendanceBatch(miqaatId, request.Day, request.MemberIds);

            // Notify each member that attendance was marked + 2 points allocated
            try
            {
                await _notificationService.SendToUsersAsync(
                    request.MemberIds,
                    "Attendance Marked ✅",
                    $"Your attendance has been marked for Day {request.Day}. +2 points allocated! Tap to view.",
                    "miqaat",
                    miqaatId.ToString());
            }
            catch { /* Don't fail attendance if notification fails */ }

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

    [HttpGet("{miqaatId:long}/day-tracking")]
    public async Task<IActionResult> GetMemberDayTracking(long miqaatId)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        // Only Admin (7) and Captain (2) can view day tracking
        if (CurrentUser.roles != 7 && CurrentUser.roles != 2)
        {
            return Forbid("Only Admin or Captain can view member day tracking");
        }

        try
        {
            var tracking = await _miqaatService.GetMemberDayTrackingAsync(miqaatId);
            return Ok(tracking);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Send miqaat creation notifications to all members of the target jamaat(s).
    /// Handles comma-separated jamaats for International miqaats.
    /// </summary>
    private async Task _sendMiqaatCreatedNotification(CreateMiqaatRequest request, long miqaatId, string? notificationImage = null)
    {
        var miqaatType = request.MiqaatType ?? "Local";
        var title = $"New Miqaat: {request.MiqaatName}";
        var body = $"A new {miqaatType} miqaat '{request.MiqaatName}' has been created. Enroll now!";

        // For International miqaats, Jamaat field is comma-separated
        if (request.Jamaat.Contains(','))
        {
            var jamaats = request.Jamaat
                .Split(',')
                .Select(j => j.Trim())
                .Where(j => !string.IsNullOrEmpty(j))
                .ToList();

            foreach (var jamaat in jamaats)
            {
                try
                {
                    await _notificationService.SendToJamaatAsync(
                        jamaat, title, body, "miqaat", miqaatId.ToString(), notificationImage);
                }
                catch { /* Continue with other jamaats even if one fails */ }
            }
        }
        else
        {
            await _notificationService.SendToJamaatAsync(
                request.Jamaat, title, body, "miqaat", miqaatId.ToString(), notificationImage);
        }
    }

    private async Task<string?> SaveNotificationImage(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var extension = Path.GetExtension(file.FileName)?.ToLower() ?? ".jpg";
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var fileName = $"notification_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}{extension}";
        var uploadsPath = @"C:\var\www\bgp_uploads\notification_images";
        Directory.CreateDirectory(uploadsPath);
        
        var filePath = Path.Combine(uploadsPath, fileName);
        
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        
        // Use an absolute URL since push notifications are viewed outside the web app context.
        // Assuming the base API domain is known, we could ideally pull from configuration.
        // As per the app settings, it might be https://bgp.baawanerp.com
        return $"https://bgp.baawanerp.com/bgp_uploads/notification_images/{fileName}";
    }
}

