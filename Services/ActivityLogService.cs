using BurhaniGuards.Api.BusinessModel;
using BurhaniGuards.Api.Repositories;
using System.Text.Json;

namespace BurhaniGuards.Api.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IActivityLogRepository _repository;

    public ActivityLogService(IActivityLogRepository repository)
    {
        _repository = repository;
    }

    public async Task LogAsync(ActivityLogModel log)
    {
        try
        {
            await _repository.AddAsync(log);
        }
        catch (Exception ex)
        {
            // Activity logging should never break the main flow
            System.Diagnostics.Debug.WriteLine($"Error logging activity: {ex.Message}");
        }
    }

    public async Task LogBatchAsync(List<ActivityLogModel> logs)
    {
        try
        {
            await _repository.AddBatchAsync(logs);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error batch logging activity: {ex.Message}");
        }
    }

    // ─── Miqaat Lifecycle ─────────────────────────────────────────────────

    public async Task LogMiqaatCreatedAsync(long miqaatId, string miqaatName, string captainName, int? captainId, string miqaatType, string jamaat)
    {
        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.Miqaat,
            EntityId = miqaatId,
            Action = ActivityAction.MiqaatCreated,
            PerformedBy = captainName,
            PerformedById = captainId,
            PerformedByRole = "Captain",
            MiqaatId = miqaatId,
            NewValue = "Pending",
            Details = JsonSerializer.Serialize(new { miqaatName, miqaatType, jamaat }),
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task LogMiqaatCreatedByAdminAsync(long miqaatId, string miqaatName, string adminName, int? adminId, string miqaatType, string jamaat)
    {
        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.Miqaat,
            EntityId = miqaatId,
            Action = ActivityAction.MiqaatCreatedByAdmin,
            PerformedBy = adminName,
            PerformedById = adminId,
            PerformedByRole = "Admin",
            MiqaatId = miqaatId,
            NewValue = "Approved",
            Details = JsonSerializer.Serialize(new { miqaatName, miqaatType, jamaat }),
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task LogMiqaatUpdatedAsync(long miqaatId, string miqaatName, string performedBy, int? performedById, string performerRole, string? details = null)
    {
        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.Miqaat,
            EntityId = miqaatId,
            Action = ActivityAction.MiqaatUpdated,
            PerformedBy = performedBy,
            PerformedById = performedById,
            PerformedByRole = performerRole,
            MiqaatId = miqaatId,
            Details = details ?? JsonSerializer.Serialize(new { miqaatName }),
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task LogMiqaatDeletedAsync(long miqaatId, string miqaatName, string performedBy, int? performedById, string performerRole)
    {
        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.Miqaat,
            EntityId = miqaatId,
            Action = ActivityAction.MiqaatDeleted,
            PerformedBy = performedBy,
            PerformedById = performedById,
            PerformedByRole = performerRole,
            MiqaatId = miqaatId,
            Details = JsonSerializer.Serialize(new { miqaatName }),
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task LogMiqaatApprovalChangedAsync(long miqaatId, string miqaatName, string performedBy, int? performedById, string oldStatus, string newStatus)
    {
        var action = newStatus == "Approved" ? ActivityAction.MiqaatAdminApproved : ActivityAction.MiqaatAdminRejected;

        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.Miqaat,
            EntityId = miqaatId,
            Action = action,
            PerformedBy = performedBy,
            PerformedById = performedById,
            PerformedByRole = "Admin",
            MiqaatId = miqaatId,
            OldValue = oldStatus,
            NewValue = newStatus,
            Details = JsonSerializer.Serialize(new { miqaatName }),
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task LogMiqaatReportSubmittedAsync(long miqaatId, string miqaatName, string captainName, int? captainId)
    {
        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.Miqaat,
            EntityId = miqaatId,
            Action = ActivityAction.MiqaatReportSubmitted,
            PerformedBy = captainName,
            PerformedById = captainId,
            PerformedByRole = "Captain",
            MiqaatId = miqaatId,
            Details = JsonSerializer.Serialize(new { miqaatName }),
            CreatedAt = DateTime.UtcNow
        });
    }

    // ─── Member Enrollment ────────────────────────────────────────────────

    public async Task LogMemberEnrollmentChangedAsync(long miqaatId, int memberId, string memberName, string performedBy, int? performedById, string performerRole, string oldStatus, string newStatus, IReadOnlyCollection<int>? days)
    {
        string action;
        if (newStatus == "Approved")
            action = ActivityAction.MemberEnrolled;
        else if (newStatus == "Rejected")
            action = ActivityAction.MemberUnenrolled;
        else
            action = ActivityAction.MemberEnrollmentChanged;

        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.MiqaatMember,
            EntityId = memberId,
            Action = action,
            PerformedBy = performedBy,
            PerformedById = performedById,
            PerformedByRole = performerRole,
            TargetMemberId = memberId,
            TargetMemberName = memberName,
            MiqaatId = miqaatId,
            OldValue = oldStatus,
            NewValue = newStatus,
            Details = days != null && days.Count > 0 
                ? JsonSerializer.Serialize(new { days = days.ToList() }) 
                : null,
            CreatedAt = DateTime.UtcNow
        });
    }

    // ─── Captain Actions ──────────────────────────────────────────────────

    public async Task LogCaptainFinalStatusAsync(long miqaatId, int memberId, string memberName, string captainName, int? captainId, string finalStatus, IReadOnlyCollection<int>? days)
    {
        var action = finalStatus == "Approved" 
            ? ActivityAction.CaptainApprovedMember 
            : ActivityAction.CaptainRejectedMember;

        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.MiqaatMember,
            EntityId = memberId,
            Action = action,
            PerformedBy = captainName,
            PerformedById = captainId,
            PerformedByRole = "Captain",
            TargetMemberId = memberId,
            TargetMemberName = memberName,
            MiqaatId = miqaatId,
            NewValue = finalStatus,
            Details = days != null && days.Count > 0 
                ? JsonSerializer.Serialize(new { days = days.ToList() }) 
                : null,
            CreatedAt = DateTime.UtcNow
        });
    }

    // ─── Attendance ───────────────────────────────────────────────────────

    public async Task LogAttendanceMarkedAsync(long miqaatId, string miqaatName, int day, List<int> memberIds, string captainName, int? captainId)
    {
        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.MiqaatMember,
            EntityId = miqaatId,
            Action = ActivityAction.AttendanceMarked,
            PerformedBy = captainName,
            PerformedById = captainId,
            PerformedByRole = "Captain",
            MiqaatId = miqaatId,
            MiqaatDay = day,
            Details = JsonSerializer.Serialize(new { miqaatName, day, memberCount = memberIds.Count, memberIds }),
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task LogAttendanceMarkedWithDetailsAsync(long miqaatId, string miqaatName, int day, List<int> memberIds, string captainName, int? captainId, List<object> memberDetails)
    {
        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.MiqaatMember,
            EntityId = miqaatId,
            Action = ActivityAction.AttendanceMarked,
            PerformedBy = captainName,
            PerformedById = captainId,
            PerformedByRole = "Captain",
            MiqaatId = miqaatId,
            MiqaatDay = day,
            Details = JsonSerializer.Serialize(new { miqaatName, day, memberCount = memberIds.Count, members = memberDetails }),
            CreatedAt = DateTime.UtcNow
        });
    }

    // ─── Member Management ────────────────────────────────────────────────

    public async Task LogMemberCreatedAsync(int memberId, string memberName, string itsId, string? createdBy, int? createdById, string creatorRole)
    {
        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.Member,
            EntityId = memberId,
            Action = ActivityAction.MemberCreated,
            PerformedBy = createdBy,
            PerformedById = createdById,
            PerformedByRole = creatorRole,
            TargetMemberId = memberId,
            TargetMemberName = memberName,
            Details = JsonSerializer.Serialize(new { memberName, itsId }),
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task LogMemberUpdatedAsync(int memberId, string memberName, string performedBy, int? performedById, string performerRole, string? details = null, string? oldName = null, string? newName = null)
    {
        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.Member,
            EntityId = memberId,
            Action = ActivityAction.MemberUpdated,
            PerformedBy = performedBy,
            PerformedById = performedById,
            PerformedByRole = performerRole,
            TargetMemberId = memberId,
            TargetMemberName = memberName,
            OldValue = oldName,
            NewValue = newName,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task LogMemberActivatedAsync(int memberId, string memberName, string performedBy, int? performedById, string performerRole)
    {
        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.Member,
            EntityId = memberId,
            Action = ActivityAction.MemberActivated,
            PerformedBy = performedBy,
            PerformedById = performedById,
            PerformedByRole = performerRole,
            TargetMemberId = memberId,
            TargetMemberName = memberName,
            OldValue = "Inactive",
            NewValue = "Active",
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task LogMemberDeactivatedAsync(int memberId, string memberName, string performedBy, int? performedById, string performerRole)
    {
        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.Member,
            EntityId = memberId,
            Action = ActivityAction.MemberDeactivated,
            PerformedBy = performedBy,
            PerformedById = performedById,
            PerformedByRole = performerRole,
            TargetMemberId = memberId,
            TargetMemberName = memberName,
            OldValue = "Active",
            NewValue = "Inactive",
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task LogMemberApprovedAsync(int memberId, string memberName, string performedBy, int? performedById, string performerRole)
    {
        await LogAsync(new ActivityLogModel
        {
            EntityType = ActivityEntityType.Member,
            EntityId = memberId,
            Action = ActivityAction.MemberApprovedByAdmin,
            PerformedBy = performedBy,
            PerformedById = performedById,
            PerformedByRole = performerRole,
            TargetMemberId = memberId,
            TargetMemberName = memberName,
            OldValue = "Unapproved",
            NewValue = "Approved",
            CreatedAt = DateTime.UtcNow
        });
    }

    // ─── Retrieval ────────────────────────────────────────────────────────

    public async Task<(List<ActivityLogModel> Items, int TotalCount)> GetAllAsync(
        string? entityType, string? action, long? miqaatId, int? memberId,
        DateTime? fromDate, DateTime? toDate, string? search,
        int page, int pageSize)
    {
        return await _repository.GetAllAsync(entityType, action, miqaatId, memberId, fromDate, toDate, search, page, pageSize);
    }

    public async Task<(List<ActivityLogModel> Items, int TotalCount)> GetByMiqaatIdAsync(long miqaatId, int page, int pageSize)
    {
        return await _repository.GetByMiqaatIdAsync(miqaatId, page, pageSize);
    }

    public async Task<(List<ActivityLogModel> Items, int TotalCount)> GetByMemberIdAsync(int memberId, int page, int pageSize)
    {
        return await _repository.GetByMemberIdAsync(memberId, page, pageSize);
    }

    public async Task<List<ActivityLogModel>> GetRecentAsync(int count)
    {
        return await _repository.GetRecentAsync(count);
    }
}
