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
                (`miqaat_name`, `miqaat_type`, `jamaat`, `jamiyat`, `from_date`, `till_date`, `miqaat_days`, `volunteer_limit`, `about_miqaat`, `notification_image`, `admin_approval`, `captain_name`, `is_admin_created`, `created_at`, `updated_at`)
                VALUES 
                (@MiqaatName, @MiqaatType, @Jamaat, @Jamiyat, @FromDate, @TillDate, @MiqaatDays, @VolunteerLimit, @AboutMiqaat, @NotificationImage, @AdminApproval, @CaptainName, @IsAdminCreated, @CreatedAt, @UpdatedAt);
                SELECT LAST_INSERT_ID();";

            var id = await connection.QuerySingleAsync<long>(sql, new
            {
                model.MiqaatName,
                model.MiqaatType,
                model.Jamaat,
                model.Jamiyat,
                FromDate = model.FromDate.Date,
                TillDate = model.TillDate.Date,
                model.MiqaatDays,
                model.VolunteerLimit,
                model.AboutMiqaat,
                model.NotificationImage,
                AdminApproval = model.AdminApproval.ToString(),
                model.CaptainName,
                model.IsAdminCreated,
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
                    `miqaat_type` AS MiqaatType,
                    `jamaat` AS Jamaat,
                    `jamiyat` AS Jamiyat,
                    `from_date` AS FromDate,
                    `till_date` AS TillDate,
                    IFNULL(`miqaat_days`, DATEDIFF(`till_date`, `from_date`) + 1) AS MiqaatDays,
                    `volunteer_limit` AS VolunteerLimit,
                    `about_miqaat` AS AboutMiqaat,
                    `notification_image` AS NotificationImage,
                    `admin_approval` AS AdminApproval,
                    `captain_name` AS CaptainName,
                    `miqaat_image_1` AS MiqaatImage1,
                    `miqaat_image_2` AS MiqaatImage2,
                    `notes` AS Notes,
                    `khidmat_done` AS KhidmatDone,
                    `is_report_submitted` AS IsReportSubmitted,
                    `is_admin_created` AS IsAdminCreated,
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
                    `miqaat_type` AS MiqaatType,
                    `jamaat` AS Jamaat,
                    `jamiyat` AS Jamiyat,
                    `from_date` AS FromDate,
                    `till_date` AS TillDate,
                    IFNULL(`miqaat_days`, DATEDIFF(`till_date`, `from_date`) + 1) AS MiqaatDays,
                    `volunteer_limit` AS VolunteerLimit,
                    `about_miqaat` AS AboutMiqaat,
                    `notification_image` AS NotificationImage,
                    `admin_approval` AS AdminApproval,
                    `captain_name` AS CaptainName,
                    `miqaat_image_1` AS MiqaatImage1,
                    `miqaat_image_2` AS MiqaatImage2,
                    `notes` AS Notes,
                    `khidmat_done` AS KhidmatDone,
                    `is_report_submitted` AS IsReportSubmitted,
                    `is_admin_created` AS IsAdminCreated,
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
                    `miqaat_type` = @MiqaatType,
                    `jamaat` = @Jamaat,
                    `jamiyat` = @Jamiyat,
                    `from_date` = @FromDate,
                    `till_date` = @TillDate,
                    `miqaat_days` = @MiqaatDays,
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
                model.MiqaatType,
                model.Jamaat,
                model.Jamiyat,
                FromDate = model.FromDate.Date,
                TillDate = model.TillDate.Date,
                model.MiqaatDays,
                model.VolunteerLimit,
                model.AboutMiqaat,
                AdminApproval = model.AdminApproval.ToString(),
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

    public async Task<List<MiqaatModel>> GetByCaptainName(string captainName)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `miqaat_name` AS MiqaatName,
                    `miqaat_type` AS MiqaatType,
                    `jamaat` AS Jamaat,
                    `jamiyat` AS Jamiyat,
                    `from_date` AS FromDate,
                    `till_date` AS TillDate,
                    IFNULL(`miqaat_days`, DATEDIFF(`till_date`, `from_date`) + 1) AS MiqaatDays,
                    `volunteer_limit` AS VolunteerLimit,
                    `about_miqaat` AS AboutMiqaat,
                    `notification_image` AS NotificationImage,
                    `admin_approval` AS AdminApproval,
                    `captain_name` AS CaptainName,
                    `miqaat_image_1` AS MiqaatImage1,
                    `miqaat_image_2` AS MiqaatImage2,
                    `notes` AS Notes,
                    `khidmat_done` AS KhidmatDone,
                    `is_report_submitted` AS IsReportSubmitted,
                    `is_admin_created` AS IsAdminCreated,
                    `created_at` AS CreatedAt,
                    `updated_at` AS UpdatedAt
                FROM `local_miqaat`
                WHERE `captain_name` = @CaptainName
                ORDER BY `created_at` DESC";

            var miqaats = await connection.QueryAsync<MiqaatModel>(sql, new { CaptainName = captainName });
            return miqaats.ToList();
        }
    }

    public async Task UpdateMiqaatReport(long miqaatId, string? image1, string? image2, string? notes, string? khidmatDone)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                UPDATE `local_miqaat`
                SET 
                    `miqaat_image_1` = @Image1,
                    `miqaat_image_2` = @Image2,
                    `notes` = @Notes,
                    `khidmat_done` = @KhidmatDone,
                    `is_report_submitted` = 1,
                    `updated_at` = @UpdatedAt
                WHERE `id` = @Id";

            await connection.ExecuteAsync(sql, new
            {
                Id = miqaatId,
                Image1 = image1,
                Image2 = image2,
                Notes = notes,
                KhidmatDone = khidmatDone,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
}

