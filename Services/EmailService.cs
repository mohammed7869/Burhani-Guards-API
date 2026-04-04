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
    private readonly ILogger<EmailService> _logger;

    // Batching configuration
    private const int BatchSize = 45;              // Send 45 emails per batch
    private const int DelayBetweenEmailsMs = 2500; // 2.5s between each email
    private const int DelayBetweenBatchesMs = 45000; // 45s pause between batches

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        var emailConfig = configuration.GetSection("Email");
        _smtpServer = emailConfig["SmtpServer"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(emailConfig["SmtpPort"] ?? "587");
        _smtpUsername = emailConfig["SmtpUsername"] ?? "bgpoona.jamiat53@gmail.com";
        _smtpPassword = emailConfig["SmtpPassword"] ?? "gwmzdeawkassevmv";
        _fromEmail = emailConfig["FromEmail"] ?? "bgpoona.jamiat53@gmail.com";
        _fromName = emailConfig["FromName"] ?? "Burhani Guards Pune";
    }

    /// <summary>
    /// Creates a fresh SmtpClient with proper settings.
    /// </summary>
    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_smtpServer, _smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000 // 30 second timeout per send
        };
        return client;
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

            using var client = CreateSmtpClient();
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - email failures shouldn't break the main flow
            _logger.LogWarning("Failed to send email to {To}: {Error}", to, ex.Message);
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
            .Select(email => email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!uniqueEmails.Any())
        {
            return;
        }

        _logger.LogInformation("Starting bulk email send: {TotalCount} recipients, batch size {BatchSize}", 
            uniqueEmails.Count, BatchSize);

        int totalSent = 0;
        int totalFailed = 0;

        // Split into batches
        var batches = uniqueEmails
            .Select((email, index) => new { email, index })
            .GroupBy(x => x.index / BatchSize)
            .Select(g => g.Select(x => x.email).ToList())
            .ToList();

        for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
        {
            var batch = batches[batchIndex];
            _logger.LogInformation("Processing batch {BatchNum}/{TotalBatches} ({BatchCount} emails)", 
                batchIndex + 1, batches.Count, batch.Count);

            int batchSent = 0;
            int batchFailed = 0;

            // Create a single SmtpClient per batch for connection reuse
            using var client = CreateSmtpClient();

            for (int i = 0; i < batch.Count; i++)
            {
                var email = batch[i];
                try
                {
                    using var message = new MailMessage();
                    message.From = new MailAddress(_fromEmail, _fromName);
                    message.To.Add(new MailAddress(email));
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = isHtml;

                    await client.SendMailAsync(message);
                    batchSent++;
                    totalSent++;
                }
                catch (SmtpException smtpEx)
                {
                    batchFailed++;
                    totalFailed++;
                    _logger.LogWarning("SMTP error sending to {Email}: {StatusCode} - {Error}", 
                        email, smtpEx.StatusCode, smtpEx.Message);

                    // If we get a rate-limit / service-unavailable error, pause longer before continuing
                    if (smtpEx.StatusCode == SmtpStatusCode.ServiceNotAvailable ||
                        smtpEx.StatusCode == SmtpStatusCode.MailboxBusy ||
                        smtpEx.StatusCode == SmtpStatusCode.InsufficientStorage)
                    {
                        _logger.LogWarning("Gmail throttle detected, pausing for 60 seconds...");
                        await Task.Delay(60000);
                    }
                }
                catch (Exception ex)
                {
                    batchFailed++;
                    totalFailed++;
                    _logger.LogWarning("Failed to send email to {Email}: {Error}", email, ex.Message);
                }

                // Delay between individual emails (skip delay after last email in batch)
                if (i < batch.Count - 1)
                {
                    await Task.Delay(DelayBetweenEmailsMs);
                }
            }

            _logger.LogInformation("Batch {BatchNum} complete: {Sent} sent, {Failed} failed", 
                batchIndex + 1, batchSent, batchFailed);

            // Pause between batches (skip pause after last batch)
            if (batchIndex < batches.Count - 1)
            {
                _logger.LogInformation("Pausing {Seconds}s before next batch...", DelayBetweenBatchesMs / 1000);
                await Task.Delay(DelayBetweenBatchesMs);
            }
        }

        _logger.LogInformation("Bulk email complete: {TotalSent} sent, {TotalFailed} failed out of {Total} total", 
            totalSent, totalFailed, uniqueEmails.Count);
    }
}
