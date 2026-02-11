namespace BurhaniGuards.Api.Contracts.Requests;

public class ForgotPasswordRequest
{
    public string ItsNumber { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    public string ItsNumber { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string ItsNumber { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
