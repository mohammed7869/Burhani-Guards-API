using BurhaniGuards.Api.BusinessModel;

namespace BurhaniGuards.Api.Services;

public interface IActivityLogService
{
    // Core logging
    Task LogAsync(ActivityLogModel log);
    Task LogBatchAsync(List<ActivityLogModel> logs);

    // Miqaat lifecycle
    Task LogMiqaatCreatedAsync(long miqaatId, string miqaatName, string captainName, int? captainId, string miqaatType, string jamaat);
    Task LogMiqaatCreatedByAdminAsync(long miqaatId, string miqaatName, string adminName, int? adminId, string miqaatType, string jamaat);
    Task LogMiqaatUpdatedAsync(long miqaatId, string miqaatName, string performedBy, int? performedById, string performerRole, string? details = null);
    Task LogMiqaatDeletedAsync(long miqaatId, string miqaatName, string performedBy, int? performedById, string performerRole);
    Task LogMiqaatApprovalChangedAsync(long miqaatId, string miqaatName, string performedBy, int? performedById, string oldStatus, string newStatus);
    Task LogMiqaatReportSubmittedAsync(long miqaatId, string miqaatName, string captainName, int? captainId);

    // Member enrollment
    Task LogMemberEnrollmentChangedAsync(long miqaatId, int memberId, string memberName, string performedBy, int? performedById, string performerRole, string oldStatus, string newStatus, IReadOnlyCollection<int>? days);

    // Captain actions
    Task LogCaptainFinalStatusAsync(long miqaatId, int memberId, string memberName, string performedBy, int? performedById, string performerRole, string finalStatus, IReadOnlyCollection<int>? days);

    // Attendance
    Task LogAttendanceMarkedAsync(long miqaatId, string miqaatName, int day, List<int> memberIds, string performedBy, int? performedById, string performerRole);
    Task LogAttendanceMarkedWithDetailsAsync(long miqaatId, string miqaatName, int day, List<int> memberIds, string performedBy, int? performedById, string performerRole, List<object> memberDetails);

    // Member management
    Task LogMemberCreatedAsync(int memberId, string memberName, string itsId, string? createdBy, int? createdById, string creatorRole);
    Task LogMemberUpdatedAsync(int memberId, string memberName, string performedBy, int? performedById, string performerRole, string? details = null, string? oldName = null, string? newName = null);
    Task LogMemberActivatedAsync(int memberId, string memberName, string performedBy, int? performedById, string performerRole);
    Task LogMemberDeactivatedAsync(int memberId, string memberName, string performedBy, int? performedById, string performerRole);
    Task LogMemberApprovedAsync(int memberId, string memberName, string performedBy, int? performedById, string performerRole);

    // Survey
    Task LogSurveySubmittedAsync(int memberId, string memberName, string itsId, int departmentId, string departmentName, int zoneId, string zoneName);

    // Retrieval
    Task<(List<ActivityLogModel> Items, int TotalCount)> GetAllAsync(
        string? entityType, string? action, long? miqaatId, int? memberId,
        DateTime? fromDate, DateTime? toDate, string? search,
        int page, int pageSize);
    Task<(List<ActivityLogModel> Items, int TotalCount)> GetByMiqaatIdAsync(long miqaatId, int page, int pageSize);
    Task<(List<ActivityLogModel> Items, int TotalCount)> GetByMemberIdAsync(int memberId, int page, int pageSize);
    Task<List<ActivityLogModel>> GetRecentAsync(int count);
}
