namespace BurhaniGuards.Api.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendBulkEmailAsync(List<string> toList, string subject, string body, bool isHtml = true);
}

