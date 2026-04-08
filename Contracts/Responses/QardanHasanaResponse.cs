namespace BurhaniGuards.Api.Contracts.Responses;

/// <summary>
/// Response DTO for Qardan Hasana application
/// </summary>
public class QardanHasanaResponse
{
    public int Id { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;

    // Applicant Info
    public int ApplicantMemberId { get; set; }
    public string ApplicantItsId { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantJamaat { get; set; } = string.Empty;
    public string? ApplicantOccupation { get; set; }
    public string ApplicantMobile { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public decimal AmountRequested { get; set; }
    public string? ApplicantSignatureUrl { get; set; }
    public string? ApplicantPhotoUrl { get; set; }

    // Guarantor 1: Captain
    public int CaptainMemberId { get; set; }
    public string CaptainName { get; set; } = string.Empty;
    public string? CaptainMobile { get; set; }

    // Guarantor 2: Member
    public int GuarantorMemberId { get; set; }
    public string GuarantorName { get; set; } = string.Empty;
    public string? GuarantorMobile { get; set; }

    // Status
    public string Status { get; set; } = "pending";
    public string? FormImageUrl { get; set; }

    // Office Use Only
    public decimal? SanctionedAmount { get; set; }
    public decimal? InstallmentAmount { get; set; }
    public int? NumberOfMonths { get; set; }
    public DateTime? InstallmentDateFrom { get; set; }
    public DateTime? InstallmentDateTo { get; set; }
    public string? AdminSignatureUrl { get; set; }
    public string? AdminFormImageUrl { get; set; }
    public int? AdminApprovedBy { get; set; }
    public DateTime? AdminApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Simplified list item for Qardan Hasana applications
/// </summary>
public class QardanHasanaListResponse
{
    public int Id { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantJamaat { get; set; } = string.Empty;
    public decimal AmountRequested { get; set; }
    public decimal? SanctionedAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Member dropdown item for guarantor selection
/// </summary>
public class JamaatMemberResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string ItsId { get; set; } = string.Empty;
}
