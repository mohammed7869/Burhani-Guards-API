using BurhaniGuards.Api.BusinessModel;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Repositories;

namespace BurhaniGuards.Api.Services;

public class MiqaatService : IMiqaatService
{
    private readonly IMiqaatRepository _miqaatRepository;

    public MiqaatService(IMiqaatRepository miqaatRepository)
    {
        _miqaatRepository = miqaatRepository;
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
            AdminApproval = "Pending",
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
            createdMiqaat.AdminApproval,
            createdMiqaat.CaptainName,
            createdMiqaat.CreatedAt,
            createdMiqaat.UpdatedAt
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
            m.AdminApproval,
            m.CaptainName,
            m.CreatedAt,
            m.UpdatedAt
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
            miqaat.AdminApproval,
            miqaat.CaptainName,
            miqaat.CreatedAt,
            miqaat.UpdatedAt
        );
    }

    public async Task Update(long id, UpdateMiqaatRequest request)
    {
        var existingMiqaat = await _miqaatRepository.GetById(id);
        if (existingMiqaat == null)
        {
            throw new Exception("Miqaat not found");
        }

        existingMiqaat.MiqaatName = request.MiqaatName;
        existingMiqaat.Jamaat = request.Jamaat;
        existingMiqaat.Jamiyat = request.Jamiyat;
        existingMiqaat.FromDate = request.FromDate;
        existingMiqaat.TillDate = request.TillDate;
        existingMiqaat.VolunteerLimit = request.VolunteerLimit;
        existingMiqaat.AboutMiqaat = request.AboutMiqaat;
        
        // Update approval status if provided
        if (!string.IsNullOrWhiteSpace(request.AdminApproval))
        {
            if (request.AdminApproval != "Pending" && 
                request.AdminApproval != "Approved" && 
                request.AdminApproval != "Rejected")
            {
                throw new Exception("Invalid approval status. Must be 'Pending', 'Approved', or 'Rejected'");
            }
            existingMiqaat.AdminApproval = request.AdminApproval;
        }
        
        existingMiqaat.UpdatedAt = DateTime.UtcNow;

        await _miqaatRepository.Update(existingMiqaat);
    }

    public async Task UpdateApprovalStatus(long id, string status)
    {
        var existingMiqaat = await _miqaatRepository.GetById(id);
        if (existingMiqaat == null)
        {
            throw new Exception("Miqaat not found");
        }

        if (status != "Pending" && status != "Approved" && status != "Rejected")
        {
            throw new Exception("Invalid approval status. Must be 'Pending', 'Approved', or 'Rejected'");
        }

        existingMiqaat.AdminApproval = status;
        existingMiqaat.UpdatedAt = DateTime.UtcNow;

        await _miqaatRepository.Update(existingMiqaat);
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
}

