namespace BurhaniGuards.Api.Domain;

/// <summary>
/// Domain model for Jamaat Transfer request
/// </summary>
public sealed class JamaatTransfer
{
    public int Id { get; init; }
    public int MemberId { get; init; }
    public string MemberItsId { get; init; } = string.Empty;
    public string MemberName { get; init; } = string.Empty;
    public string? MemberContact { get; init; }
    public string? MemberProfile { get; init; }
    public int FromJamaatId { get; init; }
    public string FromJamaat { get; init; } = string.Empty;
    public int ToJamaatId { get; init; }
    public string ToJamaat { get; init; } = string.Empty;
    public int JamiyatId { get; init; }
    public string Jamiyat { get; init; } = string.Empty;
    
    // Status can be: PENDING, ACCEPTED, APPROVED, REJECTED
    public string Status { get; init; } = "PENDING";
    
    public int InitiatedById { get; init; }
    public DateTime InitiatedAt { get; init; }
    
    public int? AcceptedById { get; init; }
    public DateTime? AcceptedAt { get; init; }
    
    public int? ApprovedById { get; init; }
    public DateTime? ApprovedAt { get; init; }
    
    public int? RejectedById { get; init; }
    public DateTime? RejectedAt { get; init; }
    
    public string? RejectionReason { get; init; }
}
