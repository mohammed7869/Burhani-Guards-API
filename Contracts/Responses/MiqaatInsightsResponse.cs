namespace BurhaniGuards.Api.Contracts.Responses;

public record MiqaatInsightsResponse(
    MiqaatOverallStats OverallStats,
    List<MiqaatInsightItem> Miqaats,
    List<JamaatInsightItem> JamaatBreakdown,
    List<MonthlyMiqaatCount> MonthlyTrend
);

public record MiqaatOverallStats(
    int TotalMiqaats,
    int ApprovedMiqaats,
    int PendingMiqaats,
    int RejectedMiqaats,
    int TotalEnrolled,
    int TotalAttended,
    int TotalLocalMiqaats,
    int TotalInternationalMiqaats,
    int ReportsSubmitted
);

public record MiqaatInsightItem(
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
    int TotalEnrolled,
    int TotalApproved,
    int TotalAttended,
    int TotalPending,
    int TotalRejected,
    bool IsReportSubmitted
);

public record JamaatInsightItem(
    string Jamaat,
    int TotalMiqaats,
    int ApprovedMiqaats,
    int TotalEnrolled,
    int TotalAttended
);

public record MonthlyMiqaatCount(
    string MonthLabel,
    int Count
);
