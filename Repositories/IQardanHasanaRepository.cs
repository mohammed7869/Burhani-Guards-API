using BurhaniGuards.Api.Contracts.Responses;

namespace BurhaniGuards.Api.Repositories;

public interface IQardanHasanaRepository
{
    Task<int> Create(
        string applicationNo, int applicantMemberId, string applicantItsId,
        string applicantName, string applicantJamaat, int? applicantJamaatId,
        string? applicantOccupation, string applicantMobile,
        string? reason, decimal amountRequested,
        string? applicantSignatureUrl, string? applicantPhotoUrl,
        bool termsAccepted,
        int captainMemberId, string captainName, string? captainMobile,
        int guarantorMemberId, string guarantorName, string? guarantorMobile);

    Task<QardanHasanaResponse?> GetById(int id);
    Task<List<QardanHasanaListResponse>> GetAll(string? statusFilter = null);
    Task<List<QardanHasanaListResponse>> GetByApplicantId(int memberId);
    Task<List<QardanHasanaListResponse>> GetByJamaat(string jamaat);
    Task<int> GetNextApplicationCount();

    Task UpdateStatus(int id, string status);
    Task UpdateFormImage(int id, string formImageUrl);
    Task Sanction(int id, decimal sanctionedAmount, decimal installmentAmount,
        int numberOfMonths, DateTime installmentDateFrom, DateTime installmentDateTo,
        string? adminSignatureUrl, string? adminFormImageUrl, int adminApprovedBy);
    Task Reject(int id, string? reason, int adminId);

    Task<List<JamaatMemberResponse>> GetMembersByJamaat(string jamaat, int excludeMemberId);
    Task<JamaatMemberResponse?> GetCaptainByJamaat(string jamaat);
    Task<MemberBasicInfo?> GetMemberById(int id);
    Task CaptainApprove(int id);
    Task GuarantorApprove(int id);
    Task<List<MemberBasicInfo>> GetResourceAdmins();
    Task<bool> HasActiveApplication(int memberId);
    Task UpdateApplication(int id, string applicantName, string? applicantOccupation,
        string applicantMobile, string? reason, decimal amountRequested,
        int captainMemberId, string captainName, string? captainMobile,
        int guarantorMemberId, string guarantorName, string? guarantorMobile);

    /// <summary>
    /// Get applications where the given member is a guarantor (Guarantor 1 or Guarantor 2)
    /// </summary>
    Task<List<QardanHasanaListResponse>> GetByGuarantorId(int memberId);
}
