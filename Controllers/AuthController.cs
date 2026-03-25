using Asp.Versioning;
using BurhaniGuards.Api.Constants;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Repositories;
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
    private readonly IEmailService _emailService;
    private readonly IDapperMemberRepository _memberRepository;

    public AuthController(
        IUserService userService,
        ITokenService tokenService,
        IEmailService emailService,
        IDapperMemberRepository memberRepository)
    {
        _userService = userService;
        _tokenService = tokenService;
        _emailService = emailService;
        _memberRepository = memberRepository;
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

        var requiresPasswordChange = string.IsNullOrWhiteSpace(user.newPasswordHash);
        var hasNewPasswordHash = !string.IsNullOrWhiteSpace(user.newPasswordHash);
        
        var currentUser = new CurrentUserViewModel
        {
            id = user.id,
            itsId = user.itsId,
            fullName = user.fullName,
            email = user.email,
            rank = user.rank,
            roles = user.roles,
            jamiyat = user.jamiyat,
            jamaat = user.jamaat,
            requiresPasswordChange = requiresPasswordChange
        };
        var token = _tokenService.GenerateToken(currentUser, GetRoleFromRank(user.rank, user.roles));
        
        var auth = new AuthResponse(
            user.id,
            user.profile,
            user.itsId,
            user.fullName,
            user.email ?? string.Empty,
            user.rank,
            user.roles,
            user.jamiyat,
            user.jamaat,
            user.gender,
            user.age,
            user.contact,
            user.dateOfBirth,
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

            var requiresPasswordChange = string.IsNullOrWhiteSpace(user.newPasswordHash);
            var hasNewPasswordHash = !string.IsNullOrWhiteSpace(user.newPasswordHash);
            
            var currentUser = new CurrentUserViewModel
            {
                id = user.id,
                itsId = user.itsId,
                fullName = user.fullName,
                email = user.email,
                rank = user.rank,
                roles = user.roles,
                jamiyat = user.jamiyat,
                jamaat = user.jamaat,
                requiresPasswordChange = requiresPasswordChange
            };
            var token = _tokenService.GenerateToken(currentUser, GetRoleFromRank(user.rank, user.roles));
            
            var auth = new AuthResponse(
                user.id,
                user.profile,
                user.itsId,
                user.fullName,
                user.email ?? string.Empty,
                user.rank,
                user.roles,
                user.jamiyat,
                user.jamaat,
                user.gender,
                user.age,
                user.contact,
                user.dateOfBirth,
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

            var requiresPasswordChange = string.IsNullOrWhiteSpace(user.newPasswordHash);
            var hasNewPasswordHash = !string.IsNullOrWhiteSpace(user.newPasswordHash);
            
            var currentUser = new CurrentUserViewModel
            {
                id = user.id,
                itsId = user.itsId,
                fullName = user.fullName,
                email = user.email,
                rank = user.rank,
                roles = user.roles,
                jamiyat = user.jamiyat,
                jamaat = user.jamaat,
                requiresPasswordChange = requiresPasswordChange
            };
            var token = _tokenService.GenerateToken(currentUser, GetRoleFromRank(user.rank, user.roles));
            
            var auth = new AuthResponse(
                user.id,
                user.profile,
                user.itsId,
                user.fullName,
                user.email ?? string.Empty,
                user.rank,
                user.roles,
                user.jamiyat,
                user.jamaat,
                user.gender,
                user.age,
                user.contact,
                user.dateOfBirth,
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

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ItsNumber))
        {
            return BadRequest(new ForgotPasswordResponse(false, "ITS Number is required", ""));
        }

        try
        {
            var member = await _memberRepository.GetByItsIdOrNull(request.ItsNumber);
            
            if (member == null)
            {
                return BadRequest(new ForgotPasswordResponse(false, "No account found with this ITS Number", ""));
            }

            if (string.IsNullOrWhiteSpace(member.Email))
            {
                return BadRequest(new ForgotPasswordResponse(false, "No email address associated with this account", ""));
            }

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var validTill = DateTime.UtcNow.AddMinutes(5);

            // Save OTP to database
            await _memberRepository.SaveOtp(member.Id, otp, validTill);

            // Mask email for display (e.g., m***@gmail.com)
            var maskedEmail = MaskEmail(member.Email);

            // Send OTP via email
            var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color: #2e7d32;'>Password Reset OTP</h2>
                    <p>Dear {member.FullName},</p>
                    <p>You have requested to reset your password for Burhani Guards Pune.</p>
                    <p>Your OTP is: <strong style='font-size: 24px; color: #1976d2;'>{otp}</strong></p>
                    <p>This OTP is valid for 5 minutes.</p>
                    <p>If you did not request this, please ignore this email.</p>
                    <br/>
                    <p>Regards,<br/>Burhani Guards Pune</p>
                </body>
                </html>";

            await _emailService.SendEmailAsync(member.Email, "Password Reset OTP - Burhani Guards", emailBody);

            return Ok(new ForgotPasswordResponse(true, "OTP sent successfully", maskedEmail));
        }
        catch (Exception ex)
        {
            return BadRequest(new ForgotPasswordResponse(false, ex.Message, ""));
        }
    }

    [AllowAnonymous]
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ItsNumber))
        {
            return BadRequest(new VerifyOtpResponse(false, "ITS Number is required"));
        }

        if (string.IsNullOrWhiteSpace(request.OtpCode))
        {
            return BadRequest(new VerifyOtpResponse(false, "OTP is required"));
        }

        try
        {
            var isValid = await _memberRepository.VerifyOtp(request.ItsNumber, request.OtpCode);

            if (!isValid)
            {
                return BadRequest(new VerifyOtpResponse(false, "Invalid or expired OTP"));
            }

            return Ok(new VerifyOtpResponse(true, "OTP verified successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(new VerifyOtpResponse(false, ex.Message));
        }
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ItsNumber))
        {
            return BadRequest(new ResetPasswordResponse(false, "ITS Number is required"));
        }

        if (string.IsNullOrWhiteSpace(request.OtpCode))
        {
            return BadRequest(new ResetPasswordResponse(false, "OTP is required"));
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new ResetPasswordResponse(false, "New password is required"));
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new ResetPasswordResponse(false, "Passwords do not match"));
        }

        if (request.NewPassword.Length < 6)
        {
            return BadRequest(new ResetPasswordResponse(false, "Password must be at least 6 characters"));
        }

        try
        {
            // Verify OTP first
            var isValid = await _memberRepository.VerifyOtp(request.ItsNumber, request.OtpCode);

            if (!isValid)
            {
                return BadRequest(new ResetPasswordResponse(false, "Invalid or expired OTP"));
            }

            // Get member
            var member = await _memberRepository.GetByItsIdOrNull(request.ItsNumber);
            if (member == null)
            {
                return BadRequest(new ResetPasswordResponse(false, "Member not found"));
            }

            // Hash new password and update
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _memberRepository.ResetPassword(member.Id, hashedPassword);

            return Ok(new ResetPasswordResponse(true, "Password reset successfully. Please login with your new password."));
        }
        catch (Exception ex)
        {
            return BadRequest(new ResetPasswordResponse(false, ex.Message));
        }
    }

    private string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            return email;

        var parts = email.Split('@');
        var username = parts[0];
        var domain = parts[1];

        if (username.Length <= 2)
            return $"{username[0]}***@{domain}";

        return $"{username[0]}{username[1]}***@{domain}";
    }
}
