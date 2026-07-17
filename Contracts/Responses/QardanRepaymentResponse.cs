namespace BurhaniGuards.Api.Contracts.Responses;

/// <summary>
/// A single recorded repayment / installment payment.
/// </summary>
public class QardanRepaymentResponse
{
    public int Id { get; set; }
    public int QardanHasanaId { get; set; }
    public int? InstallmentNumber { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PaymentMode { get; set; }
    public string? ReceiptImageUrl { get; set; }
    public string? Notes { get; set; }
    public int? RecordedBy { get; set; }
    public string? RecordedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Repayment summary for a Qardan Hasana application: how much has been paid,
/// how much is pending, next installment info, and the full payment history.
/// </summary>
public class QardanRepaymentSummaryResponse
{
    public int QardanHasanaId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Schedule (from the sanctioned application)
    public decimal? SanctionedAmount { get; set; }
    public decimal? InstallmentAmount { get; set; }
    public int? NumberOfMonths { get; set; }
    public DateTime? InstallmentDateFrom { get; set; }
    public DateTime? InstallmentDateTo { get; set; }

    // Progress
    public decimal TotalPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public int PaymentsCount { get; set; }
    public int InstallmentsCovered { get; set; }
    public bool IsFullyPaid { get; set; }

    // Next installment (null when fully paid)
    public int? NextInstallmentNumber { get; set; }
    public DateTime? NextInstallmentDate { get; set; }
    public decimal? NextInstallmentAmount { get; set; }

    public List<QardanRepaymentResponse> Repayments { get; set; } = new();
}
