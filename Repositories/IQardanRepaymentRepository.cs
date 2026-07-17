using BurhaniGuards.Api.Contracts.Responses;

namespace BurhaniGuards.Api.Repositories;

public interface IQardanRepaymentRepository
{
    Task<int> Create(
        int qardanHasanaId, int? installmentNumber, decimal amountPaid,
        DateTime paymentDate, string? paymentMode, string? receiptImageUrl,
        string? notes, int? recordedBy, string? recordedByName);

    Task<List<QardanRepaymentResponse>> GetByQardanId(int qardanHasanaId);
    Task<QardanRepaymentResponse?> GetById(int id);
    Task Delete(int id);
}
