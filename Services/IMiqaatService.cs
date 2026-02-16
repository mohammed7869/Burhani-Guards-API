using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;

namespace BurhaniGuards.Api.Services;

public interface IMiqaatService
{
    Task<MiqaatResponse> Create(CreateMiqaatRequest request, string captainName);
    Task<MiqaatResponse> CreateByAdmin(CreateMiqaatRequest request, string adminName);
    Task<List<MiqaatResponse>> GetAll();
    Task<MiqaatResponse?> GetById(long id);
    Task Update(long id, UpdateMiqaatRequest request);
    Task UpdateApprovalStatus(long id, string status);
    Task Delete(long id);
    Task<List<MiqaatResponse>> GetMiqaatsByMemberId(int memberId);
    Task<List<MiqaatResponse>> GetMiqaatsByCaptainName(string captainName);
    Task<List<MiqaatResponse>> GetMiqaatsForCurrentUser(int userId, int? userRole, string? captainName);
    Task UpdateMemberMiqaatStatus(int memberId, long miqaatId, string status, IReadOnlyCollection<int>? days);
    Task<List<MemberPointsResponse>> GetMemberPointsByJamaat(string jamaat);
    Task<MemberPointsResponse> GetMemberPointsByMemberId(int memberId);
    Task<List<AdminMemberPointsResponse>> GetAllMemberPointsForAdmin();
    Task<List<EnrolledMemberResponse>> GetEnrolledMembersByMiqaatId(long miqaatId);
    Task<List<EnrolledMemberResponse>> GetAllMembersByMiqaatId(long miqaatId);
    Task<List<EnrolledMemberResponse>> GetApprovedMembersForAttendance(long miqaatId, int day);
    Task UpdateFinalStatus(int memberId, long miqaatId, string finalStatus);
    Task MarkAttendanceBatch(long miqaatId, int day, List<int> memberIds);
    Task<MemberMiqaatAttendanceHistoryResponse> GetMemberAttendanceHistory(int memberId);
    Task UpdateMiqaatReport(long miqaatId, string? image1, string? image2, string? notes, string? khidmatDone);
    Task<bool> HasExistingReport(long miqaatId);
    Task<List<MemberEnrollmentDayResponse>> GetMemberEnrollmentDays(long miqaatId, int memberId);
}

