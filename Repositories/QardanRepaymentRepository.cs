using BurhaniGuards.Api.Contracts.Responses;
using Dapper;

namespace BurhaniGuards.Api.Repositories;

public class QardanRepaymentRepository : IQardanRepaymentRepository
{
    private readonly DapperContext _context;

    public QardanRepaymentRepository(DapperContext context)
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
        int qardanHasanaId, int? installmentNumber, decimal amountPaid,
        DateTime paymentDate, string? paymentMode, string? receiptImageUrl,
        string? notes, int? recordedBy, string? recordedByName)
    {
        var sql = @"
            INSERT INTO qardan_repayments (
                qardan_hasana_id, installment_number, amount_paid,
                payment_date, payment_mode, receipt_image_url,
                notes, recorded_by, recorded_by_name,
                created_at, updated_at
            ) VALUES (
                @QardanHasanaId, @InstallmentNumber, @AmountPaid,
                @PaymentDate, @PaymentMode, @ReceiptImageUrl,
                @Notes, @RecordedBy, @RecordedByName,
                @Now, @Now
            );
            SELECT LAST_INSERT_ID();
        ";

        using var connection = _context.CreateConnection();
        var id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            QardanHasanaId = qardanHasanaId,
            InstallmentNumber = installmentNumber,
            AmountPaid = amountPaid,
            PaymentDate = paymentDate,
            PaymentMode = paymentMode,
            ReceiptImageUrl = receiptImageUrl,
            Notes = notes,
            RecordedBy = recordedBy,
            RecordedByName = recordedByName,
            Now = GetIstNow()
        });

        return id;
    }

    public async Task<List<QardanRepaymentResponse>> GetByQardanId(int qardanHasanaId)
    {
        var sql = @"
            SELECT
                id AS Id,
                qardan_hasana_id AS QardanHasanaId,
                installment_number AS InstallmentNumber,
                amount_paid AS AmountPaid,
                payment_date AS PaymentDate,
                payment_mode AS PaymentMode,
                receipt_image_url AS ReceiptImageUrl,
                notes AS Notes,
                recorded_by AS RecordedBy,
                recorded_by_name AS RecordedByName,
                created_at AS CreatedAt
            FROM qardan_repayments
            WHERE qardan_hasana_id = @QardanHasanaId
            ORDER BY payment_date ASC, id ASC
        ";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync<QardanRepaymentResponse>(sql, new { QardanHasanaId = qardanHasanaId });
        return result.ToList();
    }

    public async Task<QardanRepaymentResponse?> GetById(int id)
    {
        var sql = @"
            SELECT
                id AS Id,
                qardan_hasana_id AS QardanHasanaId,
                installment_number AS InstallmentNumber,
                amount_paid AS AmountPaid,
                payment_date AS PaymentDate,
                payment_mode AS PaymentMode,
                receipt_image_url AS ReceiptImageUrl,
                notes AS Notes,
                recorded_by AS RecordedBy,
                recorded_by_name AS RecordedByName,
                created_at AS CreatedAt
            FROM qardan_repayments
            WHERE id = @Id
        ";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<QardanRepaymentResponse>(sql, new { Id = id });
    }

    public async Task Delete(int id)
    {
        var sql = @"DELETE FROM qardan_repayments WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
