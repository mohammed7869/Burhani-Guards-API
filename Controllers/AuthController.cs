using Asp.Versioning;
using BurhaniGuards.Api.Constants;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Services;
using BurhaniGuards.Api.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BurhaniGuards.Api.Controllers;

[Route("api/{v:apiVersion}")]
[ApiController]
[ApiVersion("1.0")]
public class AuthController : BaseController
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public AuthController(
        IUserService userService,
        ITokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Password is required" });
        }

        if (string.IsNullOrWhiteSpace(request.ItsNumber))
        {
            return BadRequest(new { message = "ITS Number is required" });
        }

        var user = await _userService.Login(request.ItsNumber, request.Password);
        if (user == null)
        {
            
            return BadRequest(new { message = "Invalid ITS Number or password" });
        }

        var token = _tokenService.GenerateToken(user.itsId ?? user.email, GetRoleFromRank(user.rank, user.roles));
        var requiresPasswordChange = string.IsNullOrWhiteSpace(user.newPasswordHash);
        var hasNewPasswordHash = !string.IsNullOrWhiteSpace(user.newPasswordHash);
        
        var auth = new AuthResponse(
            user.id,
            user.profile,
            user.itsId,
            user.fullName,
            user.email,
            user.rank,
            user.roles,
            user.jamiyat,
            user.jamaat,
            user.gender,
            user.age,
            user.contact,
            GetRoleFromRank(user.rank, user.roles),
            token,
            requiresPasswordChange,
            hasNewPasswordHash
        );

        return Ok(auth);
    }

    [AllowAnonymous]
    [HttpPost("admin/login")]
    public async Task<IActionResult> AdminLogin([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Password is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        try
        {
            var user = await _userService.LoginByEmail(request.Email, request.Password);
            
            if (user == null)
            {
                return BadRequest(new { message = "Invalid email or password" });
            }

            // Check if user has ResourceAdmin role (role = 7)
            if (user.roles != MemberRank.ResourceAdmin)
            {
                return BadRequest(new { message = "Access denied. Only Resource Admin can login to the Admin Panel." });
            }

            var token = _tokenService.GenerateToken(user.email ?? user.itsId, GetRoleFromRank(user.rank, user.roles));
            var requiresPasswordChange = string.IsNullOrWhiteSpace(user.newPasswordHash);
            var hasNewPasswordHash = !string.IsNullOrWhiteSpace(user.newPasswordHash);
            
            var auth = new AuthResponse(
                user.id,
                user.profile,
                user.itsId,
                user.fullName,
                user.email,
                user.rank,
                user.roles,
                user.jamiyat,
                user.jamaat,
                user.gender,
                user.age,
                user.contact,
                GetRoleFromRank(user.rank, user.roles),
                token,
                requiresPasswordChange,
                hasNewPasswordHash
            );

            return Ok(auth);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("captain/login")]
    public async Task<IActionResult> CaptainLogin([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Password is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        try
        {
            var user = await _userService.LoginByEmail(request.Email, request.Password);
            
            if (user == null)
            {
                return BadRequest(new { message = "Invalid email or password" });
            }

            // Check if user has ResourceAdmin role (role = 7)
            // Only Resource Admin (role = 7) can login to the Admin Panel
            if (user.roles != MemberRank.ResourceAdmin)
            {
                return BadRequest(new { message = "Access denied. Only Resource Admin can login to the Admin Panel." });
            }

            var token = _tokenService.GenerateToken(user.email ?? user.itsId, GetRoleFromRank(user.rank, user.roles));
            var requiresPasswordChange = string.IsNullOrWhiteSpace(user.newPasswordHash);
            var hasNewPasswordHash = !string.IsNullOrWhiteSpace(user.newPasswordHash);
            
            var auth = new AuthResponse(
                user.id,
                user.profile,
                user.itsId,
                user.fullName,
                user.email,
                user.rank,
                user.roles,
                user.jamiyat,
                user.jamaat,
                user.gender,
                user.age,
                user.contact,
                GetRoleFromRank(user.rank, user.roles),
                token,
                requiresPasswordChange,
                hasNewPasswordHash
            );

            return Ok(auth);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _userService.ChangePassword(request);

        return result
            ? Ok(new { message = "Password changed successfully" })
            : BadRequest(new { message = "Failed to change password. Please check your input." });
    }

    [Authorize]
    [HttpGet("user-profile")]
    public async Task<IActionResult> UserProfile()
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        var user = await _userService.GetProfile(CurrentUser);
        return Ok(user);
    }

    [Authorize]
    [HttpPost("update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserEditViewModel viewmodel)
    {
        if (CurrentUser == null)
        {
            return Unauthorized();
        }

        viewmodel.id = CurrentUser.id;
        await _userService.EditProfile(viewmodel);
        return Ok();
    }

    private string GetRoleFromRank(string rank, int? roles)
    {
        if (roles.HasValue)
        {
            return MemberRank.GetRankText(roles.Value).ToLower().Replace(" ", "-");
        }
        
        return rank?.ToLower().Replace(" ", "-") ?? "member";
    }
}
