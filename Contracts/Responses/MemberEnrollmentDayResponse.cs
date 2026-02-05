namespace BurhaniGuards.Api.Contracts.Responses;

public record MemberEnrollmentDayResponse(
    int Day,
    string Status,
    string? FinalStatus,
    string MiqaatDate
);
