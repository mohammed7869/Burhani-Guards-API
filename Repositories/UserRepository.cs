using BurhaniGuards.Api.BusinessModel;
using BurhaniGuards.Api.Contracts.Responses;
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
    Task<(List<JamiyatItem> Jamiyats, List<JamaatItem> Jamaats)> GetJamiyatJamaatWithCounts();
    Task<List<string>> GetAdminEmailsAsync();
    Task<List<int>> GetAdminUserIdsAsync();
    Task<List<MemberModel>> GetMembersByJamiyatAsync(string jamiyat);
    Task<List<MemberModel>> GetMembersByJamaatAsync(string jamaat);
    Task<List<MemberModel>> GetHierarchyMembersByJamaatAsync(string jamaat);
    Task<MemberModel?> GetCaptainByFullNameAsync(string captainName);
    Task<MemberModel?> GetCaptainByJamaatAsync(string jamaat);
    Task ApproveMember(int id);
    Task<IEnumerable<int>> GetAllUserIdsAsync();
    Task<IEnumerable<int>> GetUserIdsByJamaatAsync(string jamaat);
    Task UpdateFcmTokenAsync(int userId, string token);
    Task<string?> GetFcmTokenAsync(int userId);
    Task<Dictionary<int, string>> GetFcmTokensAsync(IEnumerable<int> userIds);
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
                var checkDupSql = @"SELECT 1 FROM `members` WHERE `its_id` = @ItsId AND `is_active` = 1";
                var exists = await connection.QueryAsync<int?>(checkDupSql, new { ItsId = viewmodel.ItsId });

                if (exists.FirstOrDefault().HasValue)
                {
                    throw new Exception("ITS ID already registered. Please try with another ITS ID");
                }
            }

            // Check duplicate email
            var checkDupSql2 = @"SELECT 1 FROM `members` WHERE `email` = @Email AND `is_active` = 1";
            var exists2 = await connection.QueryAsync<int?>(checkDupSql2, new { Email = viewmodel.Email });

            if (exists2.FirstOrDefault().HasValue)
            {
                throw new Exception("Email already registered. Please try with another email");
            }

            // Use explicit SQL INSERT with snake_case column names
            var insertSql = @"
                INSERT INTO `members` 
                (
                    `its_id`, `full_name`, `email`, `rank`, `roles`, 
                    `jamiyat`, `jamaat`, `jamiyat_id`, `jamaat_id`, 
                    `gender`, `age`, `contact`, `date_of_birth`, `password_hash`, 
                    `is_active`, `is_approved`, `created_by`, `created_at`, `updated_at`
                )
                VALUES 
                (
                    @ItsId, @FullName, @Email, @Rank, @Roles,
                    @Jamiyat, @Jamaat, @JamiyatId, @JamaatId,
                    @Gender, @Age, @Contact, @DateOfBirth, @PasswordHash,
                    @IsActive, @IsApproved, @CreatedBy, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                );
                SELECT LAST_INSERT_ID();
            ";

            var id = await connection.QuerySingleAsync<int>(insertSql, new
            {
                ItsId = viewmodel.ItsId,
                FullName = viewmodel.FullName,
                Email = viewmodel.Email,
                Rank = viewmodel.Rank,
                Roles = viewmodel.Roles,
                Jamiyat = viewmodel.Jamiyat,
                Jamaat = viewmodel.Jamaat,
                JamiyatId = viewmodel.JamiyatId,
                JamaatId = viewmodel.JamaatId,
                Gender = viewmodel.Gender,
                Age = viewmodel.Age,
                Contact = viewmodel.Contact,
                DateOfBirth = viewmodel.DateOfBirth,
                PasswordHash = viewmodel.PasswordHash,
                IsActive = viewmodel.IsActive,
                IsApproved = viewmodel.IsApproved,
                CreatedBy = viewmodel.CreatedBy
            });

            return id;
        }
    }

    public async Task Delete(int id, CurrentUserViewModel user)
    {
        using (var connection = _context.CreateConnection())
        {
            // Use raw SQL instead of Dapper.Contrib to avoid property-to-column name mismatch
            // (e.g., ItsId vs its_id, FullName vs full_name)
            var checkSql = @"SELECT COUNT(1) FROM `members` WHERE `id` = @Id AND `is_active` = 1";
            var exists = await connection.ExecuteScalarAsync<int>(checkSql, new { Id = id });

            if (exists == 0)
            {
                throw new Exception("User not found");
            }

            // Soft delete
            var sql = @"
                UPDATE `members` 
                SET `is_active` = 0, 
                    `updated_at` = CURRENT_TIMESTAMP
                WHERE `id` = @Id";
            await connection.ExecuteAsync(sql, new { Id = id });
        }
    }

    public async Task Edit(UserModel viewmodel)
    {
        using (var connection = _context.CreateConnection())
        {
            // Check if user exists
            var checkUserSql = @"SELECT 1 FROM `members` WHERE `id` = @Id";
            var userExists = await connection.QueryFirstOrDefaultAsync<int?>(checkUserSql, new { Id = viewmodel.Id });

            if (!userExists.HasValue)
            {
                throw new Exception("User not found");
            }

            // Check email duplicate
            var checkDupSql2 = @"SELECT 1 FROM `members` WHERE `id` <> @Id AND `email` = @Email AND `is_active` = 1";
            var exists2 = await connection.QueryAsync<int?>(checkDupSql2, new { Id = viewmodel.Id, Email = viewmodel.Email });

            if (exists2.FirstOrDefault().HasValue)
            {
                throw new Exception("Email already registered. Please try with another email");
            }

            // Check ITS ID duplicate if provided
            if (!string.IsNullOrWhiteSpace(viewmodel.ItsId))
            {
                var checkDupSql = @"SELECT 1 FROM `members` WHERE `id` <> @Id AND `its_id` = @ItsId AND `is_active` = 1";
                var exists = await connection.QueryAsync<int?>(checkDupSql, new { Id = viewmodel.Id, ItsId = viewmodel.ItsId });

                if (exists.FirstOrDefault().HasValue)
                {
                    throw new Exception("ITS ID already registered. Please try with another ITS ID");
                }
            }

            // Use explicit SQL update with snake_case column names
            var updateSql = @"
                UPDATE `members` 
                SET 
                    `its_id` = @ItsId,
                    `full_name` = @FullName,
                    `email` = @Email,
                    `rank` = @Rank,
                    `roles` = @Roles,
                    `jamiyat` = @Jamiyat,
                    `jamaat` = @Jamaat,
                    `jamiyat_id` = @JamiyatId,
                    `jamaat_id` = @JamaatId,
                    `gender` = @Gender,
                    `age` = @Age,
                    `contact` = @Contact,
                    `date_of_birth` = @DateOfBirth,
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
                JamiyatId = viewmodel.JamiyatId,
                JamaatId = viewmodel.JamaatId,
                Gender = viewmodel.Gender,
                Age = viewmodel.Age,
                Contact = viewmodel.Contact,
                DateOfBirth = viewmodel.DateOfBirth
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
                u.`date_of_birth` AS dateOfBirth,
                u.`is_active` AS isActive,
                u.`is_approved` AS isApproved,
                u.`badge`,
                u.`created_at` AS createdAt,
                u.`updated_at` AS updatedAt
            FROM `members` u
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
                    `jamiyat_id` AS JamiyatId,
                    `jamaat_id` AS JamaatId,
                    `full_name` AS FullName,
                    `gender` AS Gender,
                    `email` AS Email,
                    `age` AS Age,
                    `contact` AS Contact,
                    `date_of_birth` AS DateOfBirth,
                    `password_hash` AS PasswordHash,
                    `new_password_hash` AS NewPasswordHash,
                    `is_active` AS IsActive,
                    `is_approved` AS IsApproved,
                    `badge` AS Badge,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `members` 
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
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `profile` AS Profile,
                    `its_id` AS ItsId,
                    `rank` AS Rank,
                    `roles` AS Roles,
                    `jamiyat` AS Jamiyat,
                    `jamaat` AS Jamaat,
                    `jamiyat_id` AS JamiyatId,
                    `jamaat_id` AS JamaatId,
                    `full_name` AS FullName,
                    `gender` AS Gender,
                    `email` AS Email,
                    `age` AS Age,
                    `contact` AS Contact,
                    `date_of_birth` AS DateOfBirth,
                    `password_hash` AS PasswordHash,
                    `new_password_hash` AS NewPasswordHash,
                    `is_active` AS IsActive,
                    `is_approved` AS IsApproved,
                    `badge` AS Badge,
                    `created_by` AS CreatedBy,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `members`
                WHERE `id` = @Id AND `is_active` = 1
            ";
            var user = await connection.QueryFirstOrDefaultAsync<UserModel>(sql, new { Id = viewmodel.id });

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

            var checkSql = @"SELECT COUNT(1) FROM `members` WHERE `id` = @Id AND `is_active` = 1";
            var userExists = await connection.ExecuteScalarAsync<int>(checkSql, new { Id = viewmodel.Id });

            if (userExists == 0)
            {
                throw new Exception("User not found");
            }

            // Check email duplicate
            var checkDupSql2 = @"SELECT 1 FROM `members` WHERE `id` <> @Id AND `email` = @Email AND `is_active` = 1";
            var exists2 = await connection.QueryAsync<int?>(checkDupSql2, viewmodel);

            if (exists2.FirstOrDefault().HasValue)
            {
                throw new Exception("Email already registered. Please try with another email");
            }

            var updateSql = @"
                UPDATE `members` 
                SET `full_name` = @FullName,
                    `email` = @Email,
                    `contact` = @Contact,
                    `date_of_birth` = @DateOfBirth,
                    `updated_at` = CURRENT_TIMESTAMP
                WHERE `id` = @Id AND `is_active` = 1
            ";
            await connection.ExecuteAsync(updateSql, new 
            { 
                FullName = viewmodel.FullName,
                Email = viewmodel.Email,
                Contact = viewmodel.Contact,
                DateOfBirth = viewmodel.DateOfBirth,
                Id = viewmodel.Id
            });
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
                    `jamiyat_id` AS JamiyatId,
                    `jamaat_id` AS JamaatId,
                    `full_name` AS FullName,
                    `gender` AS Gender,
                    `email` AS Email,
                    `age` AS Age,
                    `contact` AS Contact,
                    `date_of_birth` AS DateOfBirth,
                    `password_hash` AS PasswordHash,
                    `new_password_hash` AS NewPasswordHash,
                    `is_active` AS IsActive,
                    `is_approved` AS IsApproved,
                    `badge` AS Badge,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `members` 
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
                    `jamiyat_id` AS JamiyatId,
                    `jamaat_id` AS JamaatId,
                    `full_name` AS FullName,
                    `gender` AS Gender,
                    `email` AS Email,
                    `age` AS Age,
                    `contact` AS Contact,
                    `date_of_birth` AS DateOfBirth,
                    `password_hash` AS PasswordHash,
                    `new_password_hash` AS NewPasswordHash,
                    `is_active` AS IsActive,
                    `is_approved` AS IsApproved,
                    `badge` AS Badge,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `members` 
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
                UPDATE `members` 
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
                UPDATE `members` 
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

    public async Task<(List<JamiyatItem> Jamiyats, List<JamaatItem> Jamaats)> GetJamiyatJamaatWithCounts()
    {
        using (var connection = _context.CreateConnection())
        {
            // Get distinct jamiyat with counts
            var jamiyatSql = @"
                SELECT 
                    jamiyat AS Name,
                    COUNT(*) AS Count
                FROM `members`
                WHERE `is_active` = 1 
                    AND `jamiyat` IS NOT NULL 
                    AND `jamiyat` != ''
                GROUP BY `jamiyat`
                ORDER BY `jamiyat`
            ";

            var jamiyatResults = await connection.QueryAsync<(string Name, long Count)>(jamiyatSql);
            var jamiyatList = jamiyatResults.Select(x => new JamiyatItem(x.Name, (int)x.Count)).ToList();

            // Get distinct jamaat with counts
            var jamaatSql = @"
                SELECT 
                    jamaat AS Name,
                    COUNT(*) AS Count
                FROM `members`
                WHERE `is_active` = 1 
                    AND `jamaat` IS NOT NULL 
                    AND `jamaat` != ''
                GROUP BY `jamaat`
                ORDER BY `jamaat`
            ";

            var jamaatResults = await connection.QueryAsync<(string Name, long Count)>(jamaatSql);
            var jamaatList = jamaatResults.Select(x => new JamaatItem(x.Name, (int)x.Count)).ToList();

            return (jamiyatList, jamaatList);
        }
    }

    public async Task<List<string>> GetAdminEmailsAsync()
    {
        using (var connection = _context.CreateConnection())
        {
            // Get all admin emails (users with roles = 3 or rank = 'Admin')
            var sql = @"
                SELECT DISTINCT `email`
                FROM `members`
                WHERE (`roles` = 7 OR `rank` = 'Admin')
                    AND `is_active` = 1
                    AND `email` IS NOT NULL
                    AND `email` != ''
            ";

            var emails = await connection.QueryAsync<string>(sql);
            return emails.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
        }
    }

    public async Task<List<int>> GetAdminUserIdsAsync()
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT DISTINCT `id`
                FROM `members`
                WHERE (`roles` = 7 OR `rank` = 'Admin')
                    AND `is_active` = 1
            ";

            var result = await connection.QueryAsync<int>(sql);
            return result.ToList();
        }
    }

    public async Task<List<MemberModel>> GetMembersByJamiyatAsync(string jamiyat)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `profile` AS Profile,
                    `its_id` AS ItsId,
                    `rank` AS `Rank`,
                    `roles` AS Roles,
                    `jamiyat` AS Jamiyat,
                    `jamaat` AS Jamaat,
                    `jamiyat_id` AS JamiyatId,
                    `jamaat_id` AS JamaatId,
                    `full_name` AS FullName,
                    `gender` AS Gender,
                    `email` AS Email,
                    `age` AS Age,
                    `contact` AS Contact,
                    `is_active` AS IsActive,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `members`
                WHERE `jamiyat` = @Jamiyat
                    AND `is_active` = 1
                    AND `email` IS NOT NULL
                    AND `email` != ''
            ";

            var members = await connection.QueryAsync(sql, new { Jamiyat = jamiyat });
            return members.Select(row => new MemberModel
            {
                Id = (long)row.Id,
                Profile = row.Profile as string,
                ItsId = row.ItsId as string ?? string.Empty,
                Rank = row.Rank as string ?? string.Empty,
                Roles = row.Roles as int?,
                Jamiyat = row.Jamiyat as string,
                Jamaat = row.Jamaat as string,
                JamiyatId = row.JamiyatId as int?,
                JamaatId = row.JamaatId as int?,
                FullName = row.FullName as string ?? string.Empty,
                Gender = row.Gender as string,
                Email = row.Email as string ?? string.Empty,
                Age = row.Age as int?,
                Contact = row.Contact as string,
                IsActive = row.IsActive as bool? ?? true,
                CreatedAt = row.CreatedAt as DateTime? ?? DateTime.UtcNow,
                UpdatedAt = row.UpdatedAt as DateTime? ?? DateTime.UtcNow
            }).ToList();
        }
    }

    public async Task<List<MemberModel>> GetMembersByJamaatAsync(string jamaat)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `profile` AS Profile,
                    `its_id` AS ItsId,
                    `rank` AS `Rank`,
                    `roles` AS Roles,
                    `jamiyat` AS Jamiyat,
                    `jamaat` AS Jamaat,
                    `jamiyat_id` AS JamiyatId,
                    `jamaat_id` AS JamaatId,
                    `full_name` AS FullName,
                    `gender` AS Gender,
                    `email` AS Email,
                    `age` AS Age,
                    `contact` AS Contact,
                    `date_of_birth` AS DateOfBirth,
                    `is_active` AS IsActive,
                    `is_approved` AS IsApproved,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `members`
                WHERE `jamaat` = @Jamaat
                    AND `is_active` = 1
                    AND `email` IS NOT NULL
                    AND `email` != ''
                ORDER BY `full_name` ASC
            ";

            var members = await connection.QueryAsync(sql, new { Jamaat = jamaat });
            return members.Select(row => new MemberModel
            {
                Id = (long)row.Id,
                Profile = row.Profile as string,
                ItsId = row.ItsId as string ?? string.Empty,
                Rank = row.Rank as string ?? string.Empty,
                Roles = row.Roles as int?,
                Jamiyat = row.Jamiyat as string,
                Jamaat = row.Jamaat as string,
                JamiyatId = row.JamiyatId as int?,
                JamaatId = row.JamaatId as int?,
                FullName = row.FullName as string ?? string.Empty,
                Gender = row.Gender as string,
                Email = row.Email as string ?? string.Empty,
                Age = row.Age as int?,
                Contact = row.Contact as string,
                DateOfBirth = row.DateOfBirth as DateTime?,
                IsActive = row.IsActive as bool? ?? true,
                IsApproved = row.IsApproved as bool? ?? true,
                CreatedAt = row.CreatedAt as DateTime? ?? DateTime.UtcNow,
                UpdatedAt = row.UpdatedAt as DateTime? ?? DateTime.UtcNow
            }).ToList();
        }
    }

    public async Task<List<MemberModel>> GetHierarchyMembersByJamaatAsync(string jamaat)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `profile` AS Profile,
                    `its_id` AS ItsId,
                    `rank` AS `Rank`,
                    `roles` AS Roles,
                    `jamiyat` AS Jamiyat,
                    `jamaat` AS Jamaat,
                    `jamiyat_id` AS JamiyatId,
                    `jamaat_id` AS JamaatId,
                    `full_name` AS FullName,
                    `gender` AS Gender,
                    `email` AS Email,
                    `age` AS Age,
                    `contact` AS Contact,
                    `date_of_birth` AS DateOfBirth,
                    `is_active` AS IsActive,
                    `is_approved` AS IsApproved,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `members`
                WHERE (`jamaat` = @Jamaat OR `roles` IN (6, 7, 8))
                    AND `is_active` = 1
                    AND `email` IS NOT NULL
                    AND `email` != ''
                ORDER BY `full_name` ASC
            ";

            var members = await connection.QueryAsync(sql, new { Jamaat = jamaat });
            return members.Select(row => new MemberModel
            {
                Id = (long)row.Id,
                Profile = row.Profile as string,
                ItsId = row.ItsId as string ?? string.Empty,
                Rank = row.Rank as string ?? string.Empty,
                Roles = row.Roles as int?,
                Jamiyat = row.Jamiyat as string,
                Jamaat = row.Jamaat as string,
                JamiyatId = row.JamiyatId as int?,
                JamaatId = row.JamaatId as int?,
                FullName = row.FullName as string ?? string.Empty,
                Gender = row.Gender as string,
                Email = row.Email as string ?? string.Empty,
                Age = row.Age as int?,
                Contact = row.Contact as string,
                DateOfBirth = row.DateOfBirth as DateTime?,
                IsActive = row.IsActive as bool? ?? true,
                IsApproved = row.IsApproved as bool? ?? true,
                CreatedAt = row.CreatedAt as DateTime? ?? DateTime.UtcNow,
                UpdatedAt = row.UpdatedAt as DateTime? ?? DateTime.UtcNow
            }).ToList();
        }
    }

    public async Task<MemberModel?> GetCaptainByFullNameAsync(string captainName)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `profile` AS Profile,
                    `its_id` AS ItsId,
                    `rank` AS `Rank`,
                    `roles` AS Roles,
                    `jamiyat` AS Jamiyat,
                    `jamaat` AS Jamaat,
                    `jamiyat_id` AS JamiyatId,
                    `jamaat_id` AS JamaatId,
                    `full_name` AS FullName,
                    `gender` AS Gender,
                    `email` AS Email,
                    `age` AS Age,
                    `contact` AS Contact,
                    `is_active` AS IsActive,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `members`
                WHERE `full_name` = @CaptainName
                    AND `rank` = 'Captain'
                    AND `is_active` = 1
                LIMIT 1
            ";

            var row = await connection.QueryFirstOrDefaultAsync(sql, new { CaptainName = captainName });
            if (row == null)
            {
                return null;
            }

            return new MemberModel
            {
                Id = (long)row.Id,
                Profile = row.Profile as string,
                ItsId = row.ItsId as string ?? string.Empty,
                Rank = row.Rank as string ?? string.Empty,
                Roles = row.Roles as int?,
                Jamiyat = row.Jamiyat as string,
                Jamaat = row.Jamaat as string,
                JamiyatId = row.JamiyatId as int?,
                JamaatId = row.JamaatId as int?,
                FullName = row.FullName as string ?? string.Empty,
                Gender = row.Gender as string,
                Email = row.Email as string ?? string.Empty,
                Age = row.Age as int?,
                Contact = row.Contact as string,
                IsActive = row.IsActive as bool? ?? true,
                CreatedAt = row.CreatedAt as DateTime? ?? DateTime.UtcNow,
                UpdatedAt = row.UpdatedAt as DateTime? ?? DateTime.UtcNow
            };
        }
    }

    public async Task<MemberModel?> GetCaptainByJamaatAsync(string jamaat)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `profile` AS Profile,
                    `its_id` AS ItsId,
                    `rank` AS `Rank`,
                    `roles` AS Roles,
                    `jamiyat` AS Jamiyat,
                    `jamaat` AS Jamaat,
                    `jamiyat_id` AS JamiyatId,
                    `jamaat_id` AS JamaatId,
                    `full_name` AS FullName,
                    `gender` AS Gender,
                    `email` AS Email,
                    `age` AS Age,
                    `contact` AS Contact,
                    `is_active` AS IsActive,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `members`
                WHERE `jamaat` = @Jamaat
                    AND `roles` = 2
                    AND `is_active` = 1
                LIMIT 1
            ";

            var row = await connection.QueryFirstOrDefaultAsync(sql, new { Jamaat = jamaat });
            if (row == null)
            {
                return null;
            }

            return new MemberModel
            {
                Id = (long)row.Id,
                Profile = row.Profile as string,
                ItsId = row.ItsId as string ?? string.Empty,
                Rank = row.Rank as string ?? string.Empty,
                Roles = row.Roles as int?,
                Jamiyat = row.Jamiyat as string,
                Jamaat = row.Jamaat as string,
                JamiyatId = row.JamiyatId as int?,
                JamaatId = row.JamaatId as int?,
                FullName = row.FullName as string ?? string.Empty,
                Gender = row.Gender as string,
                Email = row.Email as string ?? string.Empty,
                Age = row.Age as int?,
                Contact = row.Contact as string,
                IsActive = row.IsActive as bool? ?? true,
                CreatedAt = row.CreatedAt as DateTime? ?? DateTime.UtcNow,
                UpdatedAt = row.UpdatedAt as DateTime? ?? DateTime.UtcNow
            };
        }
    }

    public async Task ApproveMember(int id)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                UPDATE `members` 
                SET `is_approved` = 1, 
                    `updated_at` = CURRENT_TIMESTAMP
                WHERE `id` = @Id AND `is_active` = 1
            ";

            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });

            if (rowsAffected == 0)
            {
                throw new Exception("Member not found or inactive");
            }
        }
    }

    public async Task<IEnumerable<int>> GetAllUserIdsAsync()
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"SELECT `id` FROM `members` WHERE `is_active` = 1";
            return await connection.QueryAsync<int>(sql);
        }
    }

    public async Task<IEnumerable<int>> GetUserIdsByJamaatAsync(string jamaat)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"SELECT `id` FROM `members` WHERE `jamaat` = @Jamaat AND `is_active` = 1";
            return await connection.QueryAsync<int>(sql, new { Jamaat = jamaat });
        }
    }

    public async Task UpdateFcmTokenAsync(int userId, string token)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"UPDATE `members` SET `fcm_token` = @Token WHERE `id` = @UserId";
            await connection.ExecuteAsync(sql, new { Token = token, UserId = userId });
        }
    }

    public async Task<string?> GetFcmTokenAsync(int userId)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"SELECT `fcm_token` FROM `members` WHERE `id` = @UserId AND `is_active` = 1";
            return await connection.QuerySingleOrDefaultAsync<string?>(sql, new { UserId = userId });
        }
    }

    public async Task<Dictionary<int, string>> GetFcmTokensAsync(IEnumerable<int> userIds)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"SELECT `id`, `fcm_token` FROM `members` 
                        WHERE `id` IN @UserIds AND `is_active` = 1 AND `fcm_token` IS NOT NULL AND `fcm_token` != ''";
            var results = await connection.QueryAsync<(int id, string fcm_token)>(sql, new { UserIds = userIds.ToList() });
            return results.ToDictionary(r => r.id, r => r.fcm_token);
        }
    }
}

