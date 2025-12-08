using BurhaniGuards.Api.Persistence.Sqlite;
using BurhaniGuards.Api.Repositories.Interfaces;
using Dapper;

namespace BurhaniGuards.Api.Repositories.Sqlite;

public sealed class MemberSnapshotRepository : IMemberSnapshotRepository
{
    private readonly DapperContext _context;

    public MemberSnapshotRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task UpsertAsync(string email, string? displayName, string role, CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        const string sql = """
            INSERT INTO member_snapshots (email, display_name, role, last_login)
            VALUES (@Email, @DisplayName, @Role, CURRENT_TIMESTAMP)
            ON CONFLICT(email) DO UPDATE SET
                display_name = excluded.display_name,
                role = excluded.role,
                last_login = CURRENT_TIMESTAMP;
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

