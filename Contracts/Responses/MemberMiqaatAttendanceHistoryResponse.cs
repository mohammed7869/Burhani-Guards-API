namespace BurhaniGuards.Api.Contracts.Responses;

public sealed record MemberMiqaatAttendanceItemResponse(
    long MiqaatId,
    string MiqaatName,
    DateTime FromDate,
    DateTime TillDate,
    int MiqaatDays,
    int MiqaatDay,
    bool IsAttended,
    bool IsAbsent,
    int Points,
    string MiqaatType,
    string? MemberStatus,
    string? FinalStatus,
    string? AdminStatus
);

public sealed record MemberMiqaatAttendanceHistoryResponse(
    long MemberId,
    string FullName,
    string? ItsId,
    int TotalPoints,
    List<MemberMiqaatAttendanceItemResponse> Items
);

