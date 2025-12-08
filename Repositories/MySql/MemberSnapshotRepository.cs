using BurhaniGuards.Api.Persistence.MySql;
using BurhaniGuards.Api.Repositories.Interfaces;
using Dapper;

namespace BurhaniGuards.Api.Repositories.MySql;

public sealed class MemberSnapshotRepository : IMemberSnapshotRepository
{
    private readonly MySqlContext _context;

    public MemberSnapshotRepository(MySqlContext context)
    {
        _context = context;
    }

    public async Task UpsertAsync(string email, string? displayName, string role, CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        const string sql = """
            INSERT INTO `member_snapshots` (`email`, `display_name`, `role`, `last_login`)
            VALUES (@Email, @DisplayName, @Role, NOW())
            ON DUPLICATE KEY UPDATE
                `display_name` = @DisplayName,
                `role` = @Role,
                `last_login` = NOW();
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            commandText: sql,
            parameters: new
            {
                Email = email,
                DisplayName = displayName,
                Role = role
            },
            cancellationToken: cancellationToken));
    }
}



