using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;

namespace BurhaniGuards.Api.Services;

public interface IMiqaatService
{
    Task<MiqaatResponse> Create(CreateMiqaatRequest request, string captainName);
    Task<List<MiqaatResponse>> GetAll();
    Task<MiqaatResponse?> GetById(long id);
    Task Update(long id, UpdateMiqaatRequest request);
    Task UpdateApprovalStatus(long id, string status);
    Task Delete(long id);
    Task<List<MiqaatResponse>> GetMiqaatsByMemberId(int memberId);
    Task<List<MiqaatResponse>> GetMiqaatsByCaptainName(string captainName);
    Task<List<MiqaatResponse>> GetMiqaatsForCurrentUser(int userId, int? userRole, string? captainName);
    Task UpdateMemberMiqaatStatus(int memberId, long miqaatId, string status);
}

