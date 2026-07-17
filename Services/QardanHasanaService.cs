using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Repositories;
using BurhaniGuards.Api.ViewModel;
using BurhaniGuards.Api.BusinessModel;
using System.Text.Json;

namespace BurhaniGuards.Api.Services;

public class QardanHasanaService : IQardanHasanaService
{
    private readonly IQardanHasanaRepository _repository;
    private readonly IDapperMemberRepository _memberRepository;
    private readonly IEmailService _emailService;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<QardanHasanaService> _logger;

    public QardanHasanaService(
        IQardanHasanaRepository repository,
        IDapperMemberRepository memberRepository,
        IEmailService emailService,
        IActivityLogService activityLogService,
        ILogger<QardanHasanaService> logger)
    {
        _repository = repository;
        _memberRepository = memberRepository;
        _emailService = emailService;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<QardanHasanaResponse> Create(CreateQardanHasanaRequest request, CurrentUserViewModel currentUser)
    {
        // Validate
        if (!request.TermsAccepted)
            throw new Exception("You must accept the Terms and Conditions.");

        if (request.AmountRequested <= 0 || request.AmountRequested > 20000)
            throw new Exception("Amount must be between ₹1 and ₹20,000.");

        if (string.IsNullOrWhiteSpace(request.ApplicantName))
            throw new Exception("Name (As Per Bank) is required.");

        if (string.IsNullOrWhiteSpace(request.ApplicantMobile))
            throw new Exception("Mobile No is required.");

        if (request.Guarantor1MemberId <= 0)
            throw new Exception("Guarantor 1 is required.");

        if (request.GuarantorMemberId <= 0)
            throw new Exception("Guarantor 2 is required.");

        if (request.Guarantor1MemberId == request.GuarantorMemberId)
            throw new Exception("Guarantor 1 and Guarantor 2 cannot be the same person.");

        // Check for existing active application
        var hasActive = await _repository.HasActiveApplication(currentUser.id);
        if (hasActive)
            throw new Exception("You already have an active Qardan Hasana application. You cannot apply again until your current application is completed or rejected.");

        // Get Guarantor 1 member details
        var guarantor1Member = await _repository.GetMemberById(request.Guarantor1MemberId);
        if (guarantor1Member == null)
            throw new Exception("Selected Guarantor 1 member not found.");

        // Get Guarantor 2 member details
        var guarantor2Member = await _repository.GetMemberById(request.GuarantorMemberId);
        if (guarantor2Member == null)
            throw new Exception("Selected Guarantor 2 member not found.");

        // Get applicant member details for JamaatId
        var applicantMember = await _repository.GetMemberById(currentUser.id);

        // Generate application number: QH-YYYYMMDD-XXXX (using IST)
        var count = await _repository.GetNextApplicationCount();
        var applicationNo = $"QH-{GetIstNow():yyyyMMdd}-{count:D4}";

        // Create application (Guarantor 1 stored in captain_* columns)
        var id = await _repository.Create(
            applicationNo: applicationNo,
            applicantMemberId: currentUser.id,
            applicantItsId: currentUser.itsId ?? "",
            applicantName: request.ApplicantName,
            applicantJamaat: currentUser.jamaat ?? "",
            applicantJamaatId: applicantMember?.JamaatId,
            applicantOccupation: request.ApplicantOccupation,
            applicantMobile: request.ApplicantMobile,
            reason: request.Reason,
            amountRequested: request.AmountRequested,
            applicantSignatureUrl: null,
            applicantPhotoUrl: null,
            termsAccepted: request.TermsAccepted,
            captainMemberId: guarantor1Member.Id,
            captainName: guarantor1Member.FullName,
            captainMobile: guarantor1Member.Contact,
            guarantorMemberId: request.GuarantorMemberId,
            guarantorName: guarantor2Member.FullName,
            guarantorMobile: guarantor2Member.Contact
        );

        _logger.LogInformation("Qardan Hasana application {ApplicationNo} created by {ApplicantName} (ID: {MemberId})",
            applicationNo, request.ApplicantName, currentUser.id);

        // Activity Log
        _ = Task.Run(async () =>
        {
            try
            {
                await _activityLogService.LogAsync(new ActivityLogModel
                {
                    EntityType = ActivityEntityType.QardanHasana,
                    EntityId = id,
                    Action = ActivityAction.QardanHasanaSubmitted,
                    PerformedBy = request.ApplicantName,
                    PerformedById = currentUser.id,
                    PerformedByRole = currentUser.roles == 2 ? "Captain" : "Member",
                    TargetMemberId = currentUser.id,
                    TargetMemberName = $"{request.ApplicantName} (ITS: {currentUser.itsId})",
                    NewValue = $"₹{request.AmountRequested:N0}",
                    Details = JsonSerializer.Serialize(new { applicationNo, applicantName = request.ApplicantName, amountRequested = request.AmountRequested, reason = request.Reason, guarantor1 = guarantor1Member.FullName, guarantor2 = guarantor2Member.FullName }),
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to log Qardan Hasana submission activity for {ApplicationNo}: {Error}",
                    applicationNo, ex.Message);
            }
        });

        // Send emails to both guarantors (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await SendApplicationEmails(applicationNo, request.ApplicantName, request.AmountRequested,
                    request.Reason, currentUser.email,
                    guarantor1Member.FullName, guarantor1Member.Email,
                    guarantor2Member.FullName, guarantor2Member.Email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to send Qardan Hasana emails for {ApplicationNo}: {Error}",
                    applicationNo, ex.Message);
            }
        });

        // Return the created application
        var result = await _repository.GetById(id);
        return result!;
    }

    public async Task<QardanHasanaResponse?> GetById(int id)
    {
        return await _repository.GetById(id);
    }

    public async Task<List<QardanHasanaListResponse>> GetAll(string? statusFilter = null)
    {
        return await _repository.GetAll(statusFilter);
    }

    public async Task<List<QardanHasanaListResponse>> GetMyApplications(int memberId)
    {
        return await _repository.GetByApplicantId(memberId);
    }

    public async Task<List<QardanHasanaListResponse>> GetByJamaat(string jamaat)
    {
        return await _repository.GetByJamaat(jamaat);
    }

    public async Task<List<QardanHasanaListResponse>> GetGuarantorApplications(int memberId)
    {
        return await _repository.GetByGuarantorId(memberId);
    }

    public async Task Sanction(int id, SanctionQardanHasanaRequest request,
        string? adminSignatureUrl, string? adminFormImageUrl, int adminId)
    {
        var application = await _repository.GetById(id);
        if (application == null)
            throw new Exception("Application not found.");

        if (application.Status == "sanctioned")
            throw new Exception("Application is already sanctioned.");

        if (application.Status == "rejected")
            throw new Exception("Cannot sanction a rejected application.");

        // Both guarantors must have approved before admin can sanction
        if (!application.CaptainApproved)
            throw new Exception("Guarantor 1 has not approved this application yet.");

        if (!application.GuarantorApproved)
            throw new Exception("Guarantor 2 has not approved this application yet.");

        if (request.SanctionedAmount <= 0 || request.SanctionedAmount > 20000)
            throw new Exception("Sanctioned amount must be between ₹1 and ₹20,000.");

        await _repository.Sanction(id,
            request.SanctionedAmount,
            request.InstallmentAmount,
            request.NumberOfMonths,
            request.InstallmentDateFrom,
            request.InstallmentDateTo,
            adminSignatureUrl,
            adminFormImageUrl,
            adminId);

        _logger.LogInformation("Qardan Hasana {ApplicationNo} sanctioned by Admin {AdminId}. Amount: {Amount}",
            application.ApplicationNo, adminId, request.SanctionedAmount);

        // Activity Log for sanctioning
        var adminMember = await _repository.GetMemberById(adminId);
        var adminName = adminMember?.FullName ?? "Admin";

        _ = Task.Run(async () =>
        {
            try
            {
                await _activityLogService.LogAsync(new ActivityLogModel
                {
                    EntityType = ActivityEntityType.QardanHasana,
                    EntityId = id,
                    Action = ActivityAction.QardanHasanaSanctioned,
                    PerformedBy = adminName,
                    PerformedById = adminId,
                    PerformedByRole = "Resource Admin",
                    TargetMemberId = application.ApplicantMemberId,
                    TargetMemberName = $"{application.ApplicantName} (ITS: {application.ApplicantItsId})",
                    NewValue = $"₹{request.SanctionedAmount:N0}",
                    Details = JsonSerializer.Serialize(new { applicationNo = application.ApplicationNo, applicantName = application.ApplicantName, amountRequested = application.AmountRequested, sanctionedAmount = request.SanctionedAmount, installmentAmount = request.InstallmentAmount, numberOfMonths = request.NumberOfMonths }),
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to log Qardan Hasana sanction activity for {AppNo}: {Error}",
                    application.ApplicationNo, ex.Message);
            }
        });
    }

    public async Task Reject(int id, RejectQardanHasanaRequest request, int adminId)
    {
        var application = await _repository.GetById(id);
        if (application == null)
            throw new Exception("Application not found.");

        if (application.Status == "sanctioned")
            throw new Exception("Cannot reject a sanctioned application.");

        if (application.Status == "rejected")
            throw new Exception("Application is already rejected.");

        await _repository.Reject(id, request.Reason, adminId);

        _logger.LogInformation("Qardan Hasana {ApplicationNo} rejected by Admin {AdminId}. Reason: {Reason}",
            application.ApplicationNo, adminId, request.Reason);

        // Activity Log for admin rejection
        var adminMemberInfo = await _repository.GetMemberById(adminId);
        var rejectAdminName = adminMemberInfo?.FullName ?? "Admin";

        _ = Task.Run(async () =>
        {
            try
            {
                await _activityLogService.LogAsync(new ActivityLogModel
                {
                    EntityType = ActivityEntityType.QardanHasana,
                    EntityId = id,
                    Action = ActivityAction.QardanHasanaRejected,
                    PerformedBy = rejectAdminName,
                    PerformedById = adminId,
                    PerformedByRole = "Resource Admin",
                    TargetMemberId = application.ApplicantMemberId,
                    TargetMemberName = $"{application.ApplicantName} (ITS: {application.ApplicantItsId})",
                    NewValue = "Rejected",
                    Details = JsonSerializer.Serialize(new { applicationNo = application.ApplicationNo, applicantName = application.ApplicantName, amountRequested = application.AmountRequested, reason = request.Reason }),
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to log Qardan Hasana rejection activity for {AppNo}: {Error}",
                    application.ApplicationNo, ex.Message);
            }
        });
    }

    public async Task<List<JamaatMemberResponse>> GetMembersByJamaat(string jamaat, int excludeMemberId)
    {
        return await _repository.GetMembersByJamaat(jamaat, excludeMemberId);
    }

    public async Task<JamaatMemberResponse?> GetCaptainByJamaat(string jamaat)
    {
        return await _repository.GetCaptainByJamaat(jamaat);
    }

    public async Task<byte[]> GeneratePdf(int id)
    {
        var application = await _repository.GetById(id);
        if (application == null)
            throw new Exception("Application not found.");

        return GenerateQardanHasanaPdf(application);
    }

    #region Private Methods

    private async Task SendApplicationEmails(
        string applicationNo, string applicantName, decimal amount,
        string? reason, string applicantEmail,
        string guarantor1Name, string guarantor1Email,
        string guarantor2Name, string guarantor2Email)
    {
        var subject = $"Qardan Hasana Application - {applicationNo}";

        // Email to applicant
        var applicantBody = BuildEmailHtml(
            $"Dear {applicantName},",
            $@"Your Qardan Hasana application <strong>{applicationNo}</strong> has been submitted successfully.
            <br/><br/>
            <strong>Amount Requested:</strong> ₹{amount:N2}<br/>
            <strong>Reason:</strong> {reason ?? "N/A"}<br/><br/>
            Both your Guarantors ({guarantor1Name} and {guarantor2Name}) will receive an email to review and approve your application from the BGP app.<br/><br/>
            Once both guarantors approve, the application will be forwarded to the Resource Admin for sanctioning.");

        // Notification email disabled per request (only Miqaat creation and attendance emails are sent).
        // await _emailService.SendEmailAsync(applicantEmail, subject, applicantBody);

        // Email to Guarantor 1
        if (!string.IsNullOrWhiteSpace(guarantor1Email))
        {
            var g1Body = BuildEmailHtml(
                $"Dear {guarantor1Name},",
                $@"You have been selected as <strong>Guarantor 1</strong> for a Qardan Hasana application.
                <br/><br/>
                <strong>Application No:</strong> {applicationNo}<br/>
                <strong>Applicant:</strong> {applicantName}<br/>
                <strong>Amount:</strong> ₹{amount:N2}<br/>
                <strong>Reason:</strong> {reason ?? "N/A"}<br/><br/>
                Please open the BGP app, review the application details, and approve or reject this request.
                By approving, you accept joint responsibility for repayment in case of default as per BGP Qardan Hasana terms.");

            // Notification email disabled per request (only Miqaat creation and attendance emails are sent).
            // await _emailService.SendEmailAsync(guarantor1Email, subject, g1Body);
        }

        // Email to Guarantor 2
        if (!string.IsNullOrWhiteSpace(guarantor2Email))
        {
            var g2Body = BuildEmailHtml(
                $"Dear {guarantor2Name},",
                $@"You have been selected as <strong>Guarantor 2</strong> for a Qardan Hasana application.
                <br/><br/>
                <strong>Application No:</strong> {applicationNo}<br/>
                <strong>Applicant:</strong> {applicantName}<br/>
                <strong>Amount:</strong> ₹{amount:N2}<br/>
                <strong>Reason:</strong> {reason ?? "N/A"}<br/><br/>
                Please open the BGP app, review the application details, and approve or reject this request.
                By approving, you accept joint responsibility for repayment in case of default as per BGP Qardan Hasana terms.");

            // Notification email disabled per request (only Miqaat creation and attendance emails are sent).
            // await _emailService.SendEmailAsync(guarantor2Email, subject, g2Body);
        }
    }

    private static string BuildEmailHtml(string greeting, string body)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <div style='background: linear-gradient(135deg, #1a5632, #2d8a4e); padding: 20px; border-radius: 8px 8px 0 0;'>
                    <h2 style='color: white; margin: 0; text-align: center;'>Burhani Guards Pune</h2>
                    <p style='color: #e0e0e0; margin: 5px 0 0; text-align: center; font-size: 14px;'>Qardan Hasana</p>
                </div>
                <div style='background: #ffffff; padding: 25px; border: 1px solid #e0e0e0; border-top: none;'>
                    <p>{greeting}</p>
                    <p>{body}</p>
                </div>
                <div style='background: #f5f5f5; padding: 15px; border-radius: 0 0 8px 8px; text-align: center; font-size: 12px; color: #888;'>
                    <p>This is an automated email from Burhani Guards Pune. Please do not reply.</p>
                </div>
            </div>
        </body>
        </html>";
    }

    private static byte[] GenerateQardanHasanaPdf(QardanHasanaResponse app)
    {
        // Simple HTML-to-text PDF generation using basic approach
        // In production, consider using a library like QuestPDF or iTextSharp
        var htmlContent = $@"
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; padding: 30px; font-size: 12px; }}
                .header {{ text-align: center; margin-bottom: 20px; }}
                .header h1 {{ font-size: 18px; margin: 0; color: #1a5632; }}
                .header h2 {{ font-size: 14px; margin: 5px 0; }}
                .header p {{ font-size: 11px; color: #666; }}
                table {{ width: 100%; border-collapse: collapse; margin: 10px 0; }}
                td {{ padding: 6px 10px; border: 1px solid #ccc; vertical-align: top; }}
                td.label {{ font-weight: bold; width: 35%; background: #f9f9f9; }}
                .section {{ background: #1a5632; color: white; padding: 6px 10px; font-weight: bold; margin-top: 15px; }}
                .signature-box {{ display: inline-block; width: 200px; height: 80px; border: 1px solid #ccc; margin: 10px; text-align: center; padding-top: 50px; font-size: 10px; color: #999; }}
                .photo-box {{ width: 120px; height: 150px; border: 1px solid #ccc; text-align: center; padding-top: 60px; font-size: 10px; color: #999; float: right; }}
                .terms {{ font-size: 10px; margin-top: 15px; }}
                .terms ol {{ padding-left: 20px; }}
                .terms li {{ margin-bottom: 5px; }}
                .office-use td {{ border: 1px solid #ccc; }}
            </style>
        </head>
        <body>
            <div class='header'>
                <h1>BURHANI GUARDS PUNE</h1>
                <h2>Qardan Hasana Application Form</h2>
                <p>Application No: {app.ApplicationNo}</p>
            </div>

            <div class='photo-box'>Applicant Photo</div>

            <div class='section'>APPLICANT INFORMATION</div>
            <table>
                <tr><td class='label'>Application No</td><td>{app.ApplicationNo}</td></tr>
                <tr><td class='label'>Date</td><td>{app.CreatedAt:dd-MM-yyyy}</td></tr>
                <tr><td class='label'>Mohallah</td><td>{app.ApplicantJamaat}</td></tr>
                <tr><td class='label'>ITS No</td><td>{app.ApplicantItsId}</td></tr>
                <tr><td class='label'>Name (As Per Bank)</td><td>{app.ApplicantName}</td></tr>
                <tr><td class='label'>Occupation</td><td>{app.ApplicantOccupation ?? ""}</td></tr>
                <tr><td class='label'>Mobile No</td><td>{app.ApplicantMobile}</td></tr>
                <tr><td class='label'>Reason</td><td>{app.Reason ?? ""}</td></tr>
                <tr><td class='label'>Amount (In Figure)</td><td>₹ {app.AmountRequested:N2}</td></tr>
            </table>

            <div style='margin-top: 10px;'>
                <div class='signature-box'>Applicant Signature</div>
            </div>

            <div class='section'>GUARANTOR SECTION</div>
            <table>
                <tr>
                    <td colspan='2' style='background: #e8f5e9; font-weight: bold;'>1. Guarantor 1</td>
                </tr>
                <tr><td class='label'>Name</td><td>{app.CaptainName}</td></tr>
                <tr><td class='label'>Mobile No</td><td>{app.CaptainMobile ?? ""}</td></tr>
                <tr><td class='label'>Signature</td><td style='height: 60px;'></td></tr>
                <tr>
                    <td colspan='2' style='background: #e8f5e9; font-weight: bold;'>2. Guarantor 2</td>
                </tr>
                <tr><td class='label'>Name</td><td>{app.GuarantorName}</td></tr>
                <tr><td class='label'>Mobile No</td><td>{app.GuarantorMobile ?? ""}</td></tr>
                <tr><td class='label'>Signature</td><td style='height: 60px;'></td></tr>
            </table>

            <div class='section'>FOR OFFICE USE ONLY</div>
            <table class='office-use'>
                <tr><td class='label'>Sanctioned Amount</td><td>{(app.SanctionedAmount.HasValue ? $"₹ {app.SanctionedAmount:N2}" : "")}</td></tr>
                <tr><td class='label'>Installment Amount</td><td>{(app.InstallmentAmount.HasValue ? $"₹ {app.InstallmentAmount:N2}" : "")}</td></tr>
                <tr><td class='label'>No. of Months</td><td>{app.NumberOfMonths?.ToString() ?? ""}</td></tr>
                <tr><td class='label'>Installment Date From</td><td>{app.InstallmentDateFrom?.ToString("dd-MM-yyyy") ?? ""}</td></tr>
                <tr><td class='label'>Installment Date To</td><td>{app.InstallmentDateTo?.ToString("dd-MM-yyyy") ?? ""}</td></tr>
                <tr><td class='label'>Admin Signature</td><td style='height: 60px;'></td></tr>
            </table>

            <div class='section'>TERMS & CONDITIONS</div>
            <div class='terms'>
                <ol>
                    <li><strong>Nature of Qardan:</strong> The amount given under this scheme is Qardan Hasana (interest-free), strictly for need-based assistance. No interest, profit, service charge, or benefit shall be charged by BGP.</li>
                    <li><strong>Eligibility:</strong> Only registered BGP members are eligible. The applicant must belong to the same Mohallah under which the application is submitted. No pending dues or unresolved disciplinary matters.</li>
                    <li><strong>Qardan Amount:</strong> Maximum ₹20,000. Sanctioned amount may be less than requested.</li>
                    <li><strong>Purpose:</strong> Must be used for genuine and lawful purposes only. BGP reserves the right to seek clarification.</li>
                    <li><strong>Guarantors:</strong> Two guarantors mandatory from the same Mohallah. Must be financially responsible with no outstanding Qardan dues. Accept joint responsibility for repayment.</li>
                    <li><strong>Repayment:</strong> Without delay as per agreed schedule. Lump sum or monthly installments. Early repayment encouraged. No extension without prior written approval.</li>
                    <li><strong>Default & Recovery:</strong> Verbal/written reminders. Guarantors informed. Guarantors liable for outstanding amount. No interest or penalty.</li>
                    <li><strong>Discipline:</strong> Repeated default may result in disqualification from BGP benefits. False information results in immediate cancellation.</li>
                    <li><strong>Documentation:</strong> Valid ID proof as required. Signatures/thumb impressions on form. BGP maintains records.</li>
                    <li><strong>Discretion:</strong> Approval/rejection at sole discretion of BGP. BGP may modify scheme rules.</li>
                    <li><strong>Declaration:</strong> I declare all information is true and correct. I understand this is Qardan Hasana, entrusted as amanat, and I take full responsibility to repay within agreed time.</li>
                </ol>
            </div>

            <div style='margin-top: 20px; text-align: center; font-size: 10px; color: #999;'>
                <p>This document is generated by Burhani Guards Pune - Qardan Hasana Module</p>
            </div>
        </body>
        </html>";

        // Convert HTML content to bytes (this is a simplified approach)
        // The actual HTML will be returned and can be converted to PDF by a library
        return System.Text.Encoding.UTF8.GetBytes(htmlContent);
    }

    #endregion

    public async Task<QardanHasanaResponse> UpdateApplication(int id, UpdateQardanHasanaRequest request, CurrentUserViewModel currentUser)
    {
        // Get existing application
        var application = await _repository.GetById(id);
        if (application == null)
            throw new Exception("Application not found.");

        // Determine who is editing
        var isApplicant = application.ApplicantMemberId == currentUser.id;
        var isGuarantor1 = application.CaptainMemberId == currentUser.id;
        var isGuarantor2 = application.GuarantorMemberId == currentUser.id;
        var isAdmin = currentUser.roles == 7; // Resource Admin

        // Only the applicant, assigned guarantors, or admin can edit
        if (!isApplicant && !isGuarantor1 && !isGuarantor2 && !isAdmin)
            throw new Exception("Only the applicant, assigned Guarantors, or Admin can edit this application.");

        // Cannot edit if any guarantor has already approved (unless admin)
        if (!isAdmin && (application.CaptainApproved || application.GuarantorApproved))
            throw new Exception("This application has already been approved by a Guarantor and cannot be edited.");

        // Cannot edit if status is not pending
        if (application.Status != "pending")
            throw new Exception("This application can no longer be edited.");

        // Validate
        if (string.IsNullOrWhiteSpace(request.ApplicantName))
            throw new Exception("Name (As Per Bank) is required.");

        if (string.IsNullOrWhiteSpace(request.ApplicantMobile))
            throw new Exception("Mobile No is required.");

        if (request.AmountRequested <= 0 || request.AmountRequested > 20000)
            throw new Exception("Amount must be between ₹1 and ₹20,000.");

        if (request.Guarantor1MemberId <= 0)
            throw new Exception("Guarantor 1 is required.");

        if (request.GuarantorMemberId <= 0)
            throw new Exception("Guarantor 2 is required.");

        if (request.Guarantor1MemberId == request.GuarantorMemberId)
            throw new Exception("Guarantor 1 and Guarantor 2 cannot be the same person.");

        // Get guarantor member details
        var guarantor1Member = await _repository.GetMemberById(request.Guarantor1MemberId);
        if (guarantor1Member == null)
            throw new Exception("Selected Guarantor 1 member not found.");

        var guarantor2Member = await _repository.GetMemberById(request.GuarantorMemberId);
        if (guarantor2Member == null)
            throw new Exception("Selected Guarantor 2 member not found.");

        // Build change details for audit log
        var changes = new List<string>();
        if (application.ApplicantName != request.ApplicantName)
            changes.Add($"Name: {application.ApplicantName} → {request.ApplicantName}");
        if ((application.ApplicantOccupation ?? "") != (request.ApplicantOccupation ?? ""))
            changes.Add($"Occupation: {application.ApplicantOccupation ?? "—"} → {request.ApplicantOccupation ?? "—"}");
        if (application.ApplicantMobile != request.ApplicantMobile)
            changes.Add($"Mobile: {application.ApplicantMobile} → {request.ApplicantMobile}");
        if ((application.Reason ?? "") != (request.Reason ?? ""))
            changes.Add($"Reason: {application.Reason ?? "—"} → {request.Reason ?? "—"}");
        if (application.AmountRequested != request.AmountRequested)
            changes.Add($"Amount: ₹{application.AmountRequested:N0} → ₹{request.AmountRequested:N0}");
        if (application.CaptainMemberId != request.Guarantor1MemberId)
            changes.Add($"Guarantor 1: {application.CaptainName} → {guarantor1Member.FullName}");
        if (application.GuarantorMemberId != request.GuarantorMemberId)
            changes.Add($"Guarantor 2: {application.GuarantorName} → {guarantor2Member.FullName}");

        // Update the application (resets both guarantor approvals)
        await _repository.UpdateApplication(id,
            request.ApplicantName,
            request.ApplicantOccupation,
            request.ApplicantMobile,
            request.Reason,
            request.AmountRequested,
            guarantor1Member.Id,
            guarantor1Member.FullName,
            guarantor1Member.Contact,
            request.GuarantorMemberId,
            guarantor2Member.FullName,
            guarantor2Member.Contact);

        // Determine editor info
        string editorName;
        string editorRole;
        if (isAdmin)
        {
            var adminMember = await _repository.GetMemberById(currentUser.id);
            editorName = adminMember?.FullName ?? "Admin";
            editorRole = "Admin";
        }
        else if (isGuarantor1 || isGuarantor2)
        {
            editorName = isGuarantor1 ? application.CaptainName : application.GuarantorName;
            editorRole = "Guarantor";
        }
        else
        {
            editorName = request.ApplicantName;
            editorRole = "Member";
        }

        _logger.LogInformation("Qardan Hasana application {AppNo} edited by {Editor} ({Role}, ID: {MemberId}). Changes: {Changes}",
            application.ApplicationNo, editorName, editorRole, currentUser.id, string.Join("; ", changes));

        // Activity Log
        var activityAction = isAdmin
            ? ActivityAction.QardanHasanaAdminEdited
            : (isGuarantor1 || isGuarantor2 ? ActivityAction.QardanHasanaCaptainEdited : ActivityAction.QardanHasanaEdited);

        _ = Task.Run(async () =>
        {
            try
            {
                await _activityLogService.LogAsync(new ActivityLogModel
                {
                    EntityType = ActivityEntityType.QardanHasana,
                    EntityId = id,
                    Action = activityAction,
                    PerformedBy = editorName,
                    PerformedById = currentUser.id,
                    PerformedByRole = editorRole,
                    TargetMemberId = application.ApplicantMemberId,
                    TargetMemberName = $"{application.ApplicantName} (ITS: {application.ApplicantItsId})",
                    OldValue = $"₹{application.AmountRequested:N0}",
                    NewValue = $"₹{request.AmountRequested:N0}",
                    Details = JsonSerializer.Serialize(new { applicationNo = application.ApplicationNo, editedBy = editorRole, changes }),
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to log Qardan Hasana edit activity for {AppNo}: {Error}",
                    application.ApplicationNo, ex.Message);
            }
        });

        // Send notification emails if there are changes (fire-and-forget)
        if (changes.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var changesHtml = string.Join("<br/>", changes.Select(c => $"• {c}"));
                    var subject = $"Qardan Hasana Application Edited - {application.ApplicationNo}";

                    // Notify applicant
                    var applicantMemberInfo = await _repository.GetMemberById(application.ApplicantMemberId);
                    if (applicantMemberInfo != null && !string.IsNullOrWhiteSpace(applicantMemberInfo.Email))
                    {
                        var applicantBody = BuildEmailHtml(
                            $"Dear {application.ApplicantName},",
                            $@"Your Qardan Hasana application <strong>{application.ApplicationNo}</strong> has been edited by {editorName} ({editorRole}).
                            <br/><br/>
                            <strong>Changes Made:</strong><br/>
                            {changesHtml}<br/><br/>
                            Both guarantors will need to re-approve this application.");

                        // Notification email disabled per request (only Miqaat creation and attendance emails are sent).
                        // await _emailService.SendEmailAsync(applicantMemberInfo.Email, subject, applicantBody);
                    }

                    // Notify both guarantors
                    var g1Info = await _repository.GetMemberById(guarantor1Member.Id);
                    if (g1Info != null && !string.IsNullOrWhiteSpace(g1Info.Email))
                    {
                        var g1Body = BuildEmailHtml(
                            $"Dear {guarantor1Member.FullName},",
                            $@"The Qardan Hasana application <strong>{application.ApplicationNo}</strong> has been edited.
                            <br/><br/>
                            <strong>Changes Made:</strong><br/>
                            {changesHtml}<br/><br/>
                            Please review and re-approve this application in the BGP app.");

                        // Notification email disabled per request (only Miqaat creation and attendance emails are sent).
                        // await _emailService.SendEmailAsync(g1Info.Email, subject, g1Body);
                    }

                    var g2Info = await _repository.GetMemberById(guarantor2Member.Id);
                    if (g2Info != null && !string.IsNullOrWhiteSpace(g2Info.Email))
                    {
                        var g2Body = BuildEmailHtml(
                            $"Dear {guarantor2Member.FullName},",
                            $@"The Qardan Hasana application <strong>{application.ApplicationNo}</strong> has been edited.
                            <br/><br/>
                            <strong>Changes Made:</strong><br/>
                            {changesHtml}<br/><br/>
                            Please review and re-approve this application in the BGP app.");

                        // Notification email disabled per request (only Miqaat creation and attendance emails are sent).
                        // await _emailService.SendEmailAsync(g2Info.Email, subject, g2Body);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to send Qardan Hasana edit emails for {AppNo}: {Error}",
                        application.ApplicationNo, ex.Message);
                }
            });
        }

        // Return updated application
        var result = await _repository.GetById(id);
        return result!;
    }

    public async Task GuarantorApprove(int applicationId, int guarantorMemberId)
    {
        // Get application
        var application = await _repository.GetById(applicationId);
        if (application == null)
            throw new Exception("Application not found.");

        if (application.Status == "rejected")
            throw new Exception("This application has been rejected.");

        // Determine which guarantor is calling
        var isGuarantor1 = application.CaptainMemberId == guarantorMemberId;
        var isGuarantor2 = application.GuarantorMemberId == guarantorMemberId;

        if (!isGuarantor1 && !isGuarantor2)
            throw new Exception("Only an assigned Guarantor can approve this application.");

        // Check if already approved
        if (isGuarantor1 && application.CaptainApproved)
            throw new Exception("You have already approved this application as Guarantor 1.");

        if (isGuarantor2 && application.GuarantorApproved)
            throw new Exception("You have already approved this application as Guarantor 2.");

        // Mark as approved
        if (isGuarantor1)
        {
            await _repository.CaptainApprove(applicationId);
        }
        else
        {
            await _repository.GuarantorApprove(applicationId);
        }

        var guarantorLabel = isGuarantor1 ? "Guarantor 1" : "Guarantor 2";
        var guarantorName = isGuarantor1 ? application.CaptainName : application.GuarantorName;

        _logger.LogInformation("Qardan Hasana application {Id} ({AppNo}) {GuarantorLabel} approved by member {GuarantorId}",
            applicationId, application.ApplicationNo, guarantorLabel, guarantorMemberId);

        // Activity Log for guarantor approval
        _ = Task.Run(async () =>
        {
            try
            {
                await _activityLogService.LogAsync(new ActivityLogModel
                {
                    EntityType = ActivityEntityType.QardanHasana,
                    EntityId = applicationId,
                    Action = isGuarantor1 ? ActivityAction.QardanHasanaGuarantor1Approved : ActivityAction.QardanHasanaGuarantorApproved,
                    PerformedBy = guarantorName,
                    PerformedById = guarantorMemberId,
                    PerformedByRole = guarantorLabel,
                    TargetMemberId = application.ApplicantMemberId,
                    TargetMemberName = $"{application.ApplicantName} (ITS: {application.ApplicantItsId})",
                    NewValue = "Approved",
                    Details = JsonSerializer.Serialize(new { applicationNo = application.ApplicationNo, applicantName = application.ApplicantName, amountRequested = application.AmountRequested, guarantorRole = guarantorLabel }),
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to log Qardan Hasana guarantor approval activity for {AppNo}: {Error}",
                    application.ApplicationNo, ex.Message);
            }
        });

        // Check if both guarantors have now approved — if so, notify admins
        var refreshed = await _repository.GetById(applicationId);
        var bothApproved = refreshed != null && refreshed.CaptainApproved && refreshed.GuarantorApproved;

        // Send emails (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                // Notify applicant
                var applicantInfo = await _repository.GetMemberById(application.ApplicantMemberId);
                if (applicantInfo != null && !string.IsNullOrWhiteSpace(applicantInfo.Email))
                {
                    var subject = $"Qardan Hasana - {guarantorLabel} Approved - {application.ApplicationNo}";
                    var body = BuildEmailHtml(
                        $"Dear {application.ApplicantName},",
                        $@"Your Qardan Hasana application <strong>{application.ApplicationNo}</strong> has been approved by <strong>{guarantorName}</strong> ({guarantorLabel}).
                        <br/><br/>
                        {(bothApproved
                            ? "Both guarantors have now approved your application. It has been forwarded to the Resource Admin for sanctioning."
                            : "Waiting for the other guarantor to approve before the application can proceed.")}");

                    // Notification email disabled per request (only Miqaat creation and attendance emails are sent).
                    // await _emailService.SendEmailAsync(applicantInfo.Email, subject, body);
                }

                // If both approved, notify admins
                if (bothApproved)
                {
                    var admins = await _repository.GetResourceAdmins();
                    foreach (var admin in admins)
                    {
                        if (string.IsNullOrWhiteSpace(admin.Email)) continue;

                        var subject = $"Qardan Hasana - Both Guarantors Approved - {application.ApplicationNo}";
                        var body = BuildEmailHtml(
                            $"Dear {admin.FullName},",
                            $@"Both guarantors have approved the following Qardan Hasana application:
                            <br/><br/>
                            <strong>Application No:</strong> {application.ApplicationNo}<br/>
                            <strong>Applicant:</strong> {application.ApplicantName}<br/>
                            <strong>Amount:</strong> ₹{application.AmountRequested:N2}<br/>
                            <strong>Guarantor 1:</strong> {application.CaptainName} ✓<br/>
                            <strong>Guarantor 2:</strong> {application.GuarantorName} ✓<br/><br/>
                            Please review this application in the BGP Admin Portal for sanctioning.");

                        // Notification email disabled per request (only Miqaat creation and attendance emails are sent).
                        // await _emailService.SendEmailAsync(admin.Email, subject, body);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to send guarantor approval emails for application {Id}: {Error}",
                    applicationId, ex.Message);
            }
        });
    }

    public async Task GuarantorReject(int applicationId, int guarantorMemberId, string? reason)
    {
        var application = await _repository.GetById(applicationId);
        if (application == null)
            throw new Exception("Application not found.");

        if (application.Status == "sanctioned")
            throw new Exception("Cannot reject a sanctioned application.");

        if (application.Status == "rejected")
            throw new Exception("Application is already rejected.");

        // Verify caller is a guarantor
        var isGuarantor1 = application.CaptainMemberId == guarantorMemberId;
        var isGuarantor2 = application.GuarantorMemberId == guarantorMemberId;

        if (!isGuarantor1 && !isGuarantor2)
            throw new Exception("Only an assigned Guarantor can reject this application.");

        var guarantorLabel = isGuarantor1 ? "Guarantor 1" : "Guarantor 2";
        var guarantorName = isGuarantor1 ? application.CaptainName : application.GuarantorName;

        var rejectionText = $"Rejected by {guarantorLabel} ({guarantorName})" +
            (string.IsNullOrWhiteSpace(reason) ? "" : $": {reason}");

        // Mark application as rejected
        await _repository.Reject(applicationId, rejectionText, guarantorMemberId);

        _logger.LogInformation("Qardan Hasana application {Id} ({AppNo}) rejected by {GuarantorLabel} (member {GuarantorId}). Reason: {Reason}",
            applicationId, application.ApplicationNo, guarantorLabel, guarantorMemberId, reason);

        // Activity Log
        _ = Task.Run(async () =>
        {
            try
            {
                await _activityLogService.LogAsync(new ActivityLogModel
                {
                    EntityType = ActivityEntityType.QardanHasana,
                    EntityId = applicationId,
                    Action = ActivityAction.QardanHasanaGuarantorRejected,
                    PerformedBy = guarantorName,
                    PerformedById = guarantorMemberId,
                    PerformedByRole = guarantorLabel,
                    TargetMemberId = application.ApplicantMemberId,
                    TargetMemberName = $"{application.ApplicantName} (ITS: {application.ApplicantItsId})",
                    NewValue = "Rejected",
                    Details = JsonSerializer.Serialize(new { applicationNo = application.ApplicationNo, guarantorRole = guarantorLabel, reason }),
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to log Qardan Hasana guarantor rejection activity for {AppNo}: {Error}",
                    application.ApplicationNo, ex.Message);
            }
        });

        // Notify applicant (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                var applicantInfo = await _repository.GetMemberById(application.ApplicantMemberId);
                if (applicantInfo != null && !string.IsNullOrWhiteSpace(applicantInfo.Email))
                {
                    var subject = $"Qardan Hasana - Application Rejected - {application.ApplicationNo}";
                    var body = BuildEmailHtml(
                        $"Dear {application.ApplicantName},",
                        $@"Your Qardan Hasana application <strong>{application.ApplicationNo}</strong> has been rejected by <strong>{guarantorName}</strong> ({guarantorLabel}).
                        <br/><br/>
                        <strong>Reason:</strong> {(string.IsNullOrWhiteSpace(reason) ? "No reason provided" : reason)}<br/><br/>
                        You may submit a new application if needed.");

                    // Notification email disabled per request (only Miqaat creation and attendance emails are sent).
                    // await _emailService.SendEmailAsync(applicantInfo.Email, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to send guarantor rejection email for application {Id}: {Error}",
                    applicationId, ex.Message);
            }
        });
    }

    public async Task<bool> HasActiveApplication(int memberId)
    {
        return await _repository.HasActiveApplication(memberId);
    }

    /// <summary>
    /// Returns the current date/time in India Standard Time (IST = UTC+5:30)
    /// </summary>
    private static DateTime GetIstNow()
    {
        var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
    }
}
