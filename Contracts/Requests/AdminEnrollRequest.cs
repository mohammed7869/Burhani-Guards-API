namespace BurhaniGuards.Api.Contracts.Requests;

public sealed record AdminEnrollRequest(
    List<int> MemberIds,
    List<int>? Days
);
