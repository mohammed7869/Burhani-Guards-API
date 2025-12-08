namespace BurhaniGuards.Api.Contracts.Requests;

public record LoginRequest(string? ItsNumber, string? Email, string Password);

