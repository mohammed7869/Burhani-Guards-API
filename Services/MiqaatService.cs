using BurhaniGuards.Api.BusinessModel;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Repositories;

namespace BurhaniGuards.Api.Services;

public class MiqaatService : IMiqaatService
{
    private readonly IMiqaatRepository _miqaatRepository;
    private readonly IMiqaatMemberRepository _miqaatMemberRepository;
    private static readonly TimeZoneInfo IndiaTimeZone = GetIndiaTimeZone();

    public MiqaatService(IMiqaatRepository miqaatRepository, IMiqaatMemberRepository miqaatMemberRepository)
    {
        _miqaatRepository = miqaatRepository;
        _miqaatMemberRepository = miqaatMemberRepository;
    }

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            // Try Windows timezone ID first
            return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Try Linux/Unix timezone ID
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback: Create a custom timezone for IST (UTC+5:30)
                return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromHours(5.5), "India Standard Time", "India Standard Time");
            }
        }
    }

    private DateTime ConvertUtcToIst(DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            // If kind is unspecified, assume it's UTC
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, IndiaTimeZone);
    }

    public async Task<MiqaatResponse> Create(CreateMiqaatRequest request, string captainName)
    {
        var model = new MiqaatModel
        {
            MiqaatName = request.MiqaatName,
            Jamaat = request.Jamaat,
            Jamiyat = request.Jamiyat,
            FromDate = request.FromDate,
            TillDate = request.TillDate,
            VolunteerLimit = request.VolunteerLimit,
            AboutMiqaat = request.AboutMiqaat,
            AdminApproval = AdminApprovalStatus.Pending,
            CaptainName = captainName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var id = await _miqaatRepository.Add(model);
        var createdMiqaat = await _miqaatRepository.GetById(id);

        if (createdMiqaat == null)
        {
            throw new Exception("Failed to create miqaat");
        }

        return new MiqaatResponse(
            createdMiqaat.Id,
            createdMiqaat.MiqaatName,
            createdMiqaat.Jamaat,
            createdMiqaat.Jamiyat,
            createdMiqaat.FromDate,
            createdMiqaat.TillDate,
            createdMiqaat.VolunteerLimit,
            createdMiqaat.AboutMiqaat,
            createdMiqaat.AdminApproval.ToString(),
            createdMiqaat.CaptainName,
            ConvertUtcToIst(createdMiqaat.CreatedAt),
            ConvertUtcToIst(createdMiqaat.UpdatedAt)
        );
    }

    public async Task<List<MiqaatResponse>> GetAll()
    {
        var miqaats = await _miqaatRepository.GetAll();
        return miqaats.Select(m => new MiqaatResponse(
            m.Id,
            m.MiqaatName,
            m.Jamaat,
            m.Jamiyat,
            m.FromDate,
            m.TillDate,
            m.VolunteerLimit,
            m.AboutMiqaat,
            m.AdminApproval.ToString(),
            m.CaptainName,
            ConvertUtcToIst(m.CreatedAt),
            ConvertUtcToIst(m.UpdatedAt)
        )).ToList();
    }

    public async Task<MiqaatResponse?> GetById(long id)
    {
        var miqaat = await _miqaatRepository.GetById(id);
        if (miqaat == null)
        {
            return null;
        }

        return new MiqaatResponse(
            miqaat.Id,
            miqaat.MiqaatName,
            miqaat.Jamaat,
            miqaat.Jamiyat,
            miqaat.FromDate,
            miqaat.TillDate,
            miqaat.VolunteerLimit,
            miqaat.AboutMiqaat,
            miqaat.AdminApproval.ToString(),
            miqaat.CaptainName,
            ConvertUtcToIst(miqaat.CreatedAt),
            ConvertUtcToIst(miqaat.UpdatedAt)
        );
    }

    public async Task Update(long id, UpdateMiqaatRequest request)
    {
        var existingMiqaat = await _miqaatRepository.GetById(id);
        if (existingMiqaat == null)
        {
            throw new Exception("Miqaat not found");
        }

        var previousApprovalStatus = existingMiqaat.AdminApproval;

        existingMiqaat.MiqaatName = request.MiqaatName;
        existingMiqaat.Jamaat = request.Jamaat;
        existingMiqaat.Jamiyat = request.Jamiyat;
        existingMiqaat.FromDate = request.FromDate;
        existingMiqaat.TillDate = request.TillDate;
        existingMiqaat.VolunteerLimit = request.VolunteerLimit;
        existingMiqaat.AboutMiqaat = request.AboutMiqaat;
        
        // Update approval status if provided
        AdminApprovalStatus? newApprovalStatus = null;
        if (!string.IsNullOrWhiteSpace(request.AdminApproval))
        {
            newApprovalStatus = ParseApprovalStatus(request.AdminApproval);
            existingMiqaat.AdminApproval = newApprovalStatus.Value;
        }
        
        existingMiqaat.UpdatedAt = DateTime.UtcNow;

        await _miqaatRepository.Update(existingMiqaat);

        // Only seed miqaat_members after admin approval is set to Approved
        // UpsertMembersForMiqaat uses ON DUPLICATE KEY UPDATE, so it's safe to call multiple times
        if (newApprovalStatus.HasValue && newApprovalStatus.Value == AdminApprovalStatus.Approved)
        {
            await _miqaatMemberRepository.UpsertMembersForMiqaat(id, existingMiqaat.Jamaat, AdminApprovalStatus.Pending);
        }
    }

    public async Task UpdateApprovalStatus(long id, string status)
    {
        var existingMiqaat = await _miqaatRepository.GetById(id);
        if (existingMiqaat == null)
        {
            throw new Exception("Miqaat not found");
        }

        var previousApprovalStatus = existingMiqaat.AdminApproval;
        var parsedStatus = ParseApprovalStatus(status);
        existingMiqaat.AdminApproval = parsedStatus;
        existingMiqaat.UpdatedAt = DateTime.UtcNow;

        await _miqaatRepository.Update(existingMiqaat);

        // Only seed miqaat_members after admin approval is set to Approved
        // UpsertMembersForMiqaat uses ON DUPLICATE KEY UPDATE, so it's safe to call multiple times
        if (parsedStatus == AdminApprovalStatus.Approved)
        {
            await _miqaatMemberRepository.UpsertMembersForMiqaat(id, existingMiqaat.Jamaat, AdminApprovalStatus.Pending);
        }
    }

    public async Task Delete(long id)
    {
        var existingMiqaat = await _miqaatRepository.GetById(id);
        if (existingMiqaat == null)
        {
            throw new Exception("Miqaat not found");
        }

        await _miqaatRepository.Delete(id);
    }

    public async Task<List<MiqaatResponse>> GetMiqaatsByMemberId(int memberId)
    {
        var miqaats = await _miqaatMemberRepository.GetMiqaatsByMemberId(memberId);
        return miqaats.Select(m => new MiqaatResponse(
            m.Id,
            m.MiqaatName,
            m.Jamaat,
            m.Jamiyat,
            m.FromDate,
            m.TillDate,
            m.VolunteerLimit,
            m.AboutMiqaat,
            m.AdminApproval.ToString(),
            m.CaptainName,
            ConvertUtcToIst(m.CreatedAt),
            ConvertUtcToIst(m.UpdatedAt)
        )).ToList();
    }

    public async Task<List<MiqaatResponse>> GetMiqaatsByCaptainName(string captainName)
    {
        var miqaats = await _miqaatRepository.GetByCaptainName(captainName);
        return miqaats.Select(m => new MiqaatResponse(
            m.Id,
            m.MiqaatName,
            m.Jamaat,
            m.Jamiyat,
            m.FromDate,
            m.TillDate,
            m.VolunteerLimit,
            m.AboutMiqaat,
            m.AdminApproval.ToString(),
            m.CaptainName,
            ConvertUtcToIst(m.CreatedAt),
            ConvertUtcToIst(m.UpdatedAt)
        )).ToList();
    }

    public async Task<List<MiqaatResponse>> GetMiqaatsForCurrentUser(int userId, int? userRole, string? captainName)
    {
        // If user is Captain (role = 2), return all miqaats created by them
        if (userRole == 2 && !string.IsNullOrWhiteSpace(captainName))
        {
            return await GetMiqaatsByCaptainName(captainName);
        }
        // If user is Member (role = 1), return miqaats from miqaat_members table
        else
        {
            return await GetMiqaatsByMemberId(userId);
        }
    }

    public async Task UpdateMemberMiqaatStatus(int memberId, long miqaatId, string status)
    {
        // Validate status
        if (status != "Approved" && status != "Rejected" && status != "Pending")
        {
            throw new Exception("Invalid status. Must be 'Approved', 'Rejected', or 'Pending'");
        }

        await _miqaatMemberRepository.UpdateMemberMiqaatStatus(memberId, miqaatId, status);
    }

    private static AdminApprovalStatus ParseApprovalStatus(string status)
    {
        if (Enum.TryParse<AdminApprovalStatus>(status, true, out var parsed))
        {
            return parsed;
        }

        throw new Exception("Invalid approval status. Must be 'Pending', 'Approved', or 'Rejected'");
    }
}

