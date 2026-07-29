using Asp.Versioning;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Services;
using BurhaniGuards.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BurhaniGuards.Api.Controllers;

[Route("api/{v:apiVersion}/qardan-hasana")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class QardanHasanaController : BaseController
{
    private readonly IQardanHasanaService _service;
    private readonly IQardanRepaymentService _repaymentService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;

    public QardanHasanaController(
        IQardanHasanaService service, 
        IQardanRepaymentService repaymentService,
        INotificationService notificationService,
        IUserRepository userRepository)
    {
        _service = service;
        _repaymentService = repaymentService;
        _notificationService = notificationService;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Submit a new Qardan Hasana application (Member)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQardanHasanaRequest request)
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            var response = await _service.Create(request, CurrentUser);

            // Notify both guarantors
            try
            {
                var msg = $"{request.ApplicantName} has requested a Qardan of ₹{request.AmountRequested} and added you as a guarantor. Please review.";
                
                var guarantors = new List<int>();
                if (request.Guarantor1MemberId > 0) guarantors.Add(request.Guarantor1MemberId);
                if (request.GuarantorMemberId > 0) guarantors.Add(request.GuarantorMemberId);

                if (guarantors.Any())
                {
                    await _notificationService.SendToUsersAsync(
                        guarantors.Distinct(),
                        "Qardan Guarantor Request",
                        msg,
                        "qardan",
                        response.Id.ToString());
                }
            }
            catch { /* Ignore notification error */ }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Check if the current user can apply for Qardan Hasana
    /// </summary>
    [HttpGet("can-apply")]
    public async Task<IActionResult> CanApply()
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            var hasActive = await _service.HasActiveApplication(CurrentUser.id);
            return Ok(new { canApply = !hasActive });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all Qardan Hasana applications (Admin sees all, Captain sees jamaat, Member sees own + guarantor)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null)
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            // Resource Admin (7) sees all
            if (CurrentUser.roles == 7)
            {
                var all = await _service.GetAll(status);
                return Ok(all);
            }

            // Captain (2) sees jamaat applications
            if (CurrentUser.roles == 2)
            {
                var jamaatApps = await _service.GetByJamaat(CurrentUser.jamaat ?? "");
                return Ok(jamaatApps);
            }

            // Regular member sees own applications + applications where they are guarantor
            var myApps = await _service.GetMyApplications(CurrentUser.id);
            var guarantorApps = await _service.GetGuarantorApplications(CurrentUser.id);

            // Merge and deduplicate
            var allApps = myApps.ToList();
            foreach (var gApp in guarantorApps)
            {
                if (!allApps.Any(a => a.Id == gApp.Id))
                    allApps.Add(gApp);
            }

            return Ok(allApps.OrderByDescending(a => a.CreatedAt));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get current user's applications
    /// </summary>
    [HttpGet("my-applications")]
    public async Task<IActionResult> GetMyApplications()
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            var applications = await _service.GetMyApplications(CurrentUser.id);
            return Ok(applications);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get applications where the current user is a guarantor
    /// </summary>
    [HttpGet("my-guarantor-applications")]
    public async Task<IActionResult> GetGuarantorApplications()
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            var applications = await _service.GetGuarantorApplications(CurrentUser.id);
            return Ok(applications);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get single application by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            var application = await _service.GetById(id);
            if (application == null)
                return NotFound(new { message = "Application not found" });

            // Check access: Admin can see all, Captain can see jamaat, Member can see own or guarantor
            if (CurrentUser.roles != 7 &&
                CurrentUser.roles != 2 &&
                application.ApplicantMemberId != CurrentUser.id &&
                application.GuarantorMemberId != CurrentUser.id &&
                application.CaptainMemberId != CurrentUser.id)
            {
                return Forbid("You do not have permission to view this application");
            }

            return Ok(application);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin sanctions the application (Office Use Only)
    /// </summary>
    [HttpPut("{id:int}/sanction")]
    public async Task<IActionResult> Sanction(int id, [FromForm] SanctionQardanHasanaRequest request,
        [FromForm] IFormFile? adminSignature, [FromForm] IFormFile formImage)
    {
        if (CurrentUser == null)
            return Unauthorized();

        // Only Resource Admin (7) can sanction
        if (CurrentUser.roles != 7)
            return Forbid("Only Resource Admin can sanction applications");

        try
        {
            // Save admin signature
            string? adminSignatureUrl = null;
            if (adminSignature != null && adminSignature.Length > 0)
            {
                adminSignatureUrl = await SaveUploadedFile(adminSignature, id, "admin_sign");
            }

            // Save form image (optional)
            string? formImageUrl = null;
            if (formImage != null && formImage.Length > 0)
            {
                formImageUrl = await SaveUploadedFile(formImage, id, "form");
            }

            await _service.Sanction(id, request, adminSignatureUrl, formImageUrl, CurrentUser.id);

            // Notify Applicant, Guarantors, and Captain
            try
            {
                var application = await _service.GetById(id);
                if (application != null)
                {
                    var userIdsToNotify = new List<int>();
                    
                    if (application.ApplicantMemberId > 0) userIdsToNotify.Add(application.ApplicantMemberId);
                    if (application.CaptainMemberId > 0) userIdsToNotify.Add(application.CaptainMemberId); // Guarantor 1
                    if (application.GuarantorMemberId > 0) userIdsToNotify.Add(application.GuarantorMemberId); // Guarantor 2
                    
                    // Need to get Captain of the jamaat
                    var captain = await _userRepository.GetCaptainByJamaatAsync(application.ApplicantJamaat ?? "");
                    if (captain != null)
                    {
                        userIdsToNotify.Add((int)captain.Id);
                    }

                    if (userIdsToNotify.Any())
                    {
                        await _notificationService.SendToUsersAsync(
                            userIdsToNotify.Distinct(),
                            "Qardan Sanctioned",
                            $"Qardan application for {application.ApplicantName} has been sanctioned by Admin for ₹{request.SanctionedAmount}.",
                            "qardan",
                            id.ToString());
                    }
                }
            }
            catch { /* Ignore notification error */ }

            return Ok(new { message = "Application sanctioned successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin rejects the application
    /// </summary>
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectQardanHasanaRequest request)
    {
        if (CurrentUser == null)
            return Unauthorized();

        // Only Resource Admin (7) can reject
        if (CurrentUser.roles != 7)
            return Forbid("Only Resource Admin can reject applications");

        try
        {
            await _service.Reject(id, request, CurrentUser.id);

            // Notify Applicant
            try
            {
                var application = await _service.GetById(id);
                if (application != null && application.ApplicantMemberId > 0)
                {
                    await _notificationService.SendToUserAsync(
                        application.ApplicantMemberId,
                        "Qardan Rejected",
                        $"Your Qardan application was rejected by Admin. Reason: {request.Reason}",
                        "qardan",
                        id.ToString());
                }
            }
            catch { /* Ignore notification error */ }

            return Ok(new { message = "Application rejected" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get members from same jamaat for guarantor dropdown
    /// </summary>
    [HttpGet("members-by-jamaat")]
    public async Task<IActionResult> GetMembersByJamaat()
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            if (string.IsNullOrWhiteSpace(CurrentUser.jamaat))
                return BadRequest(new { message = "Jamaat not found for current user" });

            var members = await _service.GetMembersByJamaat(CurrentUser.jamaat, CurrentUser.id);
            return Ok(members);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get captain of current user's jamaat (kept for backward compatibility)
    /// </summary>
    [HttpGet("my-captain")]
    public async Task<IActionResult> GetMyCaptain()
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            if (string.IsNullOrWhiteSpace(CurrentUser.jamaat))
                return BadRequest(new { message = "Jamaat not found for current user" });

            var captain = await _service.GetCaptainByJamaat(CurrentUser.jamaat);
            if (captain == null)
                return NotFound(new { message = "No captain found for your Mohallah" });

            return Ok(captain);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Download PDF of the application form
    /// </summary>
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            var pdfBytes = await _service.GeneratePdf(id);
            return File(pdfBytes, "text/html", $"Qardan_Hasana_{id}.html");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Guarantor approves the application (works for both Guarantor 1 and Guarantor 2).
    /// The service determines which guarantor the caller is based on their member ID.
    /// </summary>
    [HttpPut("{id}/guarantor-approve")]
    public async Task<IActionResult> GuarantorApprove(int id)
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            await _service.GuarantorApprove(id, CurrentUser.id);

            // Notify Applicant
            try
            {
                var application = await _service.GetById(id);
                if (application != null && application.ApplicantMemberId > 0)
                {
                    await _notificationService.SendToUserAsync(
                        application.ApplicantMemberId,
                        "Guarantor Approved",
                        $"Your guarantor {CurrentUser.fullName} has approved your Qardan application.",
                        "qardan",
                        id.ToString());
                        
                    // If both guarantors have approved, notify Resource Admins and Applicant
                    if (application.CaptainApproved && application.GuarantorApproved)
                    {
                        var adminIds = await _userRepository.GetAdminUserIdsAsync();
                        if (adminIds.Any())
                        {
                            await _notificationService.SendToUsersAsync(
                                adminIds,
                                "Qardan Ready for Sanction",
                                $"{application.ApplicantName}'s application ({application.ApplicationNo}) has been approved by all guarantors and is ready for sanctioning.",
                                "qardan",
                                id.ToString());
                        }

                        // Notify Applicant that both have approved
                        await _notificationService.SendToUserAsync(
                            application.ApplicantMemberId,
                            "Qardan Ready for Sanction",
                            "Both your guarantors have approved your Qardan application. It is now ready for sanctioning.",
                            "qardan",
                            id.ToString());
                    }
                }
            }
            catch { /* Ignore notification error */ }

            return Ok(new { message = "Application approved successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Kept for backward compatibility — redirects to GuarantorApprove
    /// </summary>
    [HttpPut("{id}/captain-approve")]
    public async Task<IActionResult> CaptainApprove(int id)
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            return await GuarantorApprove(id);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Guarantor rejects the application
    /// </summary>
    [HttpPut("{id}/guarantor-reject")]
    public async Task<IActionResult> GuarantorReject(int id, [FromBody] GuarantorRejectRequest request)
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            await _service.GuarantorReject(id, CurrentUser.id, request.Reason);

            // Notify Applicant
            try
            {
                var application = await _service.GetById(id);
                if (application != null && application.ApplicantMemberId > 0)
                {
                    await _notificationService.SendToUserAsync(
                        application.ApplicantMemberId,
                        "Guarantor Rejected",
                        $"Your Qardan application was rejected by a guarantor. Reason: {request.Reason}",
                        "qardan",
                        id.ToString());
                }
            }
            catch { /* Ignore notification error */ }

            return Ok(new { message = "Application rejected by guarantor." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Applicant or Guarantor edits an application (only before guarantor approvals)
    /// </summary>
    [HttpPut("{id:int}/edit")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateQardanHasanaRequest request)
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            var response = await _service.UpdateApplication(id, request, CurrentUser);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get repayment summary + payment history for an application.
    /// Accessible by Admin, Captain, or the applicant / assigned guarantors.
    /// </summary>
    [HttpGet("{id:int}/repayments")]
    public async Task<IActionResult> GetRepayments(int id)
    {
        if (CurrentUser == null)
            return Unauthorized();

        try
        {
            var application = await _service.GetById(id);
            if (application == null)
                return NotFound(new { message = "Application not found" });

            // Same access rule as GetById
            if (CurrentUser.roles != 7 &&
                CurrentUser.roles != 2 &&
                application.ApplicantMemberId != CurrentUser.id &&
                application.GuarantorMemberId != CurrentUser.id &&
                application.CaptainMemberId != CurrentUser.id)
            {
                return Forbid("You do not have permission to view this application");
            }

            var summary = await _repaymentService.GetSummary(id);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Record a repayment / installment payment (Resource Admin only).
    /// Accepts an optional receipt/screenshot image via multipart form-data.
    /// </summary>
    [HttpPost("{id:int}/repayments")]
    public async Task<IActionResult> RecordRepayment(int id, [FromForm] RecordRepaymentRequest request,
        [FromForm] IFormFile? receiptImage)
    {
        if (CurrentUser == null)
            return Unauthorized();

        // Only Resource Admin (7) can record repayments
        if (CurrentUser.roles != 7)
            return Forbid("Only Resource Admin can record repayments");

        try
        {
            string? receiptImageUrl = null;
            if (receiptImage != null && receiptImage.Length > 0)
            {
                receiptImageUrl = await SaveRepaymentReceipt(receiptImage, id);
            }

            var response = await _repaymentService.RecordRepayment(id, request, receiptImageUrl, CurrentUser);

            // Notify Applicant
            try
            {
                var application = await _service.GetById(id);
                if (application != null && application.ApplicantMemberId > 0)
                {
                    await _notificationService.SendToUserAsync(
                        application.ApplicantMemberId,
                        "Repayment Recorded",
                        $"A repayment of ₹{request.AmountPaid} has been recorded for your Qardan.",
                        "qardan",
                        id.ToString());
                }
            }
            catch { /* Ignore notification error */ }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a recorded repayment (Resource Admin only, for corrections).
    /// </summary>
    [HttpDelete("{id:int}/repayments/{repaymentId:int}")]
    public async Task<IActionResult> DeleteRepayment(int id, int repaymentId)
    {
        if (CurrentUser == null)
            return Unauthorized();

        if (CurrentUser.roles != 7)
            return Forbid("Only Resource Admin can delete repayments");

        try
        {
            await _repaymentService.DeleteRepayment(id, repaymentId, CurrentUser);
            return Ok(new { message = "Repayment deleted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #region Private Helpers

    private async Task<string> SaveUploadedFile(IFormFile file, int applicationId, string prefix)
    {
        var extension = Path.GetExtension(file.FileName)?.ToLower() ?? ".jpg";
        if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";

        var fileName = $"qardan_{applicationId}_{prefix}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";

        var uploadsPath = @"C:\var\www\bgp_uploads\qardan_images";
        Directory.CreateDirectory(uploadsPath);

        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return fileName;
    }

    /// <summary>
    /// Saves a repayment receipt/screenshot and returns the relative path
    /// (e.g. "bgp_uploads/qardan_repayments/qardan_8_receipt_20260712120000.jpg")
    /// so it can be served directly via the static file middleware.
    /// </summary>
    private async Task<string> SaveRepaymentReceipt(IFormFile file, int applicationId)
    {
        var extension = Path.GetExtension(file.FileName)?.ToLower() ?? ".jpg";
        if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";

        var fileName = $"qardan_{applicationId}_receipt_{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}";

        var uploadsPath = @"C:\var\www\bgp_uploads\qardan_repayments";
        Directory.CreateDirectory(uploadsPath);

        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"bgp_uploads/qardan_repayments/{fileName}";
    }

    #endregion
}
