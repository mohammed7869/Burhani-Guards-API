using BurhaniGuards.Api.Domain;
using BurhaniGuards.Api.Persistence.MySql;
using BurhaniGuards.Api.Repositories.Interfaces;
using Dapper;
using System.Data;

namespace BurhaniGuards.Api.Repositories.MySql;

public sealed class MemberRepository : IMemberRepository
{
    private readonly MySqlContext _context;

    public MemberRepository(MySqlContext context)
    {
        _context = context;
    }

    public async Task<MemberDocument?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // This is for backward compatibility with MongoDB interface
        // Search by email in MySQL members table
        var member = await GetByEmailInternalAsync(email, cancellationToken);
        if (member is null) return null;

        return new MemberDocument
        {
            Email = member.Email,
            FullName = member.FullName,
            PasswordHash = member.NewPasswordHash ?? member.PasswordHash ?? string.Empty,
            Role = member.Rank.ToLowerInvariant(),
            IsActive = member.IsActive
        };
    }

    private async Task<Member?> GetByEmailInternalAsync(string email, CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        const string query = """
            SELECT 
                `id` AS Id, 
                `profile` AS Profile, 
                `its_id` AS ItsId, 
                `rank` AS `Rank`, 
                `roles` AS Roles, 
                `jamiyat` AS Jamiyat, 
                `jamaat` AS Jamaat, 
                `full_name` AS FullName, 
                `gender` AS Gender, 
                `email` AS Email, 
                `age` AS Age, 
                `contact` AS Contact, 
                `password_hash` AS PasswordHash, 
                `new_password_hash` AS NewPasswordHash, 
                `is_active` AS IsActive, 
                `created_at` AS CreatedAt, 
                `updated_at` AS UpdatedAt
            FROM `members`
            WHERE `email` = @Email
            LIMIT 1;
            """;

        var result = await connection.QueryFirstOrDefaultAsync<Member>(
            new CommandDefinition(
                commandText: query,
                parameters: new { Email = email.ToLowerInvariant() },
                cancellationToken: cancellationToken
            )
        );

        return result;
    }

    public async Task<Member?> GetByItsIdAsync(string itsId, CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        const string query = """
            SELECT 
                `id` AS Id, 
                `profile` AS Profile, 
                `its_id` AS ItsId, 
                `rank` AS `Rank`, 
                `roles` AS Roles, 
                `jamiyat` AS Jamiyat, 
                `jamaat` AS Jamaat, 
                `full_name` AS FullName, 
                `gender` AS Gender, 
                `email` AS Email, 
                `age` AS Age, 
                `contact` AS Contact, 
                `password_hash` AS PasswordHash, 
                `new_password_hash` AS NewPasswordHash, 
                `is_active` AS IsActive, 
                `created_at` AS CreatedAt, 
                `updated_at` AS UpdatedAt
            FROM `members`
            WHERE `its_id` = @ItsId
            LIMIT 1;
            """;

        var result = await connection.QueryFirstOrDefaultAsync<Member>(
            new CommandDefinition(
                commandText: query,
                parameters: new { ItsId = itsId },
                cancellationToken: cancellationToken
            )
        );

        return result;
    }

    public async Task<bool> UpdateNewPasswordAsync(string itsId, string newPasswordHash, CancellationToken cancellationToken = default)
    {
        using var connection = _context.CreateConnection();
        const string query = """
            UPDATE `members` 
            SET `new_password_hash` = @NewPasswordHash, `updated_at` = CURRENT_TIMESTAMP
            WHERE `its_id` = @ItsId;
            """;

        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(
                commandText: query,
                parameters: new { ItsId = itsId, NewPasswordHash = newPasswordHash },
                cancellationToken: cancellationToken
            )
        );

        return rowsAffected > 0;
    }
}

