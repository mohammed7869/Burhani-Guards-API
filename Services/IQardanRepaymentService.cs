using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.ViewModel;

namespace BurhaniGuards.Api.Services;

public interface IQardanRepaymentService
{
    /// <summary>
    /// Returns the repayment summary (paid / pending / next installment) plus
    /// the full payment history for a Qardan Hasana application.
    /// </summary>
    Task<QardanRepaymentSummaryResponse> GetSummary(int qardanHasanaId);

    /// <summary>
    /// Records a new repayment against a sanctioned application (Admin only).
    /// </summary>
    Task<QardanRepaymentResponse> RecordRepayment(int qardanHasanaId, RecordRepaymentRequest request,
        string? receiptImageUrl, CurrentUserViewModel currentUser);

    /// <summary>
    /// Deletes a recorded repayment (Admin only, for corrections).
    /// </summary>
    Task DeleteRepayment(int qardanHasanaId, int repaymentId, CurrentUserViewModel currentUser);
}
