namespace BurhaniGuards.Api.Contracts.Responses;

public sealed record AttendanceWindowInfo(
    bool IsOpen,
    bool IsUpcoming,
    bool IsExpired,
    DateTime WindowStart,
    DateTime WindowEnd,
    string Message,
    string DayLabel
);
