using BurhaniGuards.Api.Domain;

namespace BurhaniGuards.Api.Repositories.Interfaces;

public interface ICaptainRepository
{
    Task<CaptainCredential?> GetByItsNumberAsync(string itsNumber, CancellationToken cancellationToken = default);
    Task<bool> CreateAsync(string itsNumber, string name, string passwordHash, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string itsNumber, CancellationToken cancellationToken = default);
    Task<bool> UpdateNewPasswordAsync(string itsNumber, string newPasswordHash, CancellationToken cancellationToken = default);
}

