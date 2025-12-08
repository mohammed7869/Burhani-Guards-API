namespace BurhaniGuards.Api.Contracts.Requests;

public record ChangePasswordRequest(string ItsNumber, string NewPassword, string ConfirmPassword);

