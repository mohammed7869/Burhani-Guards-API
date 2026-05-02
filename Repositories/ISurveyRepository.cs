using BurhaniGuards.Api.Contracts.Responses;

namespace BurhaniGuards.Api.Repositories;

public interface ISurveyRepository
{
    Task<int> Create(int memberId, int department, int zone);
    Task<bool> Update(int memberId, int department, int zone);
    Task<SurveyResponse?> GetByMemberId(int memberId);
    Task<List<SurveyResponse>> GetAll();
    Task<bool> HasSubmitted(int memberId);
}
