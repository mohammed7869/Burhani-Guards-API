using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Repositories;
using BurhaniGuards.Api.ViewModel;

namespace BurhaniGuards.Api.Services;

public class SurveyService : ISurveyService
{
    private readonly ISurveyRepository _repository;
    private readonly IActivityLogService _activityLogService;

    // Static department/zone maps for activity log details
    private static readonly Dictionary<int, string> DepartmentMap = new()
    {
        {1, "Not Assigned Any Khidmat"}, {2, "PMO"}, {3, "Human Resources"},
        {4, "PR / Govt Relations"}, {5, "Construction"}, {6, "Mawaid"},
        {7, "Procurement"}, {8, "Finance"}, {9, "AVRP (Audio/Video/Relay/Photography)"},
        {10, "Flow Management"}, {11, "Transport"}, {12, "Nazafat"},
        {13, "IT Services"}, {14, "Zones"}, {15, "ITS"},
        {16, "Medical"}, {17, "Tazyeen"}, {18, "Communications"},
        {19, "Mumineen Mehmaan Reception"}, {20, "Central Office"},
        {21, "Fire Safety / HSE"}, {22, "Accommodation"}, {23, "Security"},
        {24, "Karamat"}, {25, "Food Hygiene & Safety"}, {26, "QA Support Management"},
        {27, "Waaz Talaqqi & Ohbat"}, {28, "Al-Vazarat Follow Up"},
        {29, "Signage & Maps"}, {30, "Zakereen"}, {31, "Bethak"},
        {32, "Mazaraat"}, {33, "Laundry"}
    };

    private static readonly Dictionary<int, string> ZoneMap = new()
    {
        {1, "Ashara City"}, {2, "Burhani Mohalla"}, {3, "CMZ"},
        {4, "Fakhri Mohalla"}, {5, "Fatemi Mohalla"}, {6, "Hasanjinagar"},
        {7, "Imadi Mohalla (Hadapsar)"}, {8, "Inamdaar(Azam Campus)"},
        {9, "Jamali Mohalla (Undri)"}, {10, "Kalimi Mohalla"},
        {11, "Mohammadi Mohalla"}, {12, "Mufaddal Mohalla"},
        {13, "Taiyebi Mohalla"}, {14, "Vajihi Mohalla (Kasarwadi)"},
        {15, "Zainee Mohalla"}
    };

    public SurveyService(ISurveyRepository repository, IActivityLogService activityLogService)
    {
        _repository = repository;
        _activityLogService = activityLogService;
    }

    public async Task<SurveyResponse> Create(CreateSurveyRequest request, CurrentUserViewModel currentUser)
    {
        // Check if user has already submitted
        var hasSubmitted = await _repository.HasSubmitted(currentUser.id);
        if (hasSubmitted)
        {
            throw new Exception("You have already submitted the survey.");
        }

        // Validate department and zone IDs
        if (request.Department < 1 || request.Department > 33)
        {
            throw new Exception("Invalid department selected.");
        }

        // Zone not required when department is "Not Assigned Any Khidmat" (id=1)
        if (request.Department != 1 && (request.Zone < 1 || request.Zone > 15))
        {
            throw new Exception("Please select a Zone.");
        }

        var id = await _repository.Create(currentUser.id, request.Department, request.Zone);

        var survey = await _repository.GetByMemberId(currentUser.id);
        if (survey == null)
        {
            throw new Exception("Failed to create survey entry.");
        }

        // Log to activity_log
        var deptName = DepartmentMap.GetValueOrDefault(request.Department, "Unknown");
        var zoneName = ZoneMap.GetValueOrDefault(request.Zone, "Unknown");
        await _activityLogService.LogSurveySubmittedAsync(
            currentUser.id,
            currentUser.fullName,
            currentUser.itsId ?? "",
            request.Department,
            deptName,
            request.Zone,
            zoneName
        );

        return survey;
    }

    public async Task<SurveyResponse> Update(int memberId, CreateSurveyRequest request, CurrentUserViewModel adminUser)
    {
        // Validate
        if (request.Department < 1 || request.Department > 33)
            throw new Exception("Invalid department selected.");
        // Zone not required when department is "Not Assigned Any Khidmat" (id=1)
        if (request.Department != 1 && (request.Zone < 1 || request.Zone > 15))
            throw new Exception("Please select a Zone.");

        // Get existing survey for old values
        var existing = await _repository.GetByMemberId(memberId);
        if (existing == null)
            throw new Exception("Survey not found for this member.");

        var oldDeptName = DepartmentMap.GetValueOrDefault(existing.Department, "Unknown");
        var oldZoneName = ZoneMap.GetValueOrDefault(existing.Zone, "Unknown");
        var newDeptName = DepartmentMap.GetValueOrDefault(request.Department, "Unknown");
        var newZoneName = ZoneMap.GetValueOrDefault(request.Zone, "Unknown");

        // Update
        var updated = await _repository.Update(memberId, request.Department, request.Zone);
        if (!updated)
            throw new Exception("Failed to update survey.");

        // Log activity
        var details = System.Text.Json.JsonSerializer.Serialize(new
        {
            itsId = existing.ItsId,
            memberName = existing.FullName,
            oldDepartmentId = existing.Department,
            oldDepartmentName = oldDeptName,
            newDepartmentId = request.Department,
            newDepartmentName = newDeptName,
            oldZoneId = existing.Zone,
            oldZoneName = oldZoneName,
            newZoneId = request.Zone,
            newZoneName = newZoneName
        });

        await _activityLogService.LogAsync(new BusinessModel.ActivityLogModel
        {
            EntityType = BusinessModel.ActivityEntityType.Survey,
            EntityId = memberId,
            Action = BusinessModel.ActivityAction.SurveyUpdated,
            PerformedBy = adminUser.fullName,
            PerformedById = adminUser.id,
            PerformedByRole = "Admin",
            TargetMemberId = memberId,
            TargetMemberName = existing.FullName,
            OldValue = $"{oldDeptName} | {oldZoneName}",
            NewValue = $"{newDeptName} | {newZoneName}",
            Details = details
        });

        return (await _repository.GetByMemberId(memberId))!;
    }

    public async Task<SurveyResponse?> GetByMemberId(int memberId)
    {
        return await _repository.GetByMemberId(memberId);
    }

    public async Task<List<SurveyResponse>> GetAll()
    {
        return await _repository.GetAll();
    }

    public async Task<bool> HasSubmitted(int memberId)
    {
        return await _repository.HasSubmitted(memberId);
    }
}
