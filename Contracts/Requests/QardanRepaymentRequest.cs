namespace BurhaniGuards.Api.Contracts.Requests;

/// <summary>
/// Request for Admin to record a repayment / installment payment against a
/// sanctioned Qardan Hasana application. Sent as multipart/form-data so an
/// optional receipt image can be attached.
/// </summary>
public class RecordRepaymentRequest
{
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; }

    /// <summary>Optional installment index (1..NumberOfMonths)</summary>
    public int? InstallmentNumber { get; set; }

    /// <summary>Cash / UPI / Cheque / Bank Transfer / Other</summary>
    public string? PaymentMode { get; set; }

    public string? Notes { get; set; }
}
