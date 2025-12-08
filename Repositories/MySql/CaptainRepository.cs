using BurhaniGuards.Api.Domain;
using BurhaniGuards.Api.Persistence.MySql;
using BurhaniGuards.Api.Repositories.Interfaces;
using Dapper;

namespace BurhaniGuards.Api.Repositories.MySql;

public sealed class CaptainRepository : ICaptainRepository
{
    private readonly MySqlContext _context;

    public CaptainRepository(MySqlContext context)
    {
        _context = context;
    }

    public async Task<CaptainCredential?> GetByItsNumberAsync(string itsNumber, CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        const string query = "SELECT `its_number` AS ItsNumber, `name` AS Name, `email` AS Email, `password_hash` AS PasswordHash, `new_password_hash` AS NewPasswordHash FROM `captains` WHERE `its_number` = @ItsNumber;";
        return await connection.QueryFirstOrDefaultAsync<CaptainCredential>(new CommandDefinition(
            commandText: query,
            parameters: new { ItsNumber = itsNumber },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> CreateAsync(string itsNumber, string name, string passwordHash, CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        const string query = """
            INSERT INTO `captains` (`its_number`, `name`, `password_hash`)
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
        const string query = "SELECT COUNT(1) FROM `captains` WHERE `its_number` = @ItsNumber;";
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
            UPDATE `captains` 
            SET `new_password_hash` = @NewPasswordHash, `updated_at` = CURRENT_TIMESTAMP
            WHERE `its_number` = @ItsNumber;
            """;
        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            commandText: query,
            parameters: new { ItsNumber = itsNumber, NewPasswordHash = newPasswordHash },
            cancellationToken: cancellationToken));
        return rowsAffected > 0;
    }
}



