using BurhaniGuards.Api.BusinessModel;
using Dapper;
using Dapper.Contrib.Extensions;

namespace BurhaniGuards.Api.Repositories;

public class MiqaatRepository : IMiqaatRepository
{
    private readonly DapperContext _context;

    public MiqaatRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<long> Add(MiqaatModel model)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                INSERT INTO `local_miqaat` 
                (`miqaat_name`, `jamaat`, `jamiyat`, `from_date`, `till_date`, `volunteer_limit`, `about_miqaat`, `admin_approval`, `captain_name`, `created_at`, `updated_at`)
                VALUES 
                (@MiqaatName, @Jamaat, @Jamiyat, @FromDate, @TillDate, @VolunteerLimit, @AboutMiqaat, @AdminApproval, @CaptainName, @CreatedAt, @UpdatedAt);
                SELECT LAST_INSERT_ID();";

            var id = await connection.QuerySingleAsync<long>(sql, new
            {
                model.MiqaatName,
                model.Jamaat,
                model.Jamiyat,
                FromDate = model.FromDate.Date,
                TillDate = model.TillDate.Date,
                model.VolunteerLimit,
                model.AboutMiqaat,
                model.AdminApproval,
                model.CaptainName,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            });

            return id;
        }
    }

    public async Task<List<MiqaatModel>> GetAll()
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `miqaat_name` AS MiqaatName,
                    `jamaat` AS Jamaat,
                    `jamiyat` AS Jamiyat,
                    `from_date` AS FromDate,
                    `till_date` AS TillDate,
                    `volunteer_limit` AS VolunteerLimit,
                    `about_miqaat` AS AboutMiqaat,
                    `admin_approval` AS AdminApproval,
                    `captain_name` AS CaptainName,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `local_miqaat`
                ORDER BY `created_at` DESC";

            var miqaats = await connection.QueryAsync<MiqaatModel>(sql);
            return miqaats.ToList();
        }
    }

    public async Task<MiqaatModel?> GetById(long id)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `miqaat_name` AS MiqaatName,
                    `jamaat` AS Jamaat,
                    `jamiyat` AS Jamiyat,
                    `from_date` AS FromDate,
                    `till_date` AS TillDate,
                    `volunteer_limit` AS VolunteerLimit,
                    `about_miqaat` AS AboutMiqaat,
                    `admin_approval` AS AdminApproval,
                    `captain_name` AS CaptainName,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `local_miqaat`
                WHERE `id` = @Id";

            var miqaat = await connection.QueryFirstOrDefaultAsync<MiqaatModel>(sql, new { Id = id });
            return miqaat;
        }
    }

    public async Task Update(MiqaatModel model)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                UPDATE `local_miqaat`
                SET 
                    `miqaat_name` = @MiqaatName,
                    `jamaat` = @Jamaat,
                    `jamiyat` = @Jamiyat,
                    `from_date` = @FromDate,
                    `till_date` = @TillDate,
                    `volunteer_limit` = @VolunteerLimit,
                    `about_miqaat` = @AboutMiqaat,
                    `admin_approval` = @AdminApproval,
                    `captain_name` = @CaptainName,
                    `updated_at` = @UpdatedAt
                WHERE `id` = @Id";

            await connection.ExecuteAsync(sql, new
            {
                model.Id,
                model.MiqaatName,
                model.Jamaat,
                model.Jamiyat,
                FromDate = model.FromDate.Date,
                TillDate = model.TillDate.Date,
                model.VolunteerLimit,
                model.AboutMiqaat,
                model.AdminApproval,
                model.CaptainName,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    public async Task Delete(long id)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"DELETE FROM `local_miqaat` WHERE `id` = @Id";
            await connection.ExecuteAsync(sql, new { Id = id });
        }
    }
}

