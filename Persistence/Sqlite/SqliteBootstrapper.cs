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

        // Migration: Rename users table to members if users exists and members doesn't
        try
        {
            var usersExists = await connection.QueryFirstOrDefaultAsync<int?>(@"
                SELECT COUNT(*) 
                FROM sqlite_master 
                WHERE type='table' AND name='users'
            ");
            
            var membersExists = await connection.QueryFirstOrDefaultAsync<int?>(@"
                SELECT COUNT(*) 
                FROM sqlite_master 
                WHERE type='table' AND name='members'
            ");

            if (usersExists > 0 && membersExists == 0)
            {
                await connection.ExecuteAsync("ALTER TABLE users RENAME TO members;");
            }
        }
        catch { } // Migration might fail if tables don't exist

        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS member_snapshots (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                email TEXT NOT NULL UNIQUE,
                display_name TEXT,
                role TEXT,
                last_login TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);

        // Migration: Add jamiyat_id and jamaat_id columns to members table if they don't exist
        try
        {
            await connection.ExecuteAsync("""
                ALTER TABLE members ADD COLUMN jamiyat_id INTEGER NULL;
                """);
        }
        catch { } // Column might already exist

        try
        {
            await connection.ExecuteAsync("""
                ALTER TABLE members ADD COLUMN jamaat_id INTEGER NULL;
                """);
        }
        catch { } // Column might already exist

        // Create indexes for new columns if they don't exist
        try
        {
            await connection.ExecuteAsync("""
                CREATE INDEX IF NOT EXISTS IX_members_jamiyat_id ON members(jamiyat_id);
                """);
        }
        catch { } // Index might already exist

        try
        {
            await connection.ExecuteAsync("""
                CREATE INDEX IF NOT EXISTS IX_members_jamaat_id ON members(jamaat_id);
                """);
        }
        catch { } // Index might already exist

        // Migration: Populate jamiyat_id and jamaat_id from existing text values
        try
        {
            await connection.ExecuteAsync("""
                UPDATE members 
                SET jamiyat_id = CASE 
                    WHEN jamiyat = 'Poona' THEN 1 
                    ELSE NULL 
                END
                WHERE jamiyat_id IS NULL AND jamiyat IS NOT NULL;
                """);

            await connection.ExecuteAsync("""
                UPDATE members 
                SET jamaat_id = CASE 
                    WHEN jamaat = 'BARAMATI' THEN 1 
                    WHEN jamaat = 'FAKHRI MOHALLA (POONA)' THEN 2 
                    WHEN jamaat = 'ZAINI MOHALLA (POONA)' THEN 3 
                    WHEN jamaat = 'KALIMI MOHALLA (POONA)' THEN 4 
                    WHEN jamaat = 'AHMEDNAGAR' THEN 5 
                    WHEN jamaat = 'IMADI MOHALLA (POONA)' THEN 6 
                    WHEN jamaat = 'KASARWADI' THEN 7 
                    WHEN jamaat = 'KHADKI (POONA)' THEN 8 
                    WHEN jamaat = 'LONAVALA' THEN 9 
                    WHEN jamaat = 'MUFADDAL MOHALLA (POONA)' THEN 10 
                    WHEN jamaat = 'POONA' THEN 11 
                    WHEN jamaat = 'SAIFEE MOHALLAH (POONA)' THEN 12 
                    WHEN jamaat = 'TAIYEBI MOHALLA (POONA)' THEN 13 
                    WHEN jamaat = 'FATEMI MOHALLA (POONA)' THEN 14 
                    ELSE NULL 
                END
                WHERE jamaat_id IS NULL AND jamaat IS NOT NULL;
                """);
        }
        catch { } // Migration might fail if table doesn't exist

        // Create miqaat_members table
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS miqaat_members (
                member_id INTEGER NOT NULL,
                miqaat_id INTEGER NOT NULL,
                status TEXT,
                PRIMARY KEY (member_id, miqaat_id),
                FOREIGN KEY (member_id) REFERENCES members(id) ON DELETE CASCADE,
                FOREIGN KEY (miqaat_id) REFERENCES local_miqaat(id) ON DELETE CASCADE
            );
            """);
        
        // Create indexes for miqaat_members
        await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS IX_miqaat_members_member_id ON miqaat_members(member_id);
            """);
        
        await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS IX_miqaat_members_miqaat_id ON miqaat_members(miqaat_id);
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

