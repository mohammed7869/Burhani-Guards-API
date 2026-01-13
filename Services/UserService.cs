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
    Task ApproveMember(int id);
    Task<List<MemberModel>> GetMembersByJamaatAsync(string jamaat);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;
    private CurrentUserViewModel? GetCurrentUser() => _httpContextAccessor.HttpContext?.Items["User"] as CurrentUserViewModel;

    public UserService(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor, IEmailService emailService)
    {
        _userRepository = userRepository;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
    }

    public async Task<int> Add(UserCreateViewModel viewmodel)
    {
        var currentUser = GetCurrentUser();
        
        // Determine is_approved based on current user role
        // If current user is Admin (ResourceAdmin = 7), set is_approved = true
        // If current user is Captain (Captain = 2), set is_approved = false
        // Default to true if no current user (shouldn't happen in production)
        bool isApproved = true;
        if (currentUser != null)
        {
            // If current user is Captain (roles = 2), member needs approval
            if (currentUser.roles == MemberRank.Captain)
            {
                isApproved = false;
            }
            // If current user is Admin (roles = 7), member is auto-approved
            else if (currentUser.roles == MemberRank.ResourceAdmin)
            {
                isApproved = true;
            }
        }
        
        // Override with explicit value if provided
        if (viewmodel.isApproved.HasValue)
        {
            isApproved = viewmodel.isApproved.Value;
        }

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
            IsActive = true,
            IsApproved = isApproved,
            CreatedBy = currentUser?.fullName
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

            // Send congratulatory email to the user
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    var subject = "Password Changed Successfully";
                    var body = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                                <h2 style='color: #28a745;'>Congratulations!</h2>
                                <p>Dear {user.FullName},</p>
                                <p>We are pleased to inform you that your password has been changed successfully.</p>
                                <p>Your new password has been set and is now active. You can now login to your account using your new Password</p>
                                <p>If you did not make this change, please contact us immediately.</p>
                                <p style='margin-top: 30px;'>Best regards,<br>Burhani Guards Pune Team</p>
                            </div>
                        </body>
                        </html>";

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    System.Diagnostics.Debug.WriteLine($"ChangePassword: Successfully sent email notification to {user.Email}");
                }
                catch (Exception emailEx)
                {
                    // Log email error but don't fail the password change
                    System.Diagnostics.Debug.WriteLine($"ChangePassword: Failed to send email notification: {emailEx.Message}");
                }
            }

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

    public async Task ApproveMember(int id)
    {
        var currentUser = GetCurrentUser();
        
        // Only ResourceAdmin (Admin) can approve members
        if (currentUser == null || currentUser.roles != MemberRank.ResourceAdmin)
        {
            throw new UnauthorizedAccessException("Only Admin can approve members");
        }

        await _userRepository.ApproveMember(id);
    }

    public async Task<List<MemberModel>> GetMembersByJamaatAsync(string jamaat)
    {
        return await _userRepository.GetMembersByJamaatAsync(jamaat);
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
            isApproved = user.IsApproved,
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt
        };
    }
}

