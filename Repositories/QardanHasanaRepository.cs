using BurhaniGuards.Api.Contracts.Responses;
using Dapper;

namespace BurhaniGuards.Api.Repositories;

public class QardanHasanaRepository : IQardanHasanaRepository
{
    private readonly DapperContext _context;

    public QardanHasanaRepository(DapperContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the current date/time in India Standard Time (IST = UTC+5:30)
    /// </summary>
    private static DateTime GetIstNow()
    {
        var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
    }

    public async Task<int> Create(
        string applicationNo, int applicantMemberId, string applicantItsId,
        string applicantName, string applicantJamaat, int? applicantJamaatId,
        string? applicantOccupation, string applicantMobile,
        string? reason, decimal amountRequested,
        string? applicantSignatureUrl, string? applicantPhotoUrl,
        bool termsAccepted,
        int captainMemberId, string captainName, string? captainMobile,
        int guarantorMemberId, string guarantorName, string? guarantorMobile)
    {
        var sql = @"
            INSERT INTO qardan_hasana (
                application_no, applicant_member_id, applicant_its_id,
                applicant_name, applicant_jamaat, applicant_jamaat_id,
                applicant_occupation, applicant_mobile,
                reason, amount_requested,
                applicant_signature_url, applicant_photo_url,
                terms_accepted,
                captain_member_id, captain_name, captain_mobile,
                guarantor_member_id, guarantor_name, guarantor_mobile,
                status, created_at, updated_at
            ) VALUES (
                @ApplicationNo, @ApplicantMemberId, @ApplicantItsId,
                @ApplicantName, @ApplicantJamaat, @ApplicantJamaatId,
                @ApplicantOccupation, @ApplicantMobile,
                @Reason, @AmountRequested,
                @ApplicantSignatureUrl, @ApplicantPhotoUrl,
                @TermsAccepted,
                @CaptainMemberId, @CaptainName, @CaptainMobile,
                @GuarantorMemberId, @GuarantorName, @GuarantorMobile,
                'pending', @Now, @Now
            );
            SELECT LAST_INSERT_ID();
        ";

        using var connection = _context.CreateConnection();
        var id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            ApplicationNo = applicationNo,
            ApplicantMemberId = applicantMemberId,
            ApplicantItsId = applicantItsId,
            ApplicantName = applicantName,
            ApplicantJamaat = applicantJamaat,
            ApplicantJamaatId = applicantJamaatId,
            ApplicantOccupation = applicantOccupation,
            ApplicantMobile = applicantMobile,
            Reason = reason,
            AmountRequested = amountRequested,
            ApplicantSignatureUrl = applicantSignatureUrl,
            ApplicantPhotoUrl = applicantPhotoUrl,
            TermsAccepted = termsAccepted,
            CaptainMemberId = captainMemberId,
            CaptainName = captainName,
            CaptainMobile = captainMobile,
            GuarantorMemberId = guarantorMemberId,
            GuarantorName = guarantorName,
            GuarantorMobile = guarantorMobile,
            Now = GetIstNow()
        });

        return id;
    }

    public async Task<QardanHasanaResponse?> GetById(int id)
    {
        var sql = @"
            SELECT 
                q.id AS Id,
                q.application_no AS ApplicationNo,
                q.applicant_member_id AS ApplicantMemberId,
                q.applicant_its_id AS ApplicantItsId,
                q.applicant_name AS ApplicantName,
                am.full_name AS ApplicantMemberName,
                q.applicant_jamaat AS ApplicantJamaat,
                q.applicant_occupation AS ApplicantOccupation,
                q.applicant_mobile AS ApplicantMobile,
                q.reason AS Reason,
                q.amount_requested AS AmountRequested,
                q.applicant_signature_url AS ApplicantSignatureUrl,
                q.applicant_photo_url AS ApplicantPhotoUrl,
                am.profile AS ApplicantProfile,
                q.captain_member_id AS CaptainMemberId,
                q.captain_name AS CaptainName,
                q.captain_mobile AS CaptainMobile,
                g1.its_id AS CaptainItsId,
                g1.profile AS CaptainProfile,
                q.captain_approved AS CaptainApproved,
                q.captain_approved_at AS CaptainApprovedAt,
                q.guarantor_member_id AS GuarantorMemberId,
                q.guarantor_name AS GuarantorName,
                q.guarantor_mobile AS GuarantorMobile,
                g2.its_id AS GuarantorItsId,
                g2.profile AS GuarantorProfile,
                COALESCE(q.guarantor_approved, 0) AS GuarantorApproved,
                q.guarantor_approved_at AS GuarantorApprovedAt,
                q.status AS Status,
                q.form_image_url AS FormImageUrl,
                q.sanctioned_amount AS SanctionedAmount,
                q.installment_amount AS InstallmentAmount,
                q.number_of_months AS NumberOfMonths,
                q.installment_date_from AS InstallmentDateFrom,
                q.installment_date_to AS InstallmentDateTo,
                q.admin_signature_url AS AdminSignatureUrl,
                q.admin_form_image_url AS AdminFormImageUrl,
                q.admin_approved_by AS AdminApprovedBy,
                q.admin_approved_at AS AdminApprovedAt,
                q.rejection_reason AS RejectionReason,
                q.created_at AS CreatedAt,
                q.updated_at AS UpdatedAt
            FROM qardan_hasana q
            LEFT JOIN members am ON am.id = q.applicant_member_id
            LEFT JOIN members g1 ON g1.id = q.captain_member_id
            LEFT JOIN members g2 ON g2.id = q.guarantor_member_id
            WHERE q.id = @Id
        ";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<QardanHasanaResponse>(sql, new { Id = id });
    }

    public async Task<List<QardanHasanaListResponse>> GetAll(string? statusFilter = null)
    {
        var sql = @"
            SELECT 
                id AS Id,
                application_no AS ApplicationNo,
                applicant_member_id AS ApplicantMemberId,
                applicant_name AS ApplicantName,
                applicant_jamaat AS ApplicantJamaat,
                amount_requested AS AmountRequested,
                sanctioned_amount AS SanctionedAmount,
                status AS Status,
                captain_approved AS CaptainApproved,
                COALESCE(guarantor_approved, 0) AS GuarantorApproved,
                captain_member_id AS CaptainMemberId,
                guarantor_member_id AS GuarantorMemberId,
                created_at AS CreatedAt
            FROM qardan_hasana
        ";

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            sql += " WHERE status = @Status";
        }

        sql += " ORDER BY created_at DESC";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<QardanHasanaListResponse>(sql, new { Status = statusFilter });
        return result.ToList();
    }

    public async Task<List<QardanHasanaListResponse>> GetByApplicantId(int memberId)
    {
        var sql = @"
            SELECT 
                id AS Id,
                application_no AS ApplicationNo,
                applicant_member_id AS ApplicantMemberId,
                applicant_name AS ApplicantName,
                applicant_jamaat AS ApplicantJamaat,
                amount_requested AS AmountRequested,
                sanctioned_amount AS SanctionedAmount,
                status AS Status,
                captain_approved AS CaptainApproved,
                COALESCE(guarantor_approved, 0) AS GuarantorApproved,
                captain_member_id AS CaptainMemberId,
                guarantor_member_id AS GuarantorMemberId,
                created_at AS CreatedAt
            FROM qardan_hasana
            WHERE applicant_member_id = @MemberId
            ORDER BY created_at DESC
        ";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<QardanHasanaListResponse>(sql, new { MemberId = memberId });
        return result.ToList();
    }

    public async Task<List<QardanHasanaListResponse>> GetByJamaat(string jamaat)
    {
        var sql = @"
            SELECT 
                id AS Id,
                application_no AS ApplicationNo,
                applicant_member_id AS ApplicantMemberId,
                applicant_name AS ApplicantName,
                applicant_jamaat AS ApplicantJamaat,
                amount_requested AS AmountRequested,
                sanctioned_amount AS SanctionedAmount,
                status AS Status,
                captain_approved AS CaptainApproved,
                COALESCE(guarantor_approved, 0) AS GuarantorApproved,
                captain_member_id AS CaptainMemberId,
                guarantor_member_id AS GuarantorMemberId,
                created_at AS CreatedAt
            FROM qardan_hasana
            WHERE applicant_jamaat = @Jamaat
            ORDER BY created_at DESC
        ";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<QardanHasanaListResponse>(sql, new { Jamaat = jamaat });
        return result.ToList();
    }

    public async Task<List<QardanHasanaListResponse>> GetByGuarantorId(int memberId)
    {
        var sql = @"
            SELECT 
                id AS Id,
                application_no AS ApplicationNo,
                applicant_member_id AS ApplicantMemberId,
                applicant_name AS ApplicantName,
                applicant_jamaat AS ApplicantJamaat,
                amount_requested AS AmountRequested,
                sanctioned_amount AS SanctionedAmount,
                status AS Status,
                captain_approved AS CaptainApproved,
                COALESCE(guarantor_approved, 0) AS GuarantorApproved,
                captain_member_id AS CaptainMemberId,
                guarantor_member_id AS GuarantorMemberId,
                created_at AS CreatedAt
            FROM qardan_hasana
            WHERE captain_member_id = @MemberId OR guarantor_member_id = @MemberId
            ORDER BY created_at DESC
        ";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<QardanHasanaListResponse>(sql, new { MemberId = memberId });
        return result.ToList();
    }

    public async Task<int> GetNextApplicationCount()
    {
        var sql = @"SELECT COUNT(*) + 1 FROM qardan_hasana";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql);
    }

    public async Task UpdateStatus(int id, string status)
    {
        var sql = @"UPDATE qardan_hasana SET status = @Status, updated_at = @Now WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, Status = status, Now = GetIstNow() });
    }

    public async Task UpdateFormImage(int id, string formImageUrl)
    {
        var sql = @"
            UPDATE qardan_hasana 
            SET form_image_url = @FormImageUrl, 
                status = CASE WHEN status = 'pending' THEN 'submitted_to_admin' ELSE status END,
                updated_at = @Now 
            WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, FormImageUrl = formImageUrl, Now = GetIstNow() });
    }

    public async Task Sanction(int id, decimal sanctionedAmount, decimal installmentAmount,
        int numberOfMonths, DateTime installmentDateFrom, DateTime installmentDateTo,
        string? adminSignatureUrl, string? adminFormImageUrl, int adminApprovedBy)
    {
        var sql = @"
            UPDATE qardan_hasana SET
                sanctioned_amount = @SanctionedAmount,
                installment_amount = @InstallmentAmount,
                number_of_months = @NumberOfMonths,
                installment_date_from = @InstallmentDateFrom,
                installment_date_to = @InstallmentDateTo,
                admin_signature_url = @AdminSignatureUrl,
                admin_form_image_url = @AdminFormImageUrl,
                admin_approved_by = @AdminApprovedBy,
                admin_approved_at = @Now,
                status = 'sanctioned',
                updated_at = @Now
            WHERE id = @Id
        ";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            SanctionedAmount = sanctionedAmount,
            InstallmentAmount = installmentAmount,
            NumberOfMonths = numberOfMonths,
            InstallmentDateFrom = installmentDateFrom,
            InstallmentDateTo = installmentDateTo,
            AdminSignatureUrl = adminSignatureUrl,
            AdminFormImageUrl = adminFormImageUrl,
            AdminApprovedBy = adminApprovedBy,
            Now = GetIstNow()
        });
    }

    public async Task Reject(int id, string? reason, int adminId)
    {
        var sql = @"
            UPDATE qardan_hasana SET
                status = 'rejected',
                rejection_reason = @Reason,
                admin_approved_by = @AdminId,
                admin_approved_at = @Now,
                updated_at = @Now
            WHERE id = @Id
        ";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, Reason = reason, AdminId = adminId, Now = GetIstNow() });
    }

    public async Task<List<JamaatMemberResponse>> GetMembersByJamaat(string jamaat, int excludeMemberId)
    {
        var sql = @"
            SELECT 
                `id` AS Id,
                `full_name` AS FullName,
                `contact` AS Contact,
                `its_id` AS ItsId
            FROM `members`
            WHERE `jamaat` = @Jamaat 
              AND `is_active` = 1
              AND `is_approved` = 1
              AND `id` <> @ExcludeMemberId
            ORDER BY `full_name` ASC
        ";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<JamaatMemberResponse>(sql, new { Jamaat = jamaat, ExcludeMemberId = excludeMemberId });
        return result.ToList();
    }

    public async Task<JamaatMemberResponse?> GetCaptainByJamaat(string jamaat)
    {
        var sql = @"
            SELECT 
                `id` AS Id,
                `full_name` AS FullName,
                `contact` AS Contact,
                `its_id` AS ItsId
            FROM `members`
            WHERE `jamaat` = @Jamaat 
              AND `is_active` = 1
              AND `roles` = 2
            LIMIT 1
        ";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<JamaatMemberResponse>(sql, new { Jamaat = jamaat });
    }

    public async Task<MemberBasicInfo?> GetMemberById(int id)
    {
        var sql = @"
            SELECT 
                `id` AS Id,
                `full_name` AS FullName,
                `contact` AS Contact,
                `email` AS Email,
                `jamaat_id` AS JamaatId
            FROM `members`
            WHERE `id` = @Id
        ";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<MemberBasicInfo>(sql, new { Id = id });
    }

    public async Task CaptainApprove(int id)
    {
        var sql = @"
            UPDATE qardan_hasana SET
                captain_approved = 1,
                captain_approved_at = @Now,
                updated_at = @Now
            WHERE id = @Id
        ";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, Now = GetIstNow() });
    }

    public async Task GuarantorApprove(int id)
    {
        var sql = @"
            UPDATE qardan_hasana SET
                guarantor_approved = 1,
                guarantor_approved_at = @Now,
                updated_at = @Now
            WHERE id = @Id
        ";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, Now = GetIstNow() });
    }

    public async Task<List<MemberBasicInfo>> GetResourceAdmins()
    {
        var sql = @"
            SELECT 
                `id` AS Id,
                `full_name` AS FullName,
                `contact` AS Contact,
                `email` AS Email,
                `jamaat_id` AS JamaatId
            FROM `members`
            WHERE `roles` = 7
              AND `is_active` = 1
        ";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<MemberBasicInfo>(sql);
        return result.ToList();
    }

    public async Task<bool> HasActiveApplication(int memberId)
    {
        var sql = @"
            SELECT COUNT(*) FROM qardan_hasana 
            WHERE applicant_member_id = @MemberId
            AND (
                -- Pending or in-process (not yet sanctioned/rejected)
                status IN ('pending', 'submitted_to_admin')
                OR
                -- Sanctioned with ongoing installments
                (status = 'sanctioned' AND installment_date_to >= CURDATE())
            )
        ";

        using var connection = _context.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { MemberId = memberId });
        return count > 0;
    }

    public async Task UpdateApplication(int id, string applicantName, string? applicantOccupation,
        string applicantMobile, string? reason, decimal amountRequested,
        int captainMemberId, string captainName, string? captainMobile,
        int guarantorMemberId, string guarantorName, string? guarantorMobile)
    {
        var sql = @"
            UPDATE qardan_hasana SET
                applicant_name = @ApplicantName,
                applicant_occupation = @ApplicantOccupation,
                applicant_mobile = @ApplicantMobile,
                reason = @Reason,
                amount_requested = @AmountRequested,
                captain_member_id = @CaptainMemberId,
                captain_name = @CaptainName,
                captain_mobile = @CaptainMobile,
                captain_approved = 0,
                captain_approved_at = NULL,
                guarantor_member_id = @GuarantorMemberId,
                guarantor_name = @GuarantorName,
                guarantor_mobile = @GuarantorMobile,
                guarantor_approved = 0,
                guarantor_approved_at = NULL,
                updated_at = @Now
            WHERE id = @Id
        ";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            ApplicantName = applicantName,
            ApplicantOccupation = applicantOccupation,
            ApplicantMobile = applicantMobile,
            Reason = reason,
            AmountRequested = amountRequested,
            CaptainMemberId = captainMemberId,
            CaptainName = captainName,
            CaptainMobile = captainMobile,
            GuarantorMemberId = guarantorMemberId,
            GuarantorName = guarantorName,
            GuarantorMobile = guarantorMobile,
            Now = GetIstNow()
        });
    }
}
