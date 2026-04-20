using Asp.Versioning;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Services;
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

    public QardanHasanaController(IQardanHasanaService service)
    {
        _service = service;
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
    /// Get all Qardan Hasana applications (Admin sees all, Captain sees jamaat, Member sees own)
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

            // Regular member sees own applications
            var myApps = await _service.GetMyApplications(CurrentUser.id);
            return Ok(myApps);
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

            // Check access: Admin can see all, Captain can see jamaat, Member can see own
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
            if (formImage == null || formImage.Length == 0)
                return BadRequest(new { message = "Form image is required for sanctioning" });

            // Save admin signature
            string? adminSignatureUrl = null;
            if (adminSignature != null && adminSignature.Length > 0)
            {
                adminSignatureUrl = await SaveUploadedFile(adminSignature, id, "admin_sign");
            }

            // Save form image
            var formImageUrl = await SaveUploadedFile(formImage, id, "form");

            await _service.Sanction(id, request, adminSignatureUrl, formImageUrl, CurrentUser.id);
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
    /// Get captain of current user's jamaat
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

    [HttpPut("{id}/captain-approve")]
    public async Task<IActionResult> CaptainApprove(int id)
    {
        if (CurrentUser == null)
            return Unauthorized();

        // Only Captains (role=2) can approve
        if (CurrentUser.roles != 2)
            return Forbid();

        try
        {
            await _service.CaptainApprove(id, CurrentUser.id);
            return Ok(new { message = "Application approved by Captain successfully." });
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

    #endregion
}
