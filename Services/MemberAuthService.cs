using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Repositories.Interfaces;

namespace BurhaniGuards.Api.Services;

public sealed class MemberAuthService : IMemberAuthService
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberSnapshotRepository _snapshotRepository;
    private readonly ITokenService _tokenService;

    public MemberAuthService(
        IMemberRepository memberRepository,
        IMemberSnapshotRepository snapshotRepository,
        ITokenService tokenService)
    {
        _memberRepository = memberRepository;
        _snapshotRepository = snapshotRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var member = await _memberRepository.GetByEmailAsync(email, cancellationToken);
        if (member is null || !member.IsActive)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, member.PasswordHash))
        {
            return null;
        }

        await _snapshotRepository.UpsertAsync(member.Email, member.FullName, member.Role, cancellationToken);
        var token = _tokenService.GenerateToken(member.Email, member.Role);
        return new AuthResponse(
            Id: 0,
            Profile: null,
            ItsId: null,
            FullName: member.FullName,
            Email: member.Email,
            Rank: "Member",
            Roles: null,
            Jamiyat: null,
            Jamaat: null,
            Gender: null,
            Age: null,
            Contact: null,
            Role: member.Role,
            Token: token,
            RequiresPasswordChange: false
        );
    }
}

