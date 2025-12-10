using BurhaniGuards.Api.BusinessModel;
using BurhaniGuards.Api.ViewModel;
using Dapper;
using Dapper.Contrib.Extensions;

namespace BurhaniGuards.Api.Repositories;

public interface IUserRepository
{
    Task<int> Add(UserModel model);
    Task Delete(int id, CurrentUserViewModel user);
    Task Edit(UserModel model);
    Task<List<UserListViewModel>> List();
    Task<UserModel> SelectUser(int id);
    Task<UserModel> GetProfile(CurrentUserViewModel user);
    Task EditProfile(UserModel viewmodel);
    Task<UserModel?> GetByItsId(string itsId);
    Task<UserModel?> GetByEmail(string email);
    Task UpdatePassword(UserModel model);
    Task UpdateProfileImage(UserModel model);
}

public class UserRepository : IUserRepository
{
    private readonly DapperContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private CurrentUserViewModel? currentUser => _httpContextAccessor.HttpContext?.Items["User"] as CurrentUserViewModel;

    public UserRepository(DapperContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int> Add(UserModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            // Check duplicate ITS ID if provided
            if (!string.IsNullOrWhiteSpace(viewmodel.ItsId))
            {
                var checkDupSql = @"SELECT 1 FROM `users` WHERE `its_id` = @ItsId AND `is_active` = 1";
                var exists = await connection.QueryAsync<int?>(checkDupSql, new { ItsId = viewmodel.ItsId });

                if (exists.FirstOrDefault().HasValue)
                {
                    throw new Exception("ITS ID already registered. Please try with another ITS ID");
                }
            }

            // Check duplicate email
            var checkDupSql2 = @"SELECT 1 FROM `users` WHERE `email` = @Email AND `is_active` = 1";
            var exists2 = await connection.QueryAsync<int?>(checkDupSql2, new { Email = viewmodel.Email });

            if (exists2.FirstOrDefault().HasValue)
            {
                throw new Exception("Email already registered. Please try with another email");
            }

            var id = await connection.InsertAsync(viewmodel);
            return id;
        }
    }

    public async Task Delete(int id, CurrentUserViewModel user)
    {
        using (var connection = _context.CreateConnection())
        {
            var userModel = await connection.GetAsync<UserModel>(id);

            if (userModel == null)
            {
                throw new Exception("User not found");
            }

            // Soft delete
            userModel.IsActive = false;
            userModel.UpdatedAt = DateTime.UtcNow;
            await connection.UpdateAsync(userModel);
        }
    }

    public async Task Edit(UserModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            // Check if user exists
            var checkUserSql = @"SELECT 1 FROM `users` WHERE `id` = @Id";
            var userExists = await connection.QueryFirstOrDefaultAsync<int?>(checkUserSql, new { Id = viewmodel.Id });

            if (!userExists.HasValue)
            {
                throw new Exception("User not found");
            }

            // Check email duplicate
            var checkDupSql2 = @"SELECT 1 FROM `users` WHERE `id` <> @Id AND `email` = @Email AND `is_active` = 1";
            var exists2 = await connection.QueryAsync<int?>(checkDupSql2, new { Id = viewmodel.Id, Email = viewmodel.Email });

            if (exists2.FirstOrDefault().HasValue)
            {
                throw new Exception("Email already registered. Please try with another email");
            }

            // Check ITS ID duplicate if provided
            if (!string.IsNullOrWhiteSpace(viewmodel.ItsId))
            {
                var checkDupSql = @"SELECT 1 FROM `users` WHERE `id` <> @Id AND `its_id` = @ItsId AND `is_active` = 1";
                var exists = await connection.QueryAsync<int?>(checkDupSql, new { Id = viewmodel.Id, ItsId = viewmodel.ItsId });

                if (exists.FirstOrDefault().HasValue)
                {
                    throw new Exception("ITS ID already registered. Please try with another ITS ID");
                }
            }

            // Use explicit SQL update with snake_case column names
            var updateSql = @"
                UPDATE `users` 
                SET 
                    `its_id` = @ItsId,
                    `full_name` = @FullName,
                    `email` = @Email,
                    `rank` = @Rank,
                    `roles` = @Roles,
                    `jamiyat` = @Jamiyat,
                    `jamaat` = @Jamaat,
                    `gender` = @Gender,
                    `age` = @Age,
                    `contact` = @Contact,
                    `updated_at` = CURRENT_TIMESTAMP
                WHERE `id` = @Id
            ";

            var rowsAffected = await connection.ExecuteAsync(updateSql, new
            {
                Id = viewmodel.Id,
                ItsId = viewmodel.ItsId,
                FullName = viewmodel.FullName,
                Email = viewmodel.Email,
                Rank = viewmodel.Rank,
                Roles = viewmodel.Roles,
                Jamiyat = viewmodel.Jamiyat,
                Jamaat = viewmodel.Jamaat,
                Gender = viewmodel.Gender,
                Age = viewmodel.Age,
                Contact = viewmodel.Contact
            });

            if (rowsAffected == 0)
            {
                throw new Exception("Failed to update user");
            }
        }
    }

    public async Task<List<UserListViewModel>> List()
    {
        string sql = @"
            SELECT 
                u.`id`,
                u.`profile`,
                u.`its_id` AS itsId,
                u.`rank`,
                u.`roles`,
                u.`jamiyat`,
                u.`jamaat`,
                u.`full_name` AS fullName,
                u.`gender`,
                u.`email`,
                u.`age`,
                u.`contact`,
                u.`is_active` AS isActive,
                u.`created_at` AS createdAt,
                u.`updated_at` AS updatedAt
            FROM `users` u
            WHERE u.`is_active` = 1
            ORDER BY u.`created_at` DESC
        ";

        using (var connection = _context.CreateConnection())
        {
            var result = await connection.QueryAsync<UserListViewModel>(sql);
            return result.ToList();
        }
    }

    public async Task<UserModel> SelectUser(int id)
    {
        using (var connection = _context.CreateConnection())
        {
            // Use explicit column mapping to ensure proper mapping
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `profile` AS Profile,
                    `its_id` AS ItsId,
                    `rank` AS `Rank`,
                    `roles` AS Roles,
                    `jamiyat` AS Jamiyat,
                    `jamaat` AS Jamaat,
                    `full_name` AS FullName,
                    `gender` AS Gender,
                    `email` AS Email,
                    `age` AS Age,
                    `contact` AS Contact,
                    `password_hash` AS PasswordHash,
                    `new_password_hash` AS NewPasswordHash,
                    `is_active` AS IsActive,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `users` 
                WHERE `id` = @Id
            ";

            var user = await connection.QueryFirstOrDefaultAsync<UserModel>(sql, new { Id = id });

            if (user == null)
            {
                throw new Exception("User not found");
            }

            return user;
        }
    }

    public async Task<UserModel> GetProfile(CurrentUserViewModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            var user = await connection.GetAsync<UserModel>(viewmodel.id);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            return user;
        }
    }

    public async Task EditProfile(UserModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            if (viewmodel.Id != currentUser?.id)
            {
                throw new Exception("Cannot update");
            }

            var user = await connection.GetAsync<UserModel>(viewmodel.Id);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            // Check email duplicate
            var checkDupSql2 = @"SELECT 1 FROM `users` WHERE `id` <> @Id AND `email` = @Email AND `is_active` = 1";
            var exists2 = await connection.QueryAsync<int?>(checkDupSql2, viewmodel);

            if (exists2.FirstOrDefault().HasValue)
            {
                throw new Exception("Email already registered. Please try with another email");
            }

            user.FullName = viewmodel.FullName;
            user.Email = viewmodel.Email;
            user.Contact = viewmodel.Contact;
            user.UpdatedAt = DateTime.UtcNow;

            await connection.UpdateAsync(user);
        }
    }

    public async Task<UserModel?> GetByItsId(string itsId)
    {
        using (var connection = _context.CreateConnection())
        {
            // Use explicit column mapping to ensure proper mapping
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `profile` AS Profile,
                    `its_id` AS ItsId,
                    `rank` AS `Rank`,
                    `roles` AS Roles,
                    `jamiyat` AS Jamiyat,
                    `jamaat` AS Jamaat,
                    `full_name` AS FullName,
                    `gender` AS Gender,
                    `email` AS Email,
                    `age` AS Age,
                    `contact` AS Contact,
                    `password_hash` AS PasswordHash,
                    `new_password_hash` AS NewPasswordHash,
                    `is_active` AS IsActive,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `users` 
                WHERE `its_id` = @ItsId AND `is_active` = 1
            ";

            var user = await connection.QueryFirstOrDefaultAsync<UserModel>(sql, new { ItsId = itsId });

            return user;
        }
    }

    public async Task<UserModel?> GetByEmail(string email)
    {
        using (var connection = _context.CreateConnection())
        {
            // Use explicit column mapping to ensure proper mapping
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `profile` AS Profile,
                    `its_id` AS ItsId,
                    `rank` AS `Rank`,
                    `roles` AS Roles,
                    `jamiyat` AS Jamiyat,
                    `jamaat` AS Jamaat,
                    `full_name` AS FullName,
                    `gender` AS Gender,
                    `email` AS Email,
                    `age` AS Age,
                    `contact` AS Contact,
                    `password_hash` AS PasswordHash,
                    `new_password_hash` AS NewPasswordHash,
                    `is_active` AS IsActive,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `users` 
                WHERE `email` = @Email AND `is_active` = 1
            ";

            var user = await connection.QueryFirstOrDefaultAsync<UserModel>(sql, new { Email = email });

            return user;
        }
    }

    public async Task UpdatePassword(UserModel model)
    {
        using (var connection = _context.CreateConnection())
        {
            // Update directly using ITS ID for better reliability
            var sql = @"
                UPDATE `users` 
                SET `new_password_hash` = @NewPasswordHash, 
                    `updated_at` = CURRENT_TIMESTAMP
                WHERE `its_id` = @ItsId AND `is_active` = 1
            ";

            var rowsAffected = await connection.ExecuteAsync(sql, new 
            { 
                NewPasswordHash = model.NewPasswordHash,
                ItsId = model.ItsId
            });

            if (rowsAffected == 0)
            {
                throw new Exception("User not found or inactive");
            }
        }
    }

    public async Task UpdateProfileImage(UserModel model)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                UPDATE `users` 
                SET `profile` = @Profile, 
                    `updated_at` = CURRENT_TIMESTAMP
                WHERE `id` = @Id AND `is_active` = 1
            ";

            var rowsAffected = await connection.ExecuteAsync(sql, new 
            { 
                Profile = model.Profile,
                Id = model.Id
            });

            if (rowsAffected == 0)
            {
                throw new Exception("User not found or inactive");
            }
        }
    }
}

