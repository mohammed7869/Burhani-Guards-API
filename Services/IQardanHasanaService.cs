using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.ViewModel;

namespace BurhaniGuards.Api.Services;

public interface IQardanHasanaService
{
    Task<QardanHasanaResponse> Create(CreateQardanHasanaRequest request, CurrentUserViewModel currentUser);
    Task<QardanHasanaResponse?> GetById(int id);
    Task<List<QardanHasanaListResponse>> GetAll(string? statusFilter = null);
    Task<List<QardanHasanaListResponse>> GetMyApplications(int memberId);
    Task<List<QardanHasanaListResponse>> GetByJamaat(string jamaat);
    Task<List<QardanHasanaListResponse>> GetGuarantorApplications(int memberId);
    Task Sanction(int id, SanctionQardanHasanaRequest request, string? adminSignatureUrl, string? adminFormImageUrl, int adminId);
    Task Reject(int id, RejectQardanHasanaRequest request, int adminId);
    Task GuarantorApprove(int applicationId, int guarantorMemberId);
    Task GuarantorReject(int applicationId, int guarantorMemberId, string? reason);
    Task<QardanHasanaResponse> UpdateApplication(int id, UpdateQardanHasanaRequest request, CurrentUserViewModel currentUser);
    Task<List<JamaatMemberResponse>> GetMembersByJamaat(string jamaat, int excludeMemberId);
    Task<JamaatMemberResponse?> GetCaptainByJamaat(string jamaat);
    Task<byte[]> GeneratePdf(int id);
    Task<bool> HasActiveApplication(int memberId);
}
