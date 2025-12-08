using BurhaniGuards.Api.Domain;
using BurhaniGuards.Api.Persistence.SqlServer;
using BurhaniGuards.Api.Repositories.Interfaces;
using Dapper;

namespace BurhaniGuards.Api.Repositories.SqlServer;

public sealed class CaptainRepository : ICaptainRepository
{
    private readonly SqlServerContext _context;

    public CaptainRepository(SqlServerContext context)
    {
        _context = context;
    }

    public async Task<CaptainCredential?> GetByItsNumberAsync(string itsNumber, CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        const string query = "SELECT [its_number] AS ItsNumber, [name] AS Name, [email] AS Email, [password_hash] AS PasswordHash, [new_password_hash] AS NewPasswordHash FROM [dbo].[captains] WHERE [its_number] = @ItsNumber;";
        return await connection.QueryFirstOrDefaultAsync<CaptainCredential>(new CommandDefinition(
            commandText: query,
            parameters: new { ItsNumber = itsNumber },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> CreateAsync(string itsNumber, string name, string passwordHash, CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        const string query = """
            INSERT INTO [dbo].[captains] ([its_number], [name], [password_hash])
            VALUES (@ItsNumber, @Name, @PasswordHash);
            """;
        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            commandText: query,
            parameters: new { ItsNumber = itsNumber, Name = name, PasswordHash = passwordHash },
            cancellationToken: cancellationToken));
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(string itsNumber, CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        const string query = "SELECT COUNT(1) FROM [dbo].[captains] WHERE [its_number] = @ItsNumber;";
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            commandText: query,
            parameters: new { ItsNumber = itsNumber },
            cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task<bool> UpdateNewPasswordAsync(string itsNumber, string newPasswordHash, CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        const string query = """
            UPDATE [dbo].[captains] 
            SET [new_password_hash] = @NewPasswordHash, [updated_at] = GETDATE()
            WHERE [its_number] = @ItsNumber;
            """;
        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            commandText: query,
            parameters: new { ItsNumber = itsNumber, NewPasswordHash = newPasswordHash },
            cancellationToken: cancellationToken));
        return rowsAffected > 0;
    }
}




