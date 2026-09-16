using BurhaniGuards.Api.Domain;

namespace BurhaniGuards.Api.Repositories.Interfaces;

public interface IJamaatTransferRepository
{
    Task<int> CreateAsync(JamaatTransfer transfer);
    Task<JamaatTransfer?> GetByIdAsync(int id);
    Task<IEnumerable<JamaatTransfer>> GetByMemberIdAsync(int memberId);
    Task<IEnumerable<JamaatTransfer>> GetPendingByFromJamaatIdAsync(int fromJamaatId);
    Task<IEnumerable<JamaatTransfer>> GetPendingByToJamaatIdAsync(int toJamaatId);
    Task<IEnumerable<JamaatTransfer>> GetAllByStatusAsync(string status);
    Task<IEnumerable<JamaatTransfer>> GetHistoryByJamaatIdAsync(int jamaatId);
    Task<IEnumerable<JamaatTransfer>> GetHistoryAsync();
    Task<bool> UpdateStatusAsync(int id, string status, int actionById, DateTime actionAt, string? rejectionReason = null);
}
