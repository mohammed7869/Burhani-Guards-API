namespace BurhaniGuards.Api.Contracts.Requests;

public sealed record UpdateMemberMiqaatStatusRequest(
    string Status,
    int? Day,
    List<int>? Days
);










