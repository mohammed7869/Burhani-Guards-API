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








