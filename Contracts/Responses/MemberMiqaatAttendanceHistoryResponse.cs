namespace BurhaniGuards.Api.Contracts.Responses;

public sealed record MemberMiqaatAttendanceItemResponse(
    long MiqaatId,
    string MiqaatName,
    DateTime FromDate,
    DateTime TillDate,
    int MiqaatDays,
    int MiqaatDay,
    bool IsAttended,
    int Points
);

public sealed record MemberMiqaatAttendanceHistoryResponse(
    long MemberId,
    string FullName,
    string? ItsId,
    int TotalPoints,
    List<MemberMiqaatAttendanceItemResponse> Items
);

