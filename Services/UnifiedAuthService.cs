using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Repositories.Interfaces;
using BurhaniGuards.Api.Constants;

namespace BurhaniGuards.Api.Services;

public sealed class UnifiedAuthService : IUnifiedAuthService
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberSnapshotRepository _snapshotRepository;
    private readonly ITokenService _tokenService;

    public UnifiedAuthService(
        IMemberRepository memberRepository,
        IMemberSnapshotRepository snapshotRepository,
        ITokenService tokenService)
    {
        _memberRepository = memberRepository;
        _snapshotRepository = snapshotRepository;
        _tokenService = tokenService;
    }

    public async Task<MemberAuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ItsNumber) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var itsId = request.ItsNumber.Trim();
        var member = await _memberRepository.GetByItsIdAsync(itsId, cancellationToken);
        
        if (member is null || !member.IsActive)
        {
            return null;
        }

        // Check if new password exists - if yes, use new password; otherwise use temporary password
        bool requiresPasswordChange = string.IsNullOrWhiteSpace(member.NewPasswordHash);

        string passwordToVerify = member.PasswordHash ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(member.NewPasswordHash))
        {
            // New password exists - verify against new password
            passwordToVerify = member.NewPasswordHash;
            requiresPasswordChange = false;
        }

        if (string.IsNullOrWhiteSpace(passwordToVerify) || !BCrypt.Net.BCrypt.Verify(request.Password, passwordToVerify))
        {
            return null;
        }

        // Get role text from roles ID or rank
        string role = member.Rank.ToLowerInvariant();
        if (member.Roles.HasValue)
        {
            role = MemberRank.GetRankText(member.Roles.Value).ToLowerInvariant();
        }

        // Create snapshot
        await _snapshotRepository.UpsertAsync(member.Email, member.FullName, role, cancellationToken);
        
        // Generate token
        var token = _tokenService.GenerateToken(member.ItsId, role);

        // Return full member data
        return new MemberAuthResponse(
            Id: member.Id,
            Profile: member.Profile,
            ItsId: member.ItsId,
            Rank: member.Rank,
            Roles: member.Roles,
            Jamiyat: member.Jamiyat,
            Jamaat: member.Jamaat,
            FullName: member.FullName,
            Gender: member.Gender,
            Email: member.Email,
            Age: member.Age,
            Contact: member.Contact,
            Role: role,
            Token: token,
            RequiresPasswordChange: requiresPasswordChange
        );
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ItsNumber) || 
            string.IsNullOrWhiteSpace(request.NewPassword) || 
            request.NewPassword != request.ConfirmPassword)
        {
            return false;
        }

        var itsId = request.ItsNumber.Trim();
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        return await _memberRepository.UpdateNewPasswordAsync(itsId, newPasswordHash, cancellationToken);
    }
}

