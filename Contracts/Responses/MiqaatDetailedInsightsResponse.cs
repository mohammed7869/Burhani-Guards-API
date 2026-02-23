namespace BurhaniGuards.Api.Contracts.Responses;

/// <summary>
/// Full detailed insights for a single miqaat: summary + day-wise stats + member-wise day breakdown.
/// </summary>
public record MiqaatDetailedInsightsResponse(
    MiqaatDetailSummary Summary,
    List<MiqaatDayStat> DayStats,
    List<MiqaatMemberDayDetail> Members
);

/// <summary>
/// High-level summary of the miqaat (mirrors MiqaatInsightItem but with richer counts).
/// </summary>
public record MiqaatDetailSummary(
    long Id,
    string MiqaatName,
    string MiqaatType,
    string Jamaat,
    string Jamiyat,
    string FromDate,
    string TillDate,
    int MiqaatDays,
    int VolunteerLimit,
    string AdminApproval,
    string CaptainName,
    bool IsReportSubmitted,
    // Unique members in miqaat_members
    int TotalUniqueMembers,
    // Members that enrolled (Approved) for at least one day
    int TotalEnrolledMembers,
    // Total day-slots where status = 'Approved'
    int TotalApprovedSlots,
    // Total day-slots where is_attended = 1
    int TotalAttendedSlots,
    // Total day-slots where status = 'Pending'
    int TotalPendingSlots,
    // Total day-slots where status = 'Rejected'
    int TotalRejectedSlots
);

/// <summary>
/// Per-day enrollment and attendance stats for a miqaat.
/// </summary>
public record MiqaatDayStat(
    int Day,
    string DayDate,
    int Enrolled,   // status = 'Approved'
    int Pending,    // status = 'Pending'
    int Rejected,   // status = 'Rejected'
    int Attended    // is_attended = 1
);

/// <summary>
/// Per-member day-wise breakdown for a miqaat.
/// </summary>
public record MiqaatMemberDayDetail(
    long MemberId,
    string FullName,
    string ItsId,
    string Rank,
    string Jamaat,
    string Contact,
    // Day number -> { Status, FinalStatus, IsAttended }
    List<MiqaatMemberDayEntry> Days
);

public record MiqaatMemberDayEntry(
    int Day,
    string DayDate,
    string Status,        // Pending | Approved | Rejected
    string? FinalStatus,  // null | Pending | Approved | Rejected
    bool IsAttended
);
