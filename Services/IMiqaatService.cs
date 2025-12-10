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
}

