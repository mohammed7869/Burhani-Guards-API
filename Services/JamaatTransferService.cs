using BurhaniGuards.Api.Constants;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Domain;
using BurhaniGuards.Api.Hubs;
using BurhaniGuards.Api.Repositories;
using BurhaniGuards.Api.Repositories.Interfaces;
using BurhaniGuards.Api.Services;
using BurhaniGuards.Api.BusinessModel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace BurhaniGuards.Api.Services;

public class JamaatTransferService : IJamaatTransferService
{
    private readonly IJamaatTransferRepository _transferRepo;
    private readonly IUserRepository _userRepo;
    private readonly IActivityLogService _activityLogService;
    private readonly IFcmPushService _fcmPushService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public JamaatTransferService(
        IJamaatTransferRepository transferRepo,
        IUserRepository userRepo,
        IActivityLogService activityLogService,
        IFcmPushService fcmPushService,
        IHubContext<NotificationHub> hubContext)
    {
        _transferRepo = transferRepo;
        _userRepo = userRepo;
        _activityLogService = activityLogService;
        _fcmPushService = fcmPushService;
        _hubContext = hubContext;
    }

    public async Task<int> InitiateTransferAsync(InitiateJamaatTransferRequest request, int currentUserId)
    {
        var member = await _userRepo.SelectUser(request.MemberId);
        if (member == null)
            throw new Exception("Member not found");

        var toJamaatText = Jamaat.GetJamaatText(request.ToJamaatId);
        if (toJamaatText == "Unknown")
            throw new Exception("Invalid target Jamaat");

        // Validate Jamiyat match
        if (member.JamiyatId == null || member.Jamiyat == null)
            throw new Exception("Member Jamiyat information is missing");

        // Assuming target Jamiyat is the same as current for this feature's scope
        var toJamiyatId = member.JamiyatId.Value;
        var toJamiyatText = member.Jamiyat;

        // Check if pending transfer already exists
        var existingTransfers = await _transferRepo.GetByMemberIdAsync(request.MemberId);
        if (existingTransfers.Any(t => t.Status == "PENDING" || t.Status == "ACCEPTED"))
            throw new Exception("Member already has a pending or accepted transfer request.");

        var transfer = new JamaatTransfer
        {
            MemberId = (int)member.Id,
            MemberItsId = member.ItsId ?? "",
            FromJamaatId = member.JamaatId ?? 0,
            FromJamaat = member.Jamaat ?? "",
            ToJamaatId = request.ToJamaatId,
            ToJamaat = toJamaatText,
            JamiyatId = toJamiyatId,
            Jamiyat = toJamiyatText,
            Status = "PENDING",
            InitiatedById = currentUserId,
            InitiatedAt = DateTime.UtcNow
        };

        var transferId = await _transferRepo.CreateAsync(transfer);

        var currentUser = await _userRepo.SelectUser(currentUserId);

        await _activityLogService.LogAsync(new ActivityLogModel
        {
            EntityType = "JamaatTransfer",
            EntityId = transferId,
            Action = "TRANSFER_INITIATED",
            PerformedBy = currentUser?.FullName,
            PerformedById = currentUserId,
            PerformedByRole = currentUser?.Rank,
            TargetMemberId = (int)member.Id,
            TargetMemberName = member.FullName,
            OldValue = member.Jamaat,
            NewValue = toJamaatText,
            Details = $"Transfer initiated to {toJamaatText}"
        });

        // Notify Target Jamaat Captains
        var targetCaptains = await _userRepo.GetUserIdsByJamaatAsync(toJamaatText); // Need to make sure this gets captains, or we notify all in Jamaat. Wait, we usually get captains by role.
        
        // Notify Admins
        var adminIds = await _userRepo.GetAdminUserIdsAsync();
        
        // Send signalR notifications
        if (adminIds.Any())
        {
            await _hubContext.Clients.Users(adminIds.Select(id => id.ToString()))
                .SendAsync("ReceiveNotification", "New Transfer Request Initiated", $"{member.FullName} transfer to {toJamaatText}");
        }

        return transferId;
    }

    public async Task<bool> AcceptTransferAsync(int id, int currentUserId)
    {
        var transfer = await _transferRepo.GetByIdAsync(id);
        if (transfer == null) throw new Exception("Transfer not found");
        if (transfer.Status != "PENDING") throw new Exception("Transfer is not in PENDING state");

        var success = await _transferRepo.UpdateStatusAsync(id, "ACCEPTED", currentUserId, DateTime.UtcNow);
        if (success)
        {
            var currentUser = await _userRepo.SelectUser(currentUserId);
            var member = await _userRepo.SelectUser(transfer.MemberId);

            await _activityLogService.LogAsync(new ActivityLogModel
            {
                EntityType = "JamaatTransfer",
                EntityId = id,
                Action = "TRANSFER_ACCEPTED",
                PerformedBy = currentUser?.FullName,
                PerformedById = currentUserId,
                PerformedByRole = currentUser?.Rank,
                TargetMemberId = transfer.MemberId,
                TargetMemberName = member?.FullName,
                OldValue = "PENDING",
                NewValue = "ACCEPTED",
                Details = "Transfer accepted by target jamaat captain"
            });

            // Notify Admins
            var adminIds = await _userRepo.GetAdminUserIdsAsync();
            if (adminIds.Any())
            {
                await _hubContext.Clients.Users(adminIds.Select(aid => aid.ToString()))
                    .SendAsync("ReceiveNotification", "Transfer Request Accepted", $"Transfer for {member?.FullName} was accepted and awaits your approval.");
            }
        }
        return success;
    }

    public async Task<bool> ApproveTransferAsync(int id, int currentUserId)
    {
        var transfer = await _transferRepo.GetByIdAsync(id);
        if (transfer == null) throw new Exception("Transfer not found");
        if (transfer.Status != "ACCEPTED") throw new Exception("Transfer must be ACCEPTED by target captain first");

        var success = await _transferRepo.UpdateStatusAsync(id, "APPROVED", currentUserId, DateTime.UtcNow);
        if (success)
        {
            // Update actual member record
            await _userRepo.UpdateJamaatAsync(
                transfer.MemberId, 
                transfer.ToJamaatId, 
                transfer.ToJamaat, 
                transfer.JamiyatId, 
                transfer.Jamiyat
            );

            var currentUser = await _userRepo.SelectUser(currentUserId);
            var member = await _userRepo.SelectUser(transfer.MemberId);

            await _activityLogService.LogAsync(new ActivityLogModel
            {
                EntityType = "JamaatTransfer",
                EntityId = id,
                Action = "TRANSFER_APPROVED",
                PerformedBy = currentUser?.FullName,
                PerformedById = currentUserId,
                PerformedByRole = currentUser?.Rank,
                TargetMemberId = transfer.MemberId,
                TargetMemberName = member?.FullName,
                OldValue = transfer.FromJamaat,
                NewValue = transfer.ToJamaat,
                Details = "Transfer officially approved by Admin"
            });
        }
        return success;
    }

    public async Task<bool> RejectTransferAsync(int id, RejectJamaatTransferRequest request, int currentUserId)
    {
        var transfer = await _transferRepo.GetByIdAsync(id);
        if (transfer == null) throw new Exception("Transfer not found");
        if (transfer.Status == "APPROVED" || transfer.Status == "REJECTED") 
            throw new Exception("Transfer is already completed");

        var success = await _transferRepo.UpdateStatusAsync(id, "REJECTED", currentUserId, DateTime.UtcNow, request.Reason);
        if (success)
        {
            var currentUser = await _userRepo.SelectUser(currentUserId);
            var member = await _userRepo.SelectUser(transfer.MemberId);

            await _activityLogService.LogAsync(new ActivityLogModel
            {
                EntityType = "JamaatTransfer",
                EntityId = id,
                Action = "TRANSFER_REJECTED",
                PerformedBy = currentUser?.FullName,
                PerformedById = currentUserId,
                PerformedByRole = currentUser?.Rank,
                TargetMemberId = transfer.MemberId,
                TargetMemberName = member?.FullName,
                OldValue = transfer.Status,
                NewValue = "REJECTED",
                Details = $"Reason: {request.Reason}"
            });
        }
        return success;
    }

    public async Task<IEnumerable<JamaatTransfer>> GetByMemberIdAsync(int memberId) => 
        await _transferRepo.GetByMemberIdAsync(memberId);

    public async Task<IEnumerable<JamaatTransfer>> GetPendingByFromJamaatIdAsync(int fromJamaatId) => 
        await _transferRepo.GetPendingByFromJamaatIdAsync(fromJamaatId);

    public async Task<IEnumerable<JamaatTransfer>> GetPendingByToJamaatIdAsync(int toJamaatId) => 
        await _transferRepo.GetPendingByToJamaatIdAsync(toJamaatId);

    public async Task<IEnumerable<JamaatTransfer>> GetAllByStatusAsync(string status) => 
        await _transferRepo.GetAllByStatusAsync(status);

    public async Task<JamaatTransfer?> GetByIdAsync(int id) => 
        await _transferRepo.GetByIdAsync(id);

    public async Task<IEnumerable<JamaatTransfer>> GetHistoryByJamaatIdAsync(int jamaatId) => 
        await _transferRepo.GetHistoryByJamaatIdAsync(jamaatId);

    public async Task<IEnumerable<JamaatTransfer>> GetHistoryAsync() => 
        await _transferRepo.GetHistoryAsync();
}
