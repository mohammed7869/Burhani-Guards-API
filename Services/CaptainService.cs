using BurhaniGuards.Api.BusinessModel;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Repositories;
using BurhaniGuards.Api.ViewModel;

namespace BurhaniGuards.Api.Services;

public interface ICaptainService
{
    Task<int> Add(CaptainViewModel viewmodel);
    Task Delete(int id);
    Task Edit(CaptainViewModel viewmodel);
    Task<CaptainViewModel> GetById(int id);
    Task<CaptainViewModel> GetProfile(CurrentCaptainViewModel user);
    Task EditProfile(CaptainViewModel viewmodel);
    Task<CaptainViewModel?> Login(string itsNumber, string password);
    Task<bool> ChangePassword(ChangePasswordRequest viewmodel);
}

public class CaptainService : ICaptainService
{
    private readonly IDapperCaptainRepository _captainRepository;

    public CaptainService(IDapperCaptainRepository captainRepository)
    {
        _captainRepository = captainRepository;
    }

    public async Task<int> Add(CaptainViewModel viewmodel)
    {
        var captain = new CaptainModel
        {
            ItsNumber = viewmodel.itsNumber,
            Name = viewmodel.name,
            Email = viewmodel.email,
            PasswordHash = viewmodel.passwordHash
        };

        return await _captainRepository.Add(captain);
    }

    public Task Delete(int id)
    {
        throw new NotImplementedException();
    }

    public async Task Edit(CaptainViewModel viewmodel)
    {
        var captain = new CaptainModel
        {
            Id = viewmodel.id,
            Name = viewmodel.name,
            Email = viewmodel.email
        };

        await _captainRepository.Edit(captain);
    }

    public async Task<CaptainViewModel> GetById(int id)
    {
        var captain = await _captainRepository.SelectCaptain(id);
        return new CaptainViewModel
        {
            id = (int)captain.Id,
            itsNumber = captain.ItsNumber,
            name = captain.Name,
            email = captain.Email,
            passwordHash = captain.PasswordHash,
            newPasswordHash = captain.NewPasswordHash,
            createdAt = captain.CreatedAt,
            updatedAt = captain.UpdatedAt
        };
    }

    public async Task<CaptainViewModel> GetProfile(CurrentCaptainViewModel user)
    {
        var captain = await _captainRepository.GetProfile(user);
        return new CaptainViewModel
        {
            id = (int)captain.Id,
            itsNumber = captain.ItsNumber,
            name = captain.Name,
            email = captain.Email,
            passwordHash = captain.PasswordHash,
            newPasswordHash = captain.NewPasswordHash,
            createdAt = captain.CreatedAt,
            updatedAt = captain.UpdatedAt
        };
    }

    public async Task EditProfile(CaptainViewModel viewmodel)
    {
        var captain = new CaptainModel
        {
            Id = viewmodel.id,
            Name = viewmodel.name,
            Email = viewmodel.email
        };

        await _captainRepository.EditProfile(captain);
    }

    public async Task<CaptainViewModel?> Login(string itsNumber, string password)
    {
        try
        {
            var captain = await _captainRepository.GetByItsNumber(itsNumber);

            if (captain == null)
            {
                return null;
            }

            // Check if new password exists - if yes, use new password; otherwise use temporary password
            bool passwordValid = false;

            if (!string.IsNullOrWhiteSpace(captain.NewPasswordHash))
            {
                // New password exists - verify against new password
                passwordValid = BCrypt.Net.BCrypt.Verify(password, captain.NewPasswordHash);
            }
            else if (!string.IsNullOrWhiteSpace(captain.PasswordHash))
            {
                // No new password - verify against temporary password
                passwordValid = BCrypt.Net.BCrypt.Verify(password, captain.PasswordHash);
            }

            if (!passwordValid)
            {
                return null;
            }

            return new CaptainViewModel
            {
                id = (int)captain.Id,
                itsNumber = captain.ItsNumber,
                name = captain.Name,
                email = captain.Email
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ChangePassword(ChangePasswordRequest viewmodel)
    {
        if (string.IsNullOrWhiteSpace(viewmodel.ItsNumber) ||
            string.IsNullOrWhiteSpace(viewmodel.NewPassword) ||
            string.IsNullOrWhiteSpace(viewmodel.ConfirmPassword))
        {
            return false;
        }

        // Verify passwords match
        if (viewmodel.NewPassword != viewmodel.ConfirmPassword)
        {
            return false;
        }

        try
        {
            var captain = await _captainRepository.GetByItsNumber(viewmodel.ItsNumber.Trim());

            if (captain == null)
            {
                return false;
            }

            // Hash the new password and update
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(viewmodel.NewPassword);
            captain.NewPasswordHash = newPasswordHash;

            await _captainRepository.UpdatePassword(captain);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

