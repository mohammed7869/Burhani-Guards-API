using BurhaniGuards.Api.BusinessModel;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Repositories;
using BurhaniGuards.Api.ViewModel;
using System.Text.Json;

namespace BurhaniGuards.Api.Services;

public class QardanRepaymentService : IQardanRepaymentService
{
    private readonly IQardanRepaymentRepository _repository;
    private readonly IQardanHasanaRepository _qardanRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<QardanRepaymentService> _logger;

    public QardanRepaymentService(
        IQardanRepaymentRepository repository,
        IQardanHasanaRepository qardanRepository,
        IActivityLogService activityLogService,
        ILogger<QardanRepaymentService> logger)
    {
        _repository = repository;
        _qardanRepository = qardanRepository;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<QardanRepaymentSummaryResponse> GetSummary(int qardanHasanaId)
    {
        var application = await _qardanRepository.GetById(qardanHasanaId);
        if (application == null)
            throw new Exception("Application not found.");

        var repayments = await _repository.GetByQardanId(qardanHasanaId);
        return BuildSummary(application, repayments);
    }

    public async Task<QardanRepaymentResponse> RecordRepayment(int qardanHasanaId, RecordRepaymentRequest request,
        string? receiptImageUrl, CurrentUserViewModel currentUser)
    {
        var application = await _qardanRepository.GetById(qardanHasanaId);
        if (application == null)
            throw new Exception("Application not found.");

        if (application.Status != "sanctioned")
            throw new Exception("Repayments can only be recorded for a sanctioned application.");

        if (request.AmountPaid <= 0)
            throw new Exception("Payment amount must be greater than zero.");

        if (request.PaymentDate == default)
            throw new Exception("Payment date is required.");

        // Guard against recording more than the outstanding balance
        var existing = await _repository.GetByQardanId(qardanHasanaId);
        var alreadyPaid = existing.Sum(r => r.AmountPaid);
        var sanctioned = application.SanctionedAmount ?? 0;
        if (sanctioned > 0 && alreadyPaid + request.AmountPaid > sanctioned + 0.01m)
        {
            var remaining = sanctioned - alreadyPaid;
            throw new Exception($"Payment exceeds the outstanding balance. Remaining amount is ₹{(remaining < 0 ? 0 : remaining):N2}.");
        }

        var id = await _repository.Create(
            qardanHasanaId: qardanHasanaId,
            installmentNumber: request.InstallmentNumber,
            amountPaid: request.AmountPaid,
            paymentDate: request.PaymentDate,
            paymentMode: request.PaymentMode,
            receiptImageUrl: receiptImageUrl,
            notes: request.Notes,
            recordedBy: currentUser.id,
            recordedByName: currentUser.fullName);

        _logger.LogInformation("Qardan Hasana repayment recorded for {AppNo} by Admin {AdminId}. Amount: {Amount}",
            application.ApplicationNo, currentUser.id, request.AmountPaid);

        // Activity Log (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await _activityLogService.LogAsync(new ActivityLogModel
                {
                    EntityType = ActivityEntityType.QardanHasana,
                    EntityId = qardanHasanaId,
                    Action = ActivityAction.QardanHasanaRepaymentRecorded,
                    PerformedBy = currentUser.fullName,
                    PerformedById = currentUser.id,
                    PerformedByRole = "Resource Admin",
                    TargetMemberId = application.ApplicantMemberId,
                    TargetMemberName = $"{application.ApplicantName} (ITS: {application.ApplicantItsId})",
                    NewValue = $"₹{request.AmountPaid:N0}",
                    Details = JsonSerializer.Serialize(new
                    {
                        applicationNo = application.ApplicationNo,
                        applicantName = application.ApplicantName,
                        amountPaid = request.AmountPaid,
                        installmentNumber = request.InstallmentNumber,
                        paymentMode = request.PaymentMode,
                        paymentDate = request.PaymentDate.ToString("yyyy-MM-dd")
                    }),
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to log Qardan Hasana repayment activity for {AppNo}: {Error}",
                    application.ApplicationNo, ex.Message);
            }
        });

        var created = await _repository.GetById(id);
        return created!;
    }

    public async Task DeleteRepayment(int qardanHasanaId, int repaymentId, CurrentUserViewModel currentUser)
    {
        var repayment = await _repository.GetById(repaymentId);
        if (repayment == null || repayment.QardanHasanaId != qardanHasanaId)
            throw new Exception("Repayment record not found.");

        await _repository.Delete(repaymentId);

        _logger.LogInformation("Qardan Hasana repayment {RepaymentId} deleted by Admin {AdminId}",
            repaymentId, currentUser.id);

        var application = await _qardanRepository.GetById(qardanHasanaId);

        _ = Task.Run(async () =>
        {
            try
            {
                await _activityLogService.LogAsync(new ActivityLogModel
                {
                    EntityType = ActivityEntityType.QardanHasana,
                    EntityId = qardanHasanaId,
                    Action = ActivityAction.QardanHasanaRepaymentDeleted,
                    PerformedBy = currentUser.fullName,
                    PerformedById = currentUser.id,
                    PerformedByRole = "Resource Admin",
                    TargetMemberId = application?.ApplicantMemberId,
                    TargetMemberName = application != null
                        ? $"{application.ApplicantName} (ITS: {application.ApplicantItsId})"
                        : null,
                    OldValue = $"₹{repayment.AmountPaid:N0}",
                    Details = JsonSerializer.Serialize(new
                    {
                        applicationNo = application?.ApplicationNo,
                        repaymentId,
                        amountPaid = repayment.AmountPaid
                    }),
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to log Qardan Hasana repayment deletion for {RepaymentId}: {Error}",
                    repaymentId, ex.Message);
            }
        });
    }

    /// <summary>
    /// Builds the paid / pending / next-installment summary from the application
    /// schedule and the recorded payments.
    /// </summary>
    private static QardanRepaymentSummaryResponse BuildSummary(
        QardanHasanaResponse application, List<QardanRepaymentResponse> repayments)
    {
        var sanctioned = application.SanctionedAmount ?? 0m;
        var installmentAmount = application.InstallmentAmount ?? 0m;
        var numberOfMonths = application.NumberOfMonths ?? 0;

        var totalPaid = repayments.Sum(r => r.AmountPaid);
        var remaining = sanctioned - totalPaid;
        if (remaining < 0) remaining = 0;

        var isFullyPaid = sanctioned > 0 && totalPaid >= sanctioned - 0.01m;

        // How many full installments the total paid amount covers
        var installmentsCovered = installmentAmount > 0
            ? (int)Math.Floor(totalPaid / installmentAmount)
            : repayments.Count;

        var summary = new QardanRepaymentSummaryResponse
        {
            QardanHasanaId = application.Id,
            ApplicationNo = application.ApplicationNo,
            Status = application.Status,
            SanctionedAmount = application.SanctionedAmount,
            InstallmentAmount = application.InstallmentAmount,
            NumberOfMonths = application.NumberOfMonths,
            InstallmentDateFrom = application.InstallmentDateFrom,
            InstallmentDateTo = application.InstallmentDateTo,
            TotalPaid = totalPaid,
            RemainingAmount = remaining,
            PaymentsCount = repayments.Count,
            InstallmentsCovered = installmentsCovered,
            IsFullyPaid = isFullyPaid,
            Repayments = repayments
        };

        // Compute next installment info when the loan is still outstanding
        if (!isFullyPaid && application.InstallmentDateFrom.HasValue && numberOfMonths > 0)
        {
            var nextNumber = installmentsCovered + 1;
            if (nextNumber <= numberOfMonths)
            {
                summary.NextInstallmentNumber = nextNumber;
                summary.NextInstallmentDate = application.InstallmentDateFrom.Value.AddMonths(nextNumber - 1);
                var suggested = installmentAmount > 0 ? installmentAmount : remaining;
                summary.NextInstallmentAmount = Math.Min(suggested, remaining);
            }
            else
            {
                // Beyond the scheduled months but balance still remains
                summary.NextInstallmentNumber = null;
                summary.NextInstallmentDate = application.InstallmentDateTo;
                summary.NextInstallmentAmount = remaining;
            }
        }

        return summary;
    }
}
