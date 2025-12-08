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

        // Create members table
        await dbConnection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS `members` (
                `id` INT AUTO_INCREMENT PRIMARY KEY,
                `profile` VARCHAR(255) NULL,
                `its_id` VARCHAR(50) NOT NULL UNIQUE,
                `rank` VARCHAR(50) NOT NULL DEFAULT 'Member',
                `roles` INT NULL,
                `jamiyat` VARCHAR(255) NULL,
                `jamaat` VARCHAR(255) NULL,
                `full_name` VARCHAR(255) NOT NULL,
                `gender` VARCHAR(20) NULL,
                `email` VARCHAR(255) NOT NULL UNIQUE,
                `age` INT NULL,
                `contact` VARCHAR(50) NULL,
                `password_hash` VARCHAR(255) NULL,
                `new_password_hash` VARCHAR(255) NULL,
                `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
                `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                INDEX `IX_members_its_id` (`its_id`),
                INDEX `IX_members_email` (`email`),
                INDEX `IX_members_jamiyat` (`jamiyat`),
                INDEX `IX_members_jamaat` (`jamaat`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """);

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



