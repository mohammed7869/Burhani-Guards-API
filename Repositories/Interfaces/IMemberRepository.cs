using BurhaniGuards.Api.Domain;

namespace BurhaniGuards.Api.Repositories.Interfaces;

public interface IMemberRepository
{
    Task<MemberDocument?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Member?> GetByItsIdAsync(string itsId, CancellationToken cancellationToken = default);
    Task<bool> UpdateNewPasswordAsync(string itsId, string newPasswordHash, CancellationToken cancellationToken = default);
}

