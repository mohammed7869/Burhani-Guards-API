namespace BurhaniGuards.Api.Contracts.Responses;

public record ForgotPasswordResponse(
    bool Success,
    string Message,
    string MaskedEmail
);

public record VerifyOtpResponse(
    bool Success,
    string Message
);

public record ResetPasswordResponse(
    bool Success,
    string Message
);
