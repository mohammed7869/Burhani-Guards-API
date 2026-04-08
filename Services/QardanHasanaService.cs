using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Repositories;
using BurhaniGuards.Api.ViewModel;

namespace BurhaniGuards.Api.Services;

public class QardanHasanaService : IQardanHasanaService
{
    private readonly IQardanHasanaRepository _repository;
    private readonly IDapperMemberRepository _memberRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<QardanHasanaService> _logger;

    public QardanHasanaService(
        IQardanHasanaRepository repository,
        IDapperMemberRepository memberRepository,
        IEmailService emailService,
        ILogger<QardanHasanaService> logger)
    {
        _repository = repository;
        _memberRepository = memberRepository;
        _emailService = emailService;
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

        if (request.GuarantorMemberId <= 0)
            throw new Exception("Guarantor member is required.");

        // Get Captain of the applicant's jamaat
        var captain = await _repository.GetCaptainByJamaat(currentUser.jamaat ?? "");
        if (captain == null)
            throw new Exception("No Captain found for your Mohallah. Please contact admin.");

        // Get guarantor member details
        var guarantorMember = await _memberRepository.SelectMember(request.GuarantorMemberId);
        if (guarantorMember == null)
            throw new Exception("Selected guarantor member not found.");

        // Generate application number: QH-YYYYMMDD-XXXX
        var count = await _repository.GetNextApplicationCount();
        var applicationNo = $"QH-{DateTime.UtcNow:yyyyMMdd}-{count:D4}";

        // Create application
        var id = await _repository.Create(
            applicationNo: applicationNo,
            applicantMemberId: currentUser.id,
            applicantItsId: currentUser.itsId ?? "",
            applicantName: request.ApplicantName,
            applicantJamaat: currentUser.jamaat ?? "",
            applicantJamaatId: null,
            applicantOccupation: request.ApplicantOccupation,
            applicantMobile: request.ApplicantMobile,
            reason: request.Reason,
            amountRequested: request.AmountRequested,
            applicantSignatureUrl: null,
            applicantPhotoUrl: null,
            termsAccepted: request.TermsAccepted,
            captainMemberId: captain.Id,
            captainName: captain.FullName,
            captainMobile: captain.Contact,
            guarantorMemberId: request.GuarantorMemberId,
            guarantorName: guarantorMember.FullName,
            guarantorMobile: guarantorMember.Contact
        );

        _logger.LogInformation("Qardan Hasana application {ApplicationNo} created by {ApplicantName} (ID: {MemberId})",
            applicationNo, request.ApplicantName, currentUser.id);

        // Send emails asynchronously (fire-and-forget, don't block the response)
        _ = Task.Run(async () =>
        {
            try
            {
                await SendApplicationEmails(applicationNo, request.ApplicantName, request.AmountRequested,
                    request.Reason, currentUser.email, captain.FullName, guarantorMember.FullName, guarantorMember.Email);
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

        if (string.IsNullOrWhiteSpace(adminFormImageUrl))
            throw new Exception("Form image is required for sanctioning.");

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
        string captainName, string guarantorName, string guarantorEmail)
    {
        var subject = $"Qardan Hasana Application - {applicationNo}";

        // Email to applicant
        var applicantBody = BuildEmailHtml(
            $"Dear {applicantName},",
            $@"Your Qardan Hasana application <strong>{applicationNo}</strong> has been submitted successfully.
            <br/><br/>
            <strong>Amount Requested:</strong> ₹{amount:N2}<br/>
            <strong>Reason:</strong> {reason ?? "N/A"}<br/><br/>
            Please download the PDF form from the app and collect physical signatures from your Captain ({captainName}) 
            and Guarantor Member ({guarantorName}).<br/><br/>
            Once all signatures are collected, submit the physical form to the Resource Admin for processing.");

        await _emailService.SendEmailAsync(applicantEmail, subject, applicantBody);

        // Email to Captain - use the captain's email if available
        // Captain's email is not directly available from JamaatMemberResponse, 
        // so the email will be sent in a future iteration if needed

        // Email to Guarantor Member
        var guarantorBody = BuildEmailHtml(
            $"Dear {guarantorName},",
            $@"You have been selected as a Guarantor for a Qardan Hasana application.
            <br/><br/>
            <strong>Application No:</strong> {applicationNo}<br/>
            <strong>Applicant:</strong> {applicantName}<br/>
            <strong>Amount:</strong> ₹{amount:N2}<br/>
            <strong>Reason:</strong> {reason ?? "N/A"}<br/><br/>
            The applicant will approach you for your signature on the physical form. 
            By signing, you accept joint responsibility for repayment in case of default as per BGP Qardan Hasana terms.");

        if (!string.IsNullOrWhiteSpace(guarantorEmail))
        {
            await _emailService.SendEmailAsync(guarantorEmail, subject, guarantorBody);
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
                    <td colspan='2' style='background: #e8f5e9; font-weight: bold;'>1. Captain (Guarantor 1)</td>
                </tr>
                <tr><td class='label'>Captain Name</td><td>{app.CaptainName}</td></tr>
                <tr><td class='label'>Mobile No (Captain)</td><td>{app.CaptainMobile ?? ""}</td></tr>
                <tr><td class='label'>Signature (Captain)</td><td style='height: 60px;'></td></tr>
                <tr>
                    <td colspan='2' style='background: #e8f5e9; font-weight: bold;'>2. Member (Guarantor 2)</td>
                </tr>
                <tr><td class='label'>Member Name</td><td>{app.GuarantorName}</td></tr>
                <tr><td class='label'>Mobile No (Member)</td><td>{app.GuarantorMobile ?? ""}</td></tr>
                <tr><td class='label'>Signature (Member)</td><td style='height: 60px;'></td></tr>
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
                    <li><strong>Guarantors:</strong> Two guarantors mandatory. Guarantor 1: Mohallah Captain. Guarantor 2: Active BGP member from same Mohallah. Must be financially responsible with no outstanding Qardan dues. Accept joint responsibility for repayment.</li>
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
}
