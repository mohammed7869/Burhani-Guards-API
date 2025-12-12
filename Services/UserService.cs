using BurhaniGuards.Api.BusinessModel;
using BurhaniGuards.Api.Constants;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Repositories;
using BurhaniGuards.Api.ViewModel;
using Microsoft.AspNetCore.Http;

namespace BurhaniGuards.Api.Services;

public interface IUserService
{
    Task<int> Add(UserCreateViewModel viewmodel);
    Task Delete(int id);
    Task Edit(UserEditViewModel viewmodel);
    Task<List<UserListViewModel>> GetAll();
    Task<UserViewModel> GetById(int id);
    Task<UserViewModel> GetProfile(CurrentUserViewModel user);
    Task EditProfile(UserEditViewModel viewmodel);
    Task<UserViewModel?> Login(string itsId, string password);
    Task<UserViewModel?> LoginByEmail(string email, string password);
    Task<bool> ChangePassword(ChangePasswordRequest viewmodel);
    Task UpdateProfileImage(int id, string profilePath);
    Task<JamiyatJamaatResponse> GetJamiyatJamaatWithCounts();
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private CurrentUserViewModel? GetCurrentUser() => _httpContextAccessor.HttpContext?.Items["User"] as CurrentUserViewModel;

    public UserService(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int> Add(UserCreateViewModel viewmodel)
    {
        var user = new UserModel
        {
            ItsId = viewmodel.itsId,
            FullName = viewmodel.fullName,
            Email = viewmodel.email,
            Rank = viewmodel.rank,
            Roles = viewmodel.roles ?? MemberRank.GetRoleId(viewmodel.rank) ?? MemberRank.Member,
            Jamiyat = viewmodel.jamiyat,
            Jamaat = viewmodel.jamaat,
            Gender = viewmodel.gender,
            Age = viewmodel.age,
            Contact = viewmodel.contact,
            PasswordHash = !string.IsNullOrWhiteSpace(viewmodel.password) 
                ? BCrypt.Net.BCrypt.HashPassword(viewmodel.password) 
                : null,
            IsActive = true
        };

        return await _userRepository.Add(user);
    }

    public async Task Delete(int id)
    {
        var currentUser = GetCurrentUser();
        await _userRepository.Delete(id, currentUser);
    }

    public async Task Edit(UserEditViewModel viewmodel)
    {
        var user = new UserModel
        {
            Id = viewmodel.id,
            ItsId = viewmodel.itsId,
            FullName = viewmodel.fullName,
            Email = viewmodel.email,
            Rank = viewmodel.rank,
            Roles = viewmodel.roles ?? MemberRank.GetRoleId(viewmodel.rank),
            Jamiyat = viewmodel.jamiyat,
            Jamaat = viewmodel.jamaat,
            Gender = viewmodel.gender,
            Age = viewmodel.age,
            Contact = viewmodel.contact
        };

        await _userRepository.Edit(user);
    }

    public Task<List<UserListViewModel>> GetAll()
    {
        return _userRepository.List();
    }

    public async Task<UserViewModel> GetById(int id)
    {
        var user = await _userRepository.SelectUser(id);
        return MapToViewModel(user);
    }

    public async Task<UserViewModel> GetProfile(CurrentUserViewModel user)
    {
        var userModel = await _userRepository.GetProfile(user);
        return MapToViewModel(userModel);
    }

    public async Task EditProfile(UserEditViewModel viewmodel)
    {
        var user = new UserModel
        {
            Id = viewmodel.id,
            FullName = viewmodel.fullName,
            Email = viewmodel.email,
            Contact = viewmodel.contact
        };

        await _userRepository.EditProfile(user);
    }

    public async Task<UserViewModel?> Login(string itsId, string password)
    {
        try
        {
            var user = await _userRepository.GetByItsId(itsId);

            if (user == null)
            {
                return null;
            }

            // Check if new password exists - if yes, use new password; otherwise use temporary password
            bool passwordValid = false;
            bool requiresPasswordChange = string.IsNullOrWhiteSpace(user.NewPasswordHash);

            if (!string.IsNullOrWhiteSpace(user.NewPasswordHash))
            {
                // New password exists - verify against new password
                passwordValid = BCrypt.Net.BCrypt.Verify(password, user.NewPasswordHash);
            }
            else if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                // No new password - verify against temporary password
                passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                // If using temporary password, password change is required
                requiresPasswordChange = true;
            }
            else
            {
                // No password hash at all
                return null;
            }

            if (!passwordValid)
            {
                return null;
            }

            return MapToViewModel(user);
        }
        catch (Exception ex)
        {
            // Log the exception for debugging (in production, use proper logging)
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            return null;
        }
    }

    public async Task<UserViewModel?> LoginByEmail(string email, string password)
    {
        try
        {
            var user = await _userRepository.GetByEmail(email);

            if (user == null)
            {
                return null;
            }

            // Check if new password exists - if yes, use new password; otherwise use temporary password
            bool passwordValid = false;

            if (!string.IsNullOrWhiteSpace(user.NewPasswordHash))
            {
                // New password exists - verify against new password
                passwordValid = BCrypt.Net.BCrypt.Verify(password, user.NewPasswordHash);
            }
            else if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                // No new password - verify against temporary password
                passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            else
            {
                // No password hash at all
                return null;
            }

            if (!passwordValid)
            {
                return null;
            }

            return MapToViewModel(user);
        }
        catch (Exception ex)
        {
            // Log the exception for debugging (in production, use proper logging)
            System.Diagnostics.Debug.WriteLine($"LoginByEmail error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> ChangePassword(ChangePasswordRequest viewmodel)
    {
        if (string.IsNullOrWhiteSpace(viewmodel.ItsNumber) ||
            string.IsNullOrWhiteSpace(viewmodel.NewPassword) ||
            string.IsNullOrWhiteSpace(viewmodel.ConfirmPassword))
        {
            System.Diagnostics.Debug.WriteLine("ChangePassword: Missing required fields");
            return false;
        }

        // Verify passwords match
        if (viewmodel.NewPassword != viewmodel.ConfirmPassword)
        {
            System.Diagnostics.Debug.WriteLine("ChangePassword: Passwords do not match");
            return false;
        }

        try
        {
            var itsId = viewmodel.ItsNumber.Trim();
            var user = await _userRepository.GetByItsId(itsId);

            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePassword: User not found with ITS ID: {itsId}");
                return false;
            }

            if (!user.IsActive)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePassword: User is inactive: {itsId}");
                return false;
            }

            // Hash the new password and update
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(viewmodel.NewPassword);
            user.NewPasswordHash = newPasswordHash;

            await _userRepository.UpdatePassword(user);
            System.Diagnostics.Debug.WriteLine($"ChangePassword: Successfully updated password for ITS ID: {itsId}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ChangePassword error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ChangePassword stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task UpdateProfileImage(int id, string profilePath)
    {
        var user = new UserModel
        {
            Id = id,
            Profile = profilePath
        };

        await _userRepository.UpdateProfileImage(user);
    }

    public async Task<JamiyatJamaatResponse> GetJamiyatJamaatWithCounts()
    {
        var (jamiyats, jamaats) = await _userRepository.GetJamiyatJamaatWithCounts();
        return new JamiyatJamaatResponse(jamiyats, jamaats);
    }

    private UserViewModel MapToViewModel(UserModel user)
    {
        return new UserViewModel
        {
            id = (int)user.Id,
            profile = user.Profile,
            itsId = user.ItsId,
            fullName = user.FullName,
            email = user.Email,
            rank = user.Rank,
            roles = user.Roles,
            jamiyat = user.Jamiyat,
            jamaat = user.Jamaat,
            gender = user.Gender,
            age = user.Age,
            contact = user.Contact,
            passwordHash = user.PasswordHash,
            newPasswordHash = user.NewPasswordHash,
            isActive = user.IsActive,
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt
        };
    }
}

