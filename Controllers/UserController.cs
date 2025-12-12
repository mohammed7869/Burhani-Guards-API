using Asp.Versioning;
using BurhaniGuards.Api.Services;
using BurhaniGuards.Api.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace BurhaniGuards.Api.Controllers;

[Route("api/{v:apiVersion}/users")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class UserController : BaseController
{
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _environment;
    private const string UploadPath = @"C:\var\www\bgp_uploads";

    public UserController(IUserService userService, IWebHostEnvironment environment)
    {
        _userService = userService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAll();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetById(id);
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] UserCreateViewModel viewmodel)
    {
        var id = await _userService.Add(viewmodel);
        return Ok(new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Edit(int id, [FromBody] UserEditViewModel viewmodel)
    {
        viewmodel.id = id;
        await _userService.Edit(viewmodel);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.Delete(id);
        return Ok();
    }

    [HttpGet("jamiyat-jamaat")]
    public async Task<IActionResult> GetJamiyatJamaatWithCounts()
    {
        try
        {
            var response = await _userService.GetJamiyatJamaatWithCounts();
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/upload-profile")]
    public async Task<IActionResult> UploadProfileImage(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest(new { message = "Invalid file type. Only image files are allowed." });
        }

        // Validate file size (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { message = "File size exceeds 5MB limit" });
        }

        try
        {
            // Ensure upload directory exists
            if (!Directory.Exists(UploadPath))
            {
                Directory.CreateDirectory(UploadPath);
            }

            // Generate unique filename
            var fileName = $"user_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
            var filePath = Path.Combine(UploadPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Relative path for database storage
            var relativePath = $"bgp_uploads/{fileName}";

            // Update user profile in database
            await _userService.UpdateProfileImage(id, relativePath);

            return Ok(new { message = "Profile image uploaded successfully", profile = relativePath });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error uploading file: {ex.Message}" });
        }
    }
}

