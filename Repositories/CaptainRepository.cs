using BurhaniGuards.Api.BusinessModel;
using BurhaniGuards.Api.ViewModel;
using Dapper;
using Dapper.Contrib.Extensions;

namespace BurhaniGuards.Api.Repositories;

public interface IDapperCaptainRepository
{
    Task<int> Add(CaptainModel model);
    Task Delete(int id, CurrentCaptainViewModel user);
    Task Edit(CaptainModel model);
    Task<CaptainModel> SelectCaptain(int id);
    Task<CaptainModel> GetProfile(CurrentCaptainViewModel user);
    Task EditProfile(CaptainModel viewmodel);
    Task<CaptainModel> GetByItsNumber(string itsNumber);
    Task<CaptainModel> GetByEmail(string email);
    Task UpdatePassword(CaptainModel model);
    Task<bool> ExistsAsync(string itsNumber);
}

public class DapperCaptainRepository : IDapperCaptainRepository
{
    private readonly DapperContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private CurrentCaptainViewModel? currentUser => _httpContextAccessor.HttpContext?.Items["User"] as CurrentCaptainViewModel;

    public DapperCaptainRepository(DapperContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int> Add(CaptainModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            // Check duplicate ITS Number
            var checkDupSql = @"SELECT 1 FROM [dbo].[captains] WHERE [its_number] = @ItsNumber";
            var exists = await connection.QueryAsync<int?>(checkDupSql, new { ItsNumber = viewmodel.ItsNumber });

            if (exists.FirstOrDefault().HasValue)
            {
                throw new Exception("ITS Number already registered. Please try with another ITS Number");
            }

            // Check duplicate email
            var checkDupSql2 = @"SELECT 1 FROM [dbo].[captains] WHERE [email] = @Email";
            var exists2 = await connection.QueryAsync<int?>(checkDupSql2, new { Email = viewmodel.Email });

            if (exists2.FirstOrDefault().HasValue)
            {
                throw new Exception("Email already registered. Please try with another email");
            }

            var id = await connection.InsertAsync(viewmodel);
            return id;
        }
    }

    public async Task Delete(int id, CurrentCaptainViewModel user)
    {
        using (var connection = _context.CreateConnection())
        {
            var captain = await connection.GetAsync<CaptainModel>(id);

            if (captain == null)
            {
                throw new Exception("Captain not found");
            }

            await connection.DeleteAsync(captain);
        }
    }

    public async Task Edit(CaptainModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            var captain = await connection.GetAsync<CaptainModel>(viewmodel.Id);

            if (captain == null)
            {
                throw new Exception("Captain not found");
            }

            // Check email duplicate
            var checkDupSql2 = @"SELECT 1 FROM [dbo].[captains] WHERE [id] <> @Id AND [email] = @Email";
            var exists2 = await connection.QueryAsync<int?>(checkDupSql2, viewmodel);

            if (exists2.FirstOrDefault().HasValue)
            {
                throw new Exception("Email already registered. Please try with another email");
            }

            captain.Name = viewmodel.Name;
            captain.Email = viewmodel.Email;
            captain.UpdatedAt = DateTime.UtcNow;

            await connection.UpdateAsync(captain);
        }
    }

    public async Task<CaptainModel> SelectCaptain(int id)
    {
        using (var connection = _context.CreateConnection())
        {
            var captain = await connection.GetAsync<CaptainModel>(id);

            if (captain == null)
            {
                throw new Exception("Captain not found");
            }

            return captain;
        }
    }

    public async Task<CaptainModel> GetProfile(CurrentCaptainViewModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            var captain = await connection.GetAsync<CaptainModel>(viewmodel.id);

            if (captain == null)
            {
                throw new Exception("Captain not found");
            }

            return captain;
        }
    }

    public async Task EditProfile(CaptainModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            if (viewmodel.Id != currentUser?.id)
            {
                throw new Exception("Cannot update");
            }

            var captain = await connection.GetAsync<CaptainModel>(viewmodel.Id);

            if (captain == null)
            {
                throw new Exception("Captain not found");
            }

            // Check email duplicate
            var checkDupSql2 = @"SELECT 1 FROM [dbo].[captains] WHERE [id] <> @Id AND [email] = @Email";
            var exists2 = await connection.QueryAsync<int?>(checkDupSql2, viewmodel);

            if (exists2.FirstOrDefault().HasValue)
            {
                throw new Exception("Email already registered. Please try with another email");
            }

            captain.Name = viewmodel.Name;
            captain.Email = viewmodel.Email;
            captain.UpdatedAt = DateTime.UtcNow;

            await connection.UpdateAsync(captain);
        }
    }

    public async Task<CaptainModel> GetByItsNumber(string itsNumber)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT *
                FROM [dbo].[captains] 
                WHERE [its_number] = @ItsNumber
            ";

            var captain = await connection.QueryFirstOrDefaultAsync<CaptainModel>(sql, new { ItsNumber = itsNumber });

            if (captain == null)
            {
                throw new Exception("No account found with this ITS Number.");
            }

            return captain;
        }
    }

    public async Task<CaptainModel> GetByEmail(string email)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT *
                FROM [dbo].[captains] 
                WHERE [email] = @Email
            ";

            var captain = await connection.QueryFirstOrDefaultAsync<CaptainModel>(sql, new { Email = email });

            if (captain == null)
            {
                throw new Exception("No account found with this email.");
            }

            return captain;
        }
    }

    public async Task UpdatePassword(CaptainModel model)
    {
        using (var connection = _context.CreateConnection())
        {
            var captain = await connection.GetAsync<CaptainModel>(model.Id);

            if (captain == null)
            {
                throw new Exception("Captain not found");
            }

            captain.NewPasswordHash = model.NewPasswordHash;
            captain.UpdatedAt = DateTime.UtcNow;

            await connection.UpdateAsync(captain);
        }
    }

    public async Task<bool> ExistsAsync(string itsNumber)
    {
        using (var connection = _context.CreateConnection())
        {
            var query = "SELECT COUNT(1) FROM [dbo].[captains] WHERE [its_number] = @ItsNumber;";
            var count = await connection.ExecuteScalarAsync<int>(query, new { ItsNumber = itsNumber });
            return count > 0;
        }
    }
}

