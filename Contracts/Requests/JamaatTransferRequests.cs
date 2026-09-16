namespace BurhaniGuards.Api.Contracts.Requests;

public sealed class InitiateJamaatTransferRequest
{
    public int MemberId { get; set; }
    public int ToJamaatId { get; set; }
}

public sealed class RejectJamaatTransferRequest
{
    public string Reason { get; set; } = string.Empty;
}
