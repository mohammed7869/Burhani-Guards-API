using BurhaniGuards.Api.Constants;
using Dapper;

namespace BurhaniGuards.Api.Persistence.Sqlite;

public sealed class SqliteBootstrapper
{
    private readonly DapperContext _context;

    public SqliteBootstrapper(DapperContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS captains (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                its_number TEXT NOT NULL UNIQUE,
                name TEXT NOT NULL,
                email TEXT NULL,
                password_hash TEXT NOT NULL,
                updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);
        
        // Migration: Add new columns if they don't exist
        try
        {
            await connection.ExecuteAsync("""
                ALTER TABLE captains ADD COLUMN its_number TEXT NULL;
                """);
        }
        catch { } // Column might already exist
        
        try
        {
            await connection.ExecuteAsync("""
                ALTER TABLE captains ADD COLUMN name TEXT NULL;
                """);
        }
        catch { } // Column might already exist
        
        // Add unique index for its_number if it doesn't exist
        try
        {
            await connection.ExecuteAsync("""
                CREATE UNIQUE INDEX IF NOT EXISTS IX_captains_its_number ON captains(its_number);
                """);
        }
        catch { } // Index might already exist

        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS member_snapshots (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                email TEXT NOT NULL UNIQUE,
                display_name TEXT,
                role TEXT,
                last_login TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(CaptainDefaults.Password);
        await connection.ExecuteAsync("""
            INSERT INTO captains (its_number, name, password_hash)
            VALUES (@ItsNumber, @Name, @PasswordHash)
            ON CONFLICT(its_number) DO UPDATE SET 
                name = excluded.name,
                password_hash = excluded.password_hash,
                updated_at = CURRENT_TIMESTAMP;
            """,
            new
            {
                ItsNumber = CaptainDefaults.ItsNumber,
                Name = CaptainDefaults.Name,
                PasswordHash = hashedPassword
            });
    }
}

