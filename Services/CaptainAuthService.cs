using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Repositories.Interfaces;

namespace BurhaniGuards.Api.Services;

public sealed class CaptainAuthService : ICaptainAuthService
{
    private readonly ICaptainRepository _captainRepository;
    private readonly ITokenService _tokenService;

    public CaptainAuthService(ICaptainRepository captainRepository, ITokenService tokenService)
    {
        _captainRepository = captainRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        // Use ITS Number if provided, otherwise return null
        if (string.IsNullOrWhiteSpace(request.ItsNumber))
        {
            return null;
        }

        var itsNumber = request.ItsNumber.Trim();
        
        // Only allow the specific captain (ITS 30375370) to login
        if (itsNumber != "30375370")
        {
            return null; // Reject all other captains
        }

        var captain = await _captainRepository.GetByItsNumberAsync(itsNumber, cancellationToken);
        if (captain is null)
        {
            return null;
        }

        // Check if new password exists - if yes, use new password; otherwise use temporary password
        bool passwordValid = false;
        bool requiresPasswordChange = string.IsNullOrWhiteSpace(captain.NewPasswordHash);

        if (!string.IsNullOrWhiteSpace(captain.NewPasswordHash))
        {
            // New password exists - verify against new password
            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, captain.NewPasswordHash);
        }
        else
        {
            // No new password - verify against temporary password
            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, captain.PasswordHash);
            // If using temporary password, password change is required
            requiresPasswordChange = true;
        }

        if (!passwordValid)
        {
            return null;
        }

        var token = _tokenService.GenerateToken(captain.ItsNumber, "captain");
        return new AuthResponse(
            Id: 0,
            Profile: null,
            ItsId: captain.ItsNumber,
            FullName: captain.Name,
            Email: captain.Email,
            Rank: "Captain",
            Roles: null,
            Jamiyat: null,
            Jamaat: null,
            Gender: null,
            Age: null,
            Contact: null,
            Role: "captain",
            Token: token,
            RequiresPasswordChange: requiresPasswordChange
        );
    }

    public async Task<AuthResponse?> SignupAsync(SignupRequest request, CancellationToken cancellationToken = default)
    {
        // Signup is disabled - only one captain is allowed (ITS 30375370)
        // This captain is automatically seeded in the database on startup
        return null;
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ItsNumber) || 
            string.IsNullOrWhiteSpace(request.NewPassword) || 
            string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return false;
        }

        // Verify passwords match
        if (request.NewPassword != request.ConfirmPassword)
        {
            return false;
        }

        // Only allow the specific captain
        if (request.ItsNumber.Trim() != "30375370")
        {
            return false;
        }

        // Verify captain exists
        var captain = await _captainRepository.GetByItsNumberAsync(request.ItsNumber.Trim(), cancellationToken);
        if (captain is null)
        {
            return false;
        }

        // Hash the new password and update
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        return await _captainRepository.UpdateNewPasswordAsync(request.ItsNumber.Trim(), newPasswordHash, cancellationToken);
    }
}

