namespace BurhaniGuards.Api.Contracts.Requests;

public sealed record MarkAttendanceRequest(
    List<int> MemberIds
);
