using BurhaniGuards.Api.Persistence.SqlServer;
using BurhaniGuards.Api.Repositories.Interfaces;
using Dapper;

namespace BurhaniGuards.Api.Repositories.SqlServer;

public sealed class MemberSnapshotRepository : IMemberSnapshotRepository
{
    private readonly SqlServerContext _context;

    public MemberSnapshotRepository(SqlServerContext context)
    {
        _context = context;
    }

    public async Task UpsertAsync(string email, string? displayName, string role, CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        const string sql = """
            MERGE [dbo].[member_snapshots] AS target
            USING (SELECT @Email AS email) AS source
            ON target.[email] = source.[email]
            WHEN MATCHED THEN
                UPDATE SET
                    [display_name] = @DisplayName,
                    [role] = @Role,
                    [last_login] = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT ([email], [display_name], [role], [last_login])
                VALUES (@Email, @DisplayName, @Role, GETDATE());
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








