using BurhaniGuards.Api.BusinessModel;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Repositories;
using BurhaniGuards.Api.ViewModel;

namespace BurhaniGuards.Api.Services;

public interface IMemberService
{
    Task<int> Add(MemberViewModel viewmodel);
    Task Delete(int id);
    Task Edit(MemberViewModel viewmodel);
    Task<List<MemberListViewModel>> GetAll();
    Task<MemberViewModel> GetById(int id);
    Task<MemberViewModel> GetProfile(CurrentMemberViewModel user);
    Task EditProfile(MemberViewModel viewmodel);
    Task<MemberViewModel?> Login(string itsId, string password);
    Task<bool> ChangePassword(ChangePasswordRequest viewmodel);
}

public class MemberService : IMemberService
{
    private readonly IDapperMemberRepository _memberRepository;

    public MemberService(IDapperMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<int> Add(MemberViewModel viewmodel)
    {
        var member = new MemberModel
        {
            ItsId = viewmodel.itsId,
            FullName = viewmodel.fullName,
            Email = viewmodel.email,
            Rank = viewmodel.rank,
            Jamiyat = viewmodel.jamiyat,
            Jamaat = viewmodel.jamaat,
            Gender = viewmodel.gender,
            Age = viewmodel.age,
            Contact = viewmodel.contact,
            PasswordHash = viewmodel.passwordHash,
            IsActive = true
        };

        return await _memberRepository.Add(member);
    }

    public Task Delete(int id)
    {
        throw new NotImplementedException();
    }

    public async Task Edit(MemberViewModel viewmodel)
    {
        var member = new MemberModel
        {
            Id = viewmodel.id,
            FullName = viewmodel.fullName,
            Email = viewmodel.email,
            Rank = viewmodel.rank,
            Jamiyat = viewmodel.jamiyat,
            Jamaat = viewmodel.jamaat,
            Gender = viewmodel.gender,
            Age = viewmodel.age,
            Contact = viewmodel.contact
        };

        await _memberRepository.Edit(member);
    }

    public Task<List<MemberListViewModel>> GetAll()
    {
        return _memberRepository.List();
    }

    public async Task<MemberViewModel> GetById(int id)
    {
        var member = await _memberRepository.SelectMember(id);
        return new MemberViewModel
        {
            id = (int)member.Id,
            itsId = member.ItsId,
            fullName = member.FullName,
            email = member.Email,
            rank = member.Rank,
            jamiyat = member.Jamiyat,
            jamaat = member.Jamaat,
            gender = member.Gender,
            age = member.Age,
            contact = member.Contact,
            isActive = member.IsActive,
            createdAt = member.CreatedAt,
            updatedAt = member.UpdatedAt
        };
    }

    public async Task<MemberViewModel> GetProfile(CurrentMemberViewModel user)
    {
        var member = await _memberRepository.GetProfile(user);
        return new MemberViewModel
        {
            id = (int)member.Id,
            itsId = member.ItsId,
            fullName = member.FullName,
            email = member.Email,
            rank = member.Rank,
            jamiyat = member.Jamiyat,
            jamaat = member.Jamaat,
            gender = member.Gender,
            age = member.Age,
            contact = member.Contact,
            isActive = member.IsActive,
            createdAt = member.CreatedAt,
            updatedAt = member.UpdatedAt
        };
    }

    public async Task EditProfile(MemberViewModel viewmodel)
    {
        var member = new MemberModel
        {
            Id = viewmodel.id,
            FullName = viewmodel.fullName,
            Email = viewmodel.email,
            Contact = viewmodel.contact
        };

        await _memberRepository.EditProfile(member);
    }

    public async Task<MemberViewModel?> Login(string itsId, string password)
    {
        try
        {
            var member = await _memberRepository.GetByItsId(itsId);

            if (member == null)
            {
                return null;
            }

            // Check if new password exists - if yes, use new password; otherwise use temporary password
            bool passwordValid = false;

            if (!string.IsNullOrWhiteSpace(member.NewPasswordHash))
            {
                // New password exists - verify against new password
                passwordValid = BCrypt.Net.BCrypt.Verify(password, member.NewPasswordHash);
            }
            else if (!string.IsNullOrWhiteSpace(member.PasswordHash))
            {
                // No new password - verify against temporary password
                passwordValid = BCrypt.Net.BCrypt.Verify(password, member.PasswordHash);
            }

            if (!passwordValid)
            {
                return null;
            }

            return new MemberViewModel
            {
                id = (int)member.Id,
                itsId = member.ItsId,
                fullName = member.FullName,
                email = member.Email,
                rank = member.Rank,
                isActive = member.IsActive
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
            var member = await _memberRepository.GetByItsId(viewmodel.ItsNumber.Trim());

            if (member == null)
            {
                return false;
            }

            // Hash the new password and update
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(viewmodel.NewPassword);
            member.NewPasswordHash = newPasswordHash;

            await _memberRepository.UpdatePassword(member);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

