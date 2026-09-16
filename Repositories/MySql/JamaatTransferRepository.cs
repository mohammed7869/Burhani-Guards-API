using BurhaniGuards.Api.Domain;
using BurhaniGuards.Api.Persistence.MySql;
using BurhaniGuards.Api.Repositories.Interfaces;
using Dapper;

namespace BurhaniGuards.Api.Repositories.MySql;

public class JamaatTransferRepository : IJamaatTransferRepository
{
    private readonly MySqlContext _context;

    public JamaatTransferRepository(MySqlContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(JamaatTransfer transfer)
    {
        using var connection = _context.CreateConnection();
        var sql = """
            INSERT INTO jamaat_transfers (
                member_id, member_its_id, from_jamaat_id, from_jamaat,
                to_jamaat_id, to_jamaat, jamiyat_id, jamiyat,
                status, initiated_by_id, initiated_at
            ) VALUES (
                @MemberId, @MemberItsId, @FromJamaatId, @FromJamaat,
                @ToJamaatId, @ToJamaat, @JamiyatId, @Jamiyat,
                @Status, @InitiatedById, @InitiatedAt
            );
            SELECT LAST_INSERT_ID();
            """;
            
        return await connection.ExecuteScalarAsync<int>(sql, transfer);
    }

    public async Task<JamaatTransfer?> GetByIdAsync(int id)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT 
                jt.id AS Id, jt.member_id AS MemberId, jt.member_its_id AS MemberItsId,
                m.full_name AS MemberName, m.contact AS MemberContact, m.profile AS MemberProfile,
                jt.from_jamaat_id AS FromJamaatId, jt.from_jamaat AS FromJamaat,
                jt.to_jamaat_id AS ToJamaatId, jt.to_jamaat AS ToJamaat,
                jt.jamiyat_id AS JamiyatId, jt.jamiyat AS Jamiyat, jt.status AS Status,
                jt.initiated_by_id AS InitiatedById, jt.initiated_at AS InitiatedAt,
                jt.accepted_by_id AS AcceptedById, jt.accepted_at AS AcceptedAt,
                jt.approved_by_id AS ApprovedById, jt.approved_at AS ApprovedAt,
                jt.rejected_by_id AS RejectedById, jt.rejected_at AS RejectedAt,
                jt.rejection_reason AS RejectionReason
            FROM jamaat_transfers jt
            LEFT JOIN members m ON jt.member_id = m.id
            WHERE jt.id = @Id";
        return await connection.QueryFirstOrDefaultAsync<JamaatTransfer>(sql, new { Id = id });
    }

    public async Task<IEnumerable<JamaatTransfer>> GetByMemberIdAsync(int memberId)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT 
                jt.id AS Id, jt.member_id AS MemberId, jt.member_its_id AS MemberItsId,
                m.full_name AS MemberName, m.contact AS MemberContact, m.profile AS MemberProfile,
                jt.from_jamaat_id AS FromJamaatId, jt.from_jamaat AS FromJamaat,
                jt.to_jamaat_id AS ToJamaatId, jt.to_jamaat AS ToJamaat,
                jt.jamiyat_id AS JamiyatId, jt.jamiyat AS Jamiyat, jt.status AS Status,
                jt.initiated_by_id AS InitiatedById, jt.initiated_at AS InitiatedAt,
                jt.accepted_by_id AS AcceptedById, jt.accepted_at AS AcceptedAt,
                jt.approved_by_id AS ApprovedById, jt.approved_at AS ApprovedAt,
                jt.rejected_by_id AS RejectedById, jt.rejected_at AS RejectedAt,
                jt.rejection_reason AS RejectionReason
            FROM jamaat_transfers jt
            LEFT JOIN members m ON jt.member_id = m.id
            WHERE jt.member_id = @MemberId ORDER BY jt.initiated_at DESC";
        return await connection.QueryAsync<JamaatTransfer>(sql, new { MemberId = memberId });
    }

    public async Task<IEnumerable<JamaatTransfer>> GetPendingByFromJamaatIdAsync(int fromJamaatId)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT 
                jt.id AS Id, jt.member_id AS MemberId, jt.member_its_id AS MemberItsId,
                m.full_name AS MemberName, m.contact AS MemberContact, m.profile AS MemberProfile,
                jt.from_jamaat_id AS FromJamaatId, jt.from_jamaat AS FromJamaat,
                jt.to_jamaat_id AS ToJamaatId, jt.to_jamaat AS ToJamaat,
                jt.jamiyat_id AS JamiyatId, jt.jamiyat AS Jamiyat, jt.status AS Status,
                jt.initiated_by_id AS InitiatedById, jt.initiated_at AS InitiatedAt,
                jt.accepted_by_id AS AcceptedById, jt.accepted_at AS AcceptedAt,
                jt.approved_by_id AS ApprovedById, jt.approved_at AS ApprovedAt,
                jt.rejected_by_id AS RejectedById, jt.rejected_at AS RejectedAt,
                jt.rejection_reason AS RejectionReason
            FROM jamaat_transfers jt
            LEFT JOIN members m ON jt.member_id = m.id
            WHERE jt.from_jamaat_id = @FromJamaatId AND jt.status IN ('PENDING', 'ACCEPTED') ORDER BY jt.initiated_at DESC";
        return await connection.QueryAsync<JamaatTransfer>(sql, new { FromJamaatId = fromJamaatId });
    }

    public async Task<IEnumerable<JamaatTransfer>> GetPendingByToJamaatIdAsync(int toJamaatId)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT 
                jt.id AS Id, jt.member_id AS MemberId, jt.member_its_id AS MemberItsId,
                m.full_name AS MemberName, m.contact AS MemberContact, m.profile AS MemberProfile,
                jt.from_jamaat_id AS FromJamaatId, jt.from_jamaat AS FromJamaat,
                jt.to_jamaat_id AS ToJamaatId, jt.to_jamaat AS ToJamaat,
                jt.jamiyat_id AS JamiyatId, jt.jamiyat AS Jamiyat, jt.status AS Status,
                jt.initiated_by_id AS InitiatedById, jt.initiated_at AS InitiatedAt,
                jt.accepted_by_id AS AcceptedById, jt.accepted_at AS AcceptedAt,
                jt.approved_by_id AS ApprovedById, jt.approved_at AS ApprovedAt,
                jt.rejected_by_id AS RejectedById, jt.rejected_at AS RejectedAt,
                jt.rejection_reason AS RejectionReason
            FROM jamaat_transfers jt
            LEFT JOIN members m ON jt.member_id = m.id
            WHERE jt.to_jamaat_id = @ToJamaatId AND jt.status = 'PENDING' ORDER BY jt.initiated_at DESC";
        return await connection.QueryAsync<JamaatTransfer>(sql, new { ToJamaatId = toJamaatId });
    }

    public async Task<IEnumerable<JamaatTransfer>> GetAllByStatusAsync(string status)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT 
                jt.id AS Id, jt.member_id AS MemberId, jt.member_its_id AS MemberItsId,
                m.full_name AS MemberName, m.contact AS MemberContact, m.profile AS MemberProfile,
                jt.from_jamaat_id AS FromJamaatId, jt.from_jamaat AS FromJamaat,
                jt.to_jamaat_id AS ToJamaatId, jt.to_jamaat AS ToJamaat,
                jt.jamiyat_id AS JamiyatId, jt.jamiyat AS Jamiyat, jt.status AS Status,
                jt.initiated_by_id AS InitiatedById, jt.initiated_at AS InitiatedAt,
                jt.accepted_by_id AS AcceptedById, jt.accepted_at AS AcceptedAt,
                jt.approved_by_id AS ApprovedById, jt.approved_at AS ApprovedAt,
                jt.rejected_by_id AS RejectedById, jt.rejected_at AS RejectedAt,
                jt.rejection_reason AS RejectionReason
            FROM jamaat_transfers jt
            LEFT JOIN members m ON jt.member_id = m.id
            WHERE jt.status = @Status ORDER BY jt.initiated_at DESC";
        return await connection.QueryAsync<JamaatTransfer>(sql, new { Status = status });
    }

    public async Task<IEnumerable<JamaatTransfer>> GetHistoryByJamaatIdAsync(int jamaatId)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT 
                jt.id AS Id, jt.member_id AS MemberId, jt.member_its_id AS MemberItsId,
                m.full_name AS MemberName, m.contact AS MemberContact, m.profile AS MemberProfile,
                jt.from_jamaat_id AS FromJamaatId, jt.from_jamaat AS FromJamaat,
                jt.to_jamaat_id AS ToJamaatId, jt.to_jamaat AS ToJamaat,
                jt.jamiyat_id AS JamiyatId, jt.jamiyat AS Jamiyat, jt.status AS Status,
                jt.initiated_by_id AS InitiatedById, jt.initiated_at AS InitiatedAt,
                jt.accepted_by_id AS AcceptedById, jt.accepted_at AS AcceptedAt,
                jt.approved_by_id AS ApprovedById, jt.approved_at AS ApprovedAt,
                jt.rejected_by_id AS RejectedById, jt.rejected_at AS RejectedAt,
                jt.rejection_reason AS RejectionReason
            FROM jamaat_transfers jt
            LEFT JOIN members m ON jt.member_id = m.id
            WHERE (jt.from_jamaat_id = @JamaatId OR jt.to_jamaat_id = @JamaatId) AND jt.status IN ('APPROVED', 'REJECTED') ORDER BY jt.initiated_at DESC";
        return await connection.QueryAsync<JamaatTransfer>(sql, new { JamaatId = jamaatId });
    }

    public async Task<IEnumerable<JamaatTransfer>> GetHistoryAsync()
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT 
                jt.id AS Id, jt.member_id AS MemberId, jt.member_its_id AS MemberItsId,
                m.full_name AS MemberName, m.contact AS MemberContact, m.profile AS MemberProfile,
                jt.from_jamaat_id AS FromJamaatId, jt.from_jamaat AS FromJamaat,
                jt.to_jamaat_id AS ToJamaatId, jt.to_jamaat AS ToJamaat,
                jt.jamiyat_id AS JamiyatId, jt.jamiyat AS Jamiyat, jt.status AS Status,
                jt.initiated_by_id AS InitiatedById, jt.initiated_at AS InitiatedAt,
                jt.accepted_by_id AS AcceptedById, jt.accepted_at AS AcceptedAt,
                jt.approved_by_id AS ApprovedById, jt.approved_at AS ApprovedAt,
                jt.rejected_by_id AS RejectedById, jt.rejected_at AS RejectedAt,
                jt.rejection_reason AS RejectionReason
            FROM jamaat_transfers jt
            LEFT JOIN members m ON jt.member_id = m.id
            WHERE jt.status IN ('APPROVED', 'REJECTED') ORDER BY jt.initiated_at DESC";
        return await connection.QueryAsync<JamaatTransfer>(sql);
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, int actionById, DateTime actionAt, string? rejectionReason = null)
    {
        using var connection = _context.CreateConnection();
        string sql = "";

        if (status == "ACCEPTED")
        {
            sql = @"UPDATE jamaat_transfers SET status = @Status, accepted_by_id = @ActionById, accepted_at = @ActionAt WHERE id = @Id";
        }
        else if (status == "APPROVED")
        {
            sql = @"UPDATE jamaat_transfers SET status = @Status, approved_by_id = @ActionById, approved_at = @ActionAt WHERE id = @Id";
        }
        else if (status == "REJECTED")
        {
            sql = @"UPDATE jamaat_transfers SET status = @Status, rejected_by_id = @ActionById, rejected_at = @ActionAt, rejection_reason = @RejectionReason WHERE id = @Id";
        }
        
        if (string.IsNullOrEmpty(sql)) return false;

        var rows = await connection.ExecuteAsync(sql, new { 
            Id = id, 
            Status = status, 
            ActionById = actionById, 
            ActionAt = actionAt,
            RejectionReason = rejectionReason
        });
        
        return rows > 0;
    }
}
