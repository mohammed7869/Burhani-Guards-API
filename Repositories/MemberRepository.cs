using BurhaniGuards.Api.BusinessModel;
using BurhaniGuards.Api.ViewModel;
using Dapper;
using Dapper.Contrib.Extensions;

namespace BurhaniGuards.Api.Repositories;

public interface IDapperMemberRepository
{
    Task<int> Add(MemberModel model);
    Task Delete(int id, CurrentMemberViewModel user);
    Task Edit(MemberModel model);
    Task<List<MemberListViewModel>> List();
    Task<MemberModel> SelectMember(int id);
    Task<MemberModel> GetProfile(CurrentMemberViewModel user);
    Task EditProfile(MemberModel viewmodel);
    Task<MemberModel> GetByItsId(string itsId);
    Task<MemberModel> GetByEmail(string email);
    Task UpdatePassword(MemberModel model);
}

public class DapperMemberRepository : IDapperMemberRepository
{
    private readonly DapperContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private CurrentMemberViewModel? currentUser => _httpContextAccessor.HttpContext?.Items["User"] as CurrentMemberViewModel;

    public DapperMemberRepository(DapperContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int> Add(MemberModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            // Check duplicate ITS ID
            var checkDupSql = @"SELECT 1 FROM [dbo].[members] WHERE [its_id] = @ItsId AND [is_active] = 1";
            var exists = await connection.QueryAsync<int?>(checkDupSql, new { ItsId = viewmodel.ItsId });

            if (exists.FirstOrDefault().HasValue)
            {
                throw new Exception("ITS ID already registered. Please try with another ITS ID");
            }

            // Check duplicate email
            var checkDupSql2 = @"SELECT 1 FROM [dbo].[members] WHERE [email] = @Email AND [is_active] = 1";
            var exists2 = await connection.QueryAsync<int?>(checkDupSql2, new { Email = viewmodel.Email });

            if (exists2.FirstOrDefault().HasValue)
            {
                throw new Exception("Email already registered. Please try with another email");
            }

            var id = await connection.InsertAsync(viewmodel);
            return id;
        }
    }

    public async Task Delete(int id, CurrentMemberViewModel user)
    {
        using (var connection = _context.CreateConnection())
        {
            var member = await connection.GetAsync<MemberModel>(id);

            if (member == null)
            {
                throw new Exception("Member not found");
            }

            // Soft delete
            member.IsActive = false;
            member.UpdatedAt = DateTime.UtcNow;
            await connection.UpdateAsync(member);
        }
    }

    public async Task Edit(MemberModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            var member = await connection.GetAsync<MemberModel>(viewmodel.Id);

            if (member == null)
            {
                throw new Exception("Member not found");
            }

            // Check email duplicate
            var checkDupSql2 = @"SELECT 1 FROM [dbo].[members] WHERE [id] <> @Id AND [email] = @Email AND [is_active] = 1";
            var exists2 = await connection.QueryAsync<int?>(checkDupSql2, viewmodel);

            if (exists2.FirstOrDefault().HasValue)
            {
                throw new Exception("Email already registered. Please try with another email");
            }

            member.FullName = viewmodel.FullName;
            member.Email = viewmodel.Email;
            member.Rank = viewmodel.Rank;
            member.Jamiyat = viewmodel.Jamiyat;
            member.Jamaat = viewmodel.Jamaat;
            member.Gender = viewmodel.Gender;
            member.Age = viewmodel.Age;
            member.Contact = viewmodel.Contact;
            member.UpdatedAt = DateTime.UtcNow;

            await connection.UpdateAsync(member);
        }
    }

    public async Task<List<MemberListViewModel>> List()
    {
        string sql = @"
            SELECT 
                m.[id],
                m.[its_id] AS itsId,
                m.[full_name] AS fullName,
                m.[email],
                m.[rank],
                m.[is_active] AS isActive,
                m.[created_at] AS createdAt
            FROM [dbo].[members] m
            WHERE m.[is_active] = 1
            ORDER BY m.[created_at] DESC
        ";

        using (var connection = _context.CreateConnection())
        {
            var result = await connection.QueryAsync<MemberListViewModel>(sql);
            return result.ToList();
        }
    }

    public async Task<MemberModel> SelectMember(int id)
    {
        using (var connection = _context.CreateConnection())
        {
            var member = await connection.GetAsync<MemberModel>(id);

            if (member == null)
            {
                throw new Exception("Member not found");
            }

            return member;
        }
    }

    public async Task<MemberModel> GetProfile(CurrentMemberViewModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            var member = await connection.GetAsync<MemberModel>(viewmodel.id);

            if (member == null)
            {
                throw new Exception("Member not found");
            }

            return member;
        }
    }

    public async Task EditProfile(MemberModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            if (viewmodel.Id != currentUser?.id)
            {
                throw new Exception("Cannot update");
            }

            var member = await connection.GetAsync<MemberModel>(viewmodel.Id);

            if (member == null)
            {
                throw new Exception("Member not found");
            }

            // Check email duplicate
            var checkDupSql2 = @"SELECT 1 FROM [dbo].[members] WHERE [id] <> @Id AND [email] = @Email AND [is_active] = 1";
            var exists2 = await connection.QueryAsync<int?>(checkDupSql2, viewmodel);

            if (exists2.FirstOrDefault().HasValue)
            {
                throw new Exception("Email already registered. Please try with another email");
            }

            member.FullName = viewmodel.FullName;
            member.Email = viewmodel.Email;
            member.Contact = viewmodel.Contact;
            member.UpdatedAt = DateTime.UtcNow;

            await connection.UpdateAsync(member);
        }
    }

    public async Task<MemberModel> GetByItsId(string itsId)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT *
                FROM [dbo].[members] 
                WHERE [its_id] = @ItsId AND [is_active] = 1
            ";

            var member = await connection.QueryFirstOrDefaultAsync<MemberModel>(sql, new { ItsId = itsId });

            if (member == null)
            {
                throw new Exception("No account found with this ITS ID.");
            }

            return member;
        }
    }

    public async Task<MemberModel> GetByEmail(string email)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT *
                FROM [dbo].[members] 
                WHERE [email] = @Email AND [is_active] = 1
            ";

            var member = await connection.QueryFirstOrDefaultAsync<MemberModel>(sql, new { Email = email });

            if (member == null)
            {
                throw new Exception("No account found with this email.");
            }

            return member;
        }
    }

    public async Task UpdatePassword(MemberModel model)
    {
        using (var connection = _context.CreateConnection())
        {
            var member = await connection.GetAsync<MemberModel>(model.Id);

            if (member == null)
            {
                throw new Exception("Member not found");
            }

            member.NewPasswordHash = model.NewPasswordHash;
            member.UpdatedAt = DateTime.UtcNow;

            await connection.UpdateAsync(member);
        }
    }
}

