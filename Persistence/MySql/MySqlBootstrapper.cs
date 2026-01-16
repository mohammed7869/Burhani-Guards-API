using BurhaniGuards.Api.Constants;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace BurhaniGuards.Api.Persistence.MySql;

public sealed class MySqlBootstrapper
{
    private readonly MySqlContext _context;
    private readonly string _connectionString;

    public MySqlBootstrapper(MySqlContext context, IConfiguration configuration)
    {
        _context = context;
        var connectionString = configuration.GetConnectionString("MySql") 
            ?? configuration.GetSection("MySql:ConnectionString").Value 
            ?? throw new InvalidOperationException("MySQL connection string is required");
        _connectionString = connectionString;
    }

    private string GetConnectionStringWithoutDatabase()
    {
        // Parse connection string and remove Database parameter
        var builder = new MySqlConnectionStringBuilder(_connectionString);
        var databaseName = builder.Database;
        builder.Database = null; // Remove database to connect to server
        return builder.ConnectionString;
    }

    private string GetDatabaseName()
    {
        var builder = new MySqlConnectionStringBuilder(_connectionString);
        return builder.Database ?? throw new InvalidOperationException("Database name is required in connection string");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var databaseName = GetDatabaseName();
        
        // First, connect without database to create it if needed
        var connectionStringWithoutDb = GetConnectionStringWithoutDatabase();
        using (var connection = new MySqlConnection(connectionStringWithoutDb))
        {
            connection.Open();
            await connection.ExecuteAsync($"""
                CREATE DATABASE IF NOT EXISTS `{databaseName}` 
                CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
                """);
        }

        // Now connect to the actual database
        using var dbConnection = _context.CreateConnection();
        dbConnection.Open();

        // Create captains table
        await dbConnection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS `captains` (
                `id` INT AUTO_INCREMENT PRIMARY KEY,
                `its_number` VARCHAR(50) NOT NULL UNIQUE,
                `name` VARCHAR(255) NOT NULL,
                `email` VARCHAR(255) NULL,
                `password_hash` VARCHAR(255) NOT NULL,
                `new_password_hash` VARCHAR(255) NULL,
                `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                INDEX `IX_captains_its_number` (`its_number`),
                INDEX `IX_captains_email` (`email`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """);
        
        // Migration: Add new columns if they don't exist
        try
        {
            await dbConnection.ExecuteAsync("""
                ALTER TABLE `captains`
                ADD COLUMN `its_number` VARCHAR(50) NULL AFTER `id`;
                """);
        }
        catch { } // Column might already exist
        
        try
        {
            await dbConnection.ExecuteAsync("""
                ALTER TABLE `captains`
                ADD COLUMN `name` VARCHAR(255) NULL AFTER `its_number`;
                """);
        }
        catch { } // Column might already exist
        
        // Add unique index for its_number if it doesn't exist
        try
        {
            await dbConnection.ExecuteAsync("""
                CREATE UNIQUE INDEX `IX_captains_its_number` ON `captains` (`its_number`);
                """);
        }
        catch { } // Index might already exist
        
        // Migration: Add new_password_hash column if it doesn't exist
        try
        {
            await dbConnection.ExecuteAsync("""
                ALTER TABLE `captains`
                ADD COLUMN `new_password_hash` VARCHAR(255) NULL AFTER `password_hash`;
                """);
        }
        catch { } // Column might already exist
        
        // Migration: Add roles column to members table if it doesn't exist
        try
        {
            await dbConnection.ExecuteAsync("""
                ALTER TABLE `members`
                ADD COLUMN `roles` INT NULL AFTER `rank`;
                """);
        }
        catch { } // Column might already exist

        // Migration: Rename users table to members if users exists and members doesn't
        try
        {
            var usersExists = await dbConnection.QueryFirstOrDefaultAsync<int?>(@"
                SELECT COUNT(*) 
                FROM information_schema.tables 
                WHERE table_schema = DATABASE() 
                AND table_name = 'users'
            ");
            
            var membersExists = await dbConnection.QueryFirstOrDefaultAsync<int?>(@"
                SELECT COUNT(*) 
                FROM information_schema.tables 
                WHERE table_schema = DATABASE() 
                AND table_name = 'members'
            ");

            if (usersExists > 0 && membersExists == 0)
            {
                await dbConnection.ExecuteAsync("RENAME TABLE `users` TO `members`;");
            }
        }
        catch { } // Migration might fail if tables don't exist

        // Migration: Add jamiyat_id and jamaat_id columns if they don't exist
        try
        {
            await dbConnection.ExecuteAsync("""
                ALTER TABLE `members`
                ADD COLUMN `jamiyat_id` INT NULL AFTER `jamaat`;
                """);
        }
        catch { } // Column might already exist

        try
        {
            await dbConnection.ExecuteAsync("""
                ALTER TABLE `members`
                ADD COLUMN `jamaat_id` INT NULL AFTER `jamiyat_id`;
                """);
        }
        catch { } // Column might already exist

        // Create indexes for new columns if they don't exist
        try
        {
            await dbConnection.ExecuteAsync("""
                CREATE INDEX `IX_members_jamiyat_id` ON `members` (`jamiyat_id`);
                """);
        }
        catch { } // Index might already exist

        try
        {
            await dbConnection.ExecuteAsync("""
                CREATE INDEX `IX_members_jamaat_id` ON `members` (`jamaat_id`);
                """);
        }
        catch { } // Index might already exist

        // Migration: Populate jamiyat_id and jamaat_id from existing text values
        try
        {
            await dbConnection.ExecuteAsync("""
                UPDATE `members` 
                SET `jamiyat_id` = CASE 
                    WHEN `jamiyat` = 'Poona' THEN 1 
                    ELSE NULL 
                END
                WHERE `jamiyat_id` IS NULL AND `jamiyat` IS NOT NULL;
                """);

            await dbConnection.ExecuteAsync("""
                UPDATE `members` 
                SET `jamaat_id` = CASE 
                    WHEN `jamaat` = 'BARAMATI' THEN 1 
                    WHEN `jamaat` = 'FAKHRI MOHALLA (POONA)' THEN 2 
                    WHEN `jamaat` = 'ZAINI MOHALLA (POONA)' THEN 3 
                    WHEN `jamaat` = 'KALIMI MOHALLA (POONA)' THEN 4 
                    WHEN `jamaat` = 'AHMEDNAGAR' THEN 5 
                    WHEN `jamaat` = 'IMADI MOHALLA (POONA)' THEN 6 
                    WHEN `jamaat` = 'KASARWADI' THEN 7 
                    WHEN `jamaat` = 'KHADKI (POONA)' THEN 8 
                    WHEN `jamaat` = 'LONAVALA' THEN 9 
                    WHEN `jamaat` = 'MUFADDAL MOHALLA (POONA)' THEN 10 
                    WHEN `jamaat` = 'POONA' THEN 11 
                    WHEN `jamaat` = 'SAIFEE MOHALLAH (POONA)' THEN 12 
                    WHEN `jamaat` = 'TAIYEBI MOHALLA (POONA)' THEN 13 
                    WHEN `jamaat` = 'FATEMI MOHALLA (POONA)' THEN 14 
                    ELSE NULL 
                END
                WHERE `jamaat_id` IS NULL AND `jamaat` IS NOT NULL;
                """);
        }
        catch { } // Migration might fail if columns don't exist yet

        // Create member_snapshots table
        await dbConnection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS `member_snapshots` (
                `id` INT AUTO_INCREMENT PRIMARY KEY,
                `email` VARCHAR(255) NOT NULL UNIQUE,
                `display_name` VARCHAR(255) NULL,
                `role` VARCHAR(50) NULL,
                `last_login` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                INDEX `IX_member_snapshots_email` (`email`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """);

        // Create miqaat_members table
        await dbConnection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS `miqaat_members` (
                `member_id` INT NOT NULL,
                `miqaat_id` BIGINT NOT NULL,
                `miqaat_day` INT NOT NULL DEFAULT 1,
                `status` VARCHAR(50) NULL,
                `final_status` VARCHAR(50) NULL,
                `is_attended` TINYINT(1) NOT NULL DEFAULT 0,
                PRIMARY KEY (`member_id`, `miqaat_id`, `miqaat_day`),
                INDEX `IX_miqaat_members_member_id` (`member_id`),
                INDEX `IX_miqaat_members_miqaat_id` (`miqaat_id`),
                INDEX `IX_miqaat_members_miqaat_id_day` (`miqaat_id`, `miqaat_day`),
                CONSTRAINT `FK_miqaat_members_member_id` FOREIGN KEY (`member_id`) REFERENCES `members` (`id`) ON DELETE CASCADE,
                CONSTRAINT `FK_miqaat_members_miqaat_id` FOREIGN KEY (`miqaat_id`) REFERENCES `local_miqaat` (`id`) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """);
        
        // Add final_status column if it doesn't exist (for existing databases)
        try
        {
            await dbConnection.ExecuteAsync("""
                ALTER TABLE `miqaat_members` 
                ADD COLUMN `final_status` VARCHAR(50) NULL;
                """);
        }
        catch { } // Column might already exist

        // Migration: Add miqaat_day column if it doesn't exist
        try
        {
            await dbConnection.ExecuteAsync("""
                ALTER TABLE `miqaat_members`
                ADD COLUMN `miqaat_day` INT NOT NULL DEFAULT 1 AFTER `miqaat_id`;
                """);
        }
        catch { } // Column might already exist

        // Migration: Add is_attended column if it doesn't exist
        try
        {
            await dbConnection.ExecuteAsync("""
                ALTER TABLE `miqaat_members`
                ADD COLUMN `is_attended` TINYINT(1) NOT NULL DEFAULT 0;
                """);
        }
        catch { } // Column might already exist

        // Note: Changing primary keys on existing DBs is intentionally not automated here.
        // Use MIGRATION_ADD_MIQaat_MEMBER_DAY.sql to update the primary key to (member_id, miqaat_id, miqaat_day).

        // Migration: Alter created_by column from INT to VARCHAR to store captain's name
        try
        {
            // Check if created_by column exists and is INT type
            var columnInfo = await dbConnection.QueryFirstOrDefaultAsync<dynamic>("""
                SELECT DATA_TYPE, COLUMN_TYPE 
                FROM information_schema.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'members' 
                AND COLUMN_NAME = 'created_by'
                """);
            
            if (columnInfo != null)
            {
                var dataType = columnInfo.DATA_TYPE?.ToString() ?? "";
                // If it's an integer type, alter it to VARCHAR
                if (dataType.ToUpper().Contains("INT"))
                {
                    await dbConnection.ExecuteAsync("""
                        ALTER TABLE `members` 
                        MODIFY COLUMN `created_by` VARCHAR(255) NULL;
                        """);
                }
            }
        }
        catch { } // Migration might fail, column might not exist or already be correct type

        // Seed default captain if not exists
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(CaptainDefaults.Password);
        await dbConnection.ExecuteAsync("""
            INSERT INTO `captains` (`its_number`, `name`, `password_hash`)
            VALUES (@ItsNumber, @Name, @PasswordHash)
            ON DUPLICATE KEY UPDATE
                `name` = @Name,
                `password_hash` = @PasswordHash,
                `updated_at` = CURRENT_TIMESTAMP;
            """,
            new
            {
                ItsNumber = CaptainDefaults.ItsNumber,
                Name = CaptainDefaults.Name,
                PasswordHash = hashedPassword
            });
    }
}



