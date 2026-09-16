using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Domain;

namespace BurhaniGuards.Api.Services;

public interface IJamaatTransferService
{
    Task<int> InitiateTransferAsync(InitiateJamaatTransferRequest request, int currentUserId);
    Task<bool> AcceptTransferAsync(int id, int currentUserId);
    Task<bool> ApproveTransferAsync(int id, int currentUserId);
    Task<bool> RejectTransferAsync(int id, RejectJamaatTransferRequest request, int currentUserId);
    Task<IEnumerable<JamaatTransfer>> GetByMemberIdAsync(int memberId);
    Task<IEnumerable<JamaatTransfer>> GetPendingByFromJamaatIdAsync(int fromJamaatId);
    Task<IEnumerable<JamaatTransfer>> GetPendingByToJamaatIdAsync(int toJamaatId);
    Task<IEnumerable<JamaatTransfer>> GetAllByStatusAsync(string status);
    Task<IEnumerable<JamaatTransfer>> GetHistoryByJamaatIdAsync(int jamaatId);
    Task<IEnumerable<JamaatTransfer>> GetHistoryAsync();
    Task<JamaatTransfer?> GetByIdAsync(int id);
}
