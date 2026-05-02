using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.ViewModel;

namespace BurhaniGuards.Api.Services;

public interface ISurveyService
{
    Task<SurveyResponse> Create(CreateSurveyRequest request, CurrentUserViewModel currentUser);
    Task<SurveyResponse> Update(int memberId, CreateSurveyRequest request, CurrentUserViewModel adminUser);
    Task<SurveyResponse?> GetByMemberId(int memberId);
    Task<List<SurveyResponse>> GetAll();
    Task<bool> HasSubmitted(int memberId);
}
