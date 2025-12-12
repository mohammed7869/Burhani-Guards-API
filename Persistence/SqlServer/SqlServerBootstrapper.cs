using BurhaniGuards.Api.Constants;
using Dapper;

namespace BurhaniGuards.Api.Persistence.SqlServer;

public sealed class SqlServerBootstrapper
{
    private readonly SqlServerContext _context;

    public SqlServerBootstrapper(SqlServerContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        // Create captains table
        await connection.ExecuteAsync("""
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[captains]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [dbo].[captains] (
                    [id] INT IDENTITY(1,1) PRIMARY KEY,
                    [its_number] NVARCHAR(50) NOT NULL UNIQUE,
                    [name] NVARCHAR(255) NOT NULL,
                    [email] NVARCHAR(255) NULL,
                    [password_hash] NVARCHAR(255) NOT NULL,
                    [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                    [updated_at] DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE INDEX IX_captains_its_number ON [dbo].[captains]([its_number]);
                CREATE INDEX IX_captains_email ON [dbo].[captains]([email]);
            END
            """);
        
        // Migration: Add new columns if they don't exist
        try
        {
            await connection.ExecuteAsync("""
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[captains]') AND name = 'its_number')
                BEGIN
                    ALTER TABLE [dbo].[captains] ADD [its_number] NVARCHAR(50) NULL;
                END
                """);
        }
        catch { } // Column might already exist
        
        try
        {
            await connection.ExecuteAsync("""
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[captains]') AND name = 'name')
                BEGIN
                    ALTER TABLE [dbo].[captains] ADD [name] NVARCHAR(255) NULL;
                END
                """);
        }
        catch { } // Column might already exist
        
        // Add unique index for its_number if it doesn't exist
        try
        {
            await connection.ExecuteAsync("""
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[captains]') AND name = 'IX_captains_its_number')
                BEGIN
                    CREATE UNIQUE INDEX IX_captains_its_number ON [dbo].[captains]([its_number]);
                END
                """);
        }
        catch { } // Index might already exist

        // Migration: Rename users table to members if users exists and members doesn't
        try
        {
            var usersExists = await connection.QueryFirstOrDefaultAsync<int?>(@"
                SELECT COUNT(*) 
                FROM sys.objects 
                WHERE object_id = OBJECT_ID(N'[dbo].[users]') AND type in (N'U')
            ");
            
            var membersExists = await connection.QueryFirstOrDefaultAsync<int?>(@"
                SELECT COUNT(*) 
                FROM sys.objects 
                WHERE object_id = OBJECT_ID(N'[dbo].[members]') AND type in (N'U')
            ");

            if (usersExists > 0 && membersExists == 0)
            {
                await connection.ExecuteAsync("EXEC sp_rename '[dbo].[users]', 'members';");
            }
        }
        catch { } // Migration might fail if tables don't exist

        // Create member_snapshots table
        await connection.ExecuteAsync("""
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[member_snapshots]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [dbo].[member_snapshots] (
                    [id] INT IDENTITY(1,1) PRIMARY KEY,
                    [email] NVARCHAR(255) NOT NULL UNIQUE,
                    [display_name] NVARCHAR(255) NULL,
                    [role] NVARCHAR(50) NULL,
                    [last_login] DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE INDEX IX_member_snapshots_email ON [dbo].[member_snapshots]([email]);
            END
            """);

        // Migration: Add jamiyat_id and jamaat_id columns to members table if they don't exist
        try
        {
            await connection.ExecuteAsync("""
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[members]') AND type in (N'U'))
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[members]') AND name = 'jamiyat_id')
                    BEGIN
                        ALTER TABLE [dbo].[members] ADD [jamiyat_id] INT NULL;
                        CREATE INDEX IX_members_jamiyat_id ON [dbo].[members]([jamiyat_id]);
                    END
                END
                """);
        }
        catch { } // Column might already exist

        try
        {
            await connection.ExecuteAsync("""
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[members]') AND type in (N'U'))
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[members]') AND name = 'jamaat_id')
                    BEGIN
                        ALTER TABLE [dbo].[members] ADD [jamaat_id] INT NULL;
                        CREATE INDEX IX_members_jamaat_id ON [dbo].[members]([jamaat_id]);
                    END
                END
                """);
        }
        catch { } // Column might already exist

        // Migration: Populate jamiyat_id and jamaat_id from existing text values
        try
        {
            await connection.ExecuteAsync("""
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[members]') AND type in (N'U'))
                BEGIN
                    UPDATE [dbo].[members] 
                    SET [jamiyat_id] = CASE 
                        WHEN [jamiyat] = 'Poona' THEN 1 
                        ELSE NULL 
                    END
                    WHERE [jamiyat_id] IS NULL AND [jamiyat] IS NOT NULL;

                    UPDATE [dbo].[members] 
                    SET [jamaat_id] = CASE 
                        WHEN [jamaat] = 'BARAMATI' THEN 1 
                        WHEN [jamaat] = 'FAKHRI MOHALLA (POONA)' THEN 2 
                        WHEN [jamaat] = 'ZAINI MOHALLA (POONA)' THEN 3 
                        WHEN [jamaat] = 'KALIMI MOHALLA (POONA)' THEN 4 
                        WHEN [jamaat] = 'AHMEDNAGAR' THEN 5 
                        WHEN [jamaat] = 'IMADI MOHALLA (POONA)' THEN 6 
                        WHEN [jamaat] = 'KASARWADI' THEN 7 
                        WHEN [jamaat] = 'KHADKI (POONA)' THEN 8 
                        WHEN [jamaat] = 'LONAVALA' THEN 9 
                        WHEN [jamaat] = 'MUFADDAL MOHALLA (POONA)' THEN 10 
                        WHEN [jamaat] = 'POONA' THEN 11 
                        WHEN [jamaat] = 'SAIFEE MOHALLAH (POONA)' THEN 12 
                        WHEN [jamaat] = 'TAIYEBI MOHALLA (POONA)' THEN 13 
                        WHEN [jamaat] = 'FATEMI MOHALLA (POONA)' THEN 14 
                        ELSE NULL 
                    END
                    WHERE [jamaat_id] IS NULL AND [jamaat] IS NOT NULL;
                END
                """);
        }
        catch { } // Migration might fail if table doesn't exist

        // Create miqaat_members table
        await connection.ExecuteAsync("""
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[miqaat_members]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [dbo].[miqaat_members] (
                    [member_id] INT NOT NULL,
                    [miqaat_id] BIGINT NOT NULL,
                    [status] NVARCHAR(50) NULL,
                    PRIMARY KEY ([member_id], [miqaat_id])
                );
                CREATE INDEX IX_miqaat_members_member_id ON [dbo].[miqaat_members]([member_id]);
                CREATE INDEX IX_miqaat_members_miqaat_id ON [dbo].[miqaat_members]([miqaat_id]);
                
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_miqaat_members_member_id')
                BEGIN
                    ALTER TABLE [dbo].[miqaat_members]
                    ADD CONSTRAINT FK_miqaat_members_member_id FOREIGN KEY ([member_id]) REFERENCES [dbo].[members]([id]) ON DELETE CASCADE;
                END
                
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_miqaat_members_miqaat_id')
                BEGIN
                    ALTER TABLE [dbo].[miqaat_members]
                    ADD CONSTRAINT FK_miqaat_members_miqaat_id FOREIGN KEY ([miqaat_id]) REFERENCES [dbo].[local_miqaat]([id]) ON DELETE CASCADE;
                END
            END
            """);

        // Seed default captain if not exists
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(CaptainDefaults.Password);
        await connection.ExecuteAsync("""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[captains] WHERE [its_number] = @ItsNumber)
            BEGIN
                INSERT INTO [dbo].[captains] ([its_number], [name], [password_hash])
                VALUES (@ItsNumber, @Name, @PasswordHash);
            END
            ELSE
            BEGIN
                UPDATE [dbo].[captains]
                SET [name] = @Name,
                    [password_hash] = @PasswordHash,
                    [updated_at] = GETDATE()
                WHERE [its_number] = @ItsNumber;
            END
            """,
            new
            {
                ItsNumber = CaptainDefaults.ItsNumber,
                Name = CaptainDefaults.Name,
                PasswordHash = hashedPassword
            });
    }
}








