using System.Net;
using System.Net.Mail;

namespace BurhaniGuards.Api.Services;

public class EmailService : IEmailService
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration)
    {
        var emailConfig = configuration.GetSection("Email");
        _smtpServer = emailConfig["SmtpServer"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(emailConfig["SmtpPort"] ?? "587");
        _smtpUsername = emailConfig["SmtpUsername"] ?? "bgpoona.jamiat53@gmail.com";
        _smtpPassword = emailConfig["SmtpPassword"] ?? "wuqftuejyquiadjt";
        _fromEmail = emailConfig["FromEmail"] ?? "bgpoona.jamiat53@gmail.com";
        _fromName = emailConfig["FromName"] ?? "Burhani Guards Pune";
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            return; // Skip if no email address
        }

        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_fromEmail, _fromName);
            message.To.Add(new MailAddress(to));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;

            using var client = new SmtpClient(_smtpServer, _smtpPort);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - email failures shouldn't break the main flow
            System.Diagnostics.Debug.WriteLine($"Failed to send email to {to}: {ex.Message}");
        }
    }

    public async Task SendBulkEmailAsync(List<string> toList, string subject, string body, bool isHtml = true)
    {
        if (toList == null || !toList.Any())
        {
            return;
        }

        // Remove duplicates and empty emails
        var uniqueEmails = toList
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Distinct()
            .ToList();

        if (!uniqueEmails.Any())
        {
            return;
        }

        // Send emails in parallel for better performance
        var tasks = uniqueEmails.Select(email => SendEmailAsync(email, subject, body, isHtml));
        await Task.WhenAll(tasks);
    }
}


