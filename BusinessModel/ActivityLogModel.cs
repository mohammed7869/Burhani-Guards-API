namespace BurhaniGuards.Api.BusinessModel;

public class ActivityLogModel
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;      // "Miqaat", "MiqaatMember", "Member"
    public long EntityId { get; set; }
    public string Action { get; set; } = string.Empty;           // Action constant
    public string? PerformedBy { get; set; }                      // Name of performer
    public int? PerformedById { get; set; }                       // Member ID of performer
    public string? PerformedByRole { get; set; }                  // "Admin", "Captain", "Member"
    public int? TargetMemberId { get; set; }                      // Member being acted upon
    public string? TargetMemberName { get; set; }                 // Name of target member
    public long? MiqaatId { get; set; }                           // Associated miqaat
    public string? MiqaatName { get; set; }                       // Resolved miqaat name from local_miqaat
    public int? MiqaatDay { get; set; }                           // Day number
    public string? OldValue { get; set; }                         // Previous state
    public string? NewValue { get; set; }                         // New state
    public string? Details { get; set; }                          // Additional JSON details
    public string? IpAddress { get; set; }                        // IP address
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Constants for activity log action types
/// </summary>
public static class ActivityAction
{
    // Miqaat lifecycle
    public const string MiqaatCreated = "MIQAAT_CREATED";
    public const string MiqaatCreatedByAdmin = "MIQAAT_CREATED_BY_ADMIN";
    public const string MiqaatUpdated = "MIQAAT_UPDATED";
    public const string MiqaatDeleted = "MIQAAT_DELETED";
    public const string MiqaatAdminApproved = "MIQAAT_ADMIN_APPROVED";
    public const string MiqaatAdminRejected = "MIQAAT_ADMIN_REJECTED";
    public const string MiqaatReportSubmitted = "MIQAAT_REPORT_SUBMITTED";

    // Member enrollment
    public const string MemberEnrolled = "MEMBER_ENROLLED";
    public const string MemberUnenrolled = "MEMBER_UNENROLLED";
    public const string MemberEnrollmentChanged = "MEMBER_ENROLLMENT_CHANGED";

    // Captain actions
    public const string CaptainApprovedMember = "CAPTAIN_APPROVED_MEMBER";
    public const string CaptainRejectedMember = "CAPTAIN_REJECTED_MEMBER";

    // Attendance
    public const string AttendanceMarked = "ATTENDANCE_MARKED";

    // Member management
    public const string MemberCreated = "MEMBER_CREATED";
    public const string MemberUpdated = "MEMBER_UPDATED";
    public const string MemberActivated = "MEMBER_ACTIVATED";
    public const string MemberDeactivated = "MEMBER_DEACTIVATED";
    public const string MemberApprovedByAdmin = "MEMBER_APPROVED_BY_ADMIN";

    // Survey
    public const string SurveySubmitted = "SURVEY_SUBMITTED";
    public const string SurveyUpdated = "SURVEY_UPDATED";

    // Qardan Hasana
    public const string QardanHasanaSubmitted = "QARDAN_HASANA_SUBMITTED";
    public const string QardanHasanaEdited = "QARDAN_HASANA_EDITED";
    public const string QardanHasanaCaptainEdited = "QARDAN_HASANA_CAPTAIN_EDITED";
    public const string QardanHasanaAdminEdited = "QARDAN_HASANA_ADMIN_EDITED";
    public const string QardanHasanaCaptainApproved = "QARDAN_HASANA_CAPTAIN_APPROVED";
}

/// <summary>
/// Constants for entity types
/// </summary>
public static class ActivityEntityType
{
    public const string Miqaat = "Miqaat";
    public const string MiqaatMember = "MiqaatMember";
    public const string Member = "Member";
    public const string Survey = "Survey";
    public const string QardanHasana = "QardanHasana";
}
