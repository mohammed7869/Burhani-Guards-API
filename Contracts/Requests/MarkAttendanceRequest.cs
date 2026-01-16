namespace BurhaniGuards.Api.Contracts.Requests;

public sealed record MarkAttendanceRequest(
    int Day,
    List<int> MemberIds
);
