namespace BurhaniGuards.Api.Domain;

/// <summary>
/// Qardan Hasana (interest-free loan) entity from MySQL qardan_hasana table
/// </summary>
public sealed class QardanHasana
{
    public int Id { get; init; }
    public string ApplicationNo { get; init; } = string.Empty;

    // Applicant Information
    public int ApplicantMemberId { get; init; }
    public string ApplicantItsId { get; init; } = string.Empty;
    public string ApplicantName { get; init; } = string.Empty;
    public string ApplicantJamaat { get; init; } = string.Empty;
    public int? ApplicantJamaatId { get; init; }
    public string? ApplicantOccupation { get; init; }
    public string ApplicantMobile { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public decimal AmountRequested { get; init; }
    public string? ApplicantSignatureUrl { get; init; }
    public string? ApplicantPhotoUrl { get; init; }
    public bool TermsAccepted { get; init; }

    // Guarantor 1 (stored in captain_* columns for backward compatibility)
    public int CaptainMemberId { get; init; }
    public string CaptainName { get; init; } = string.Empty;
    public string? CaptainMobile { get; init; }
    public bool CaptainApproved { get; init; }
    public DateTime? CaptainApprovedAt { get; init; }

    // Guarantor 2: Member
    public int GuarantorMemberId { get; init; }
    public string GuarantorName { get; init; } = string.Empty;
    public string? GuarantorMobile { get; init; }
    public bool GuarantorApproved { get; init; }
    public DateTime? GuarantorApprovedAt { get; init; }

    // Status
    public string Status { get; init; } = "pending";
    public string? FormImageUrl { get; init; }

    // Office Use Only
    public decimal? SanctionedAmount { get; init; }
    public decimal? InstallmentAmount { get; init; }
    public int? NumberOfMonths { get; init; }
    public DateTime? InstallmentDateFrom { get; init; }
    public DateTime? InstallmentDateTo { get; init; }
    public string? AdminSignatureUrl { get; init; }
    public string? AdminFormImageUrl { get; init; }
    public int? AdminApprovedBy { get; init; }
    public DateTime? AdminApprovedAt { get; init; }
    public string? RejectionReason { get; init; }

    // Timestamps
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
