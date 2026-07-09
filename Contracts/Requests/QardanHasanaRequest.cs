namespace BurhaniGuards.Api.Contracts.Requests;

/// <summary>
/// Request to create a new Qardan Hasana application (submitted by Member)
/// </summary>
public class CreateQardanHasanaRequest
{
    public string ApplicantName { get; set; } = string.Empty;
    public string? ApplicantOccupation { get; set; }
    public string ApplicantMobile { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public decimal AmountRequested { get; set; }
    public bool TermsAccepted { get; set; }

    // Guarantor 1 - Member selected from dropdown (same jamaat)
    public int Guarantor1MemberId { get; set; }

    // Guarantor 2 - Member selected from dropdown (same jamaat)
    public int GuarantorMemberId { get; set; }
}

/// <summary>
/// Request for Admin to sanction (Office Use Only section)
/// </summary>
public class SanctionQardanHasanaRequest
{
    public decimal SanctionedAmount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public int NumberOfMonths { get; set; }
    public DateTime InstallmentDateFrom { get; set; }
    public DateTime InstallmentDateTo { get; set; }
}

/// <summary>
/// Request for Admin to reject an application
/// </summary>
public class RejectQardanHasanaRequest
{
    public string? Reason { get; set; }
}

/// <summary>
/// Request to update a Qardan Hasana application (by Applicant, before guarantor approvals)
/// Only specific fields are editable: Name, Occupation, Mobile, Reason, Amount, Guarantor1, Guarantor2
/// </summary>
public class UpdateQardanHasanaRequest
{
    public string ApplicantName { get; set; } = string.Empty;
    public string? ApplicantOccupation { get; set; }
    public string ApplicantMobile { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public decimal AmountRequested { get; set; }
    public int Guarantor1MemberId { get; set; }
    public int GuarantorMemberId { get; set; }
}

/// <summary>
/// Request for a guarantor to reject an application
/// </summary>
public class GuarantorRejectRequest
{
    public string? Reason { get; set; }
}
