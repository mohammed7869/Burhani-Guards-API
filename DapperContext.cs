using Microsoft.Extensions.Options;
using BurhaniGuards.Api.Models;
using MySqlConnector;
using System.Data;

namespace BurhaniGuards.Api;

public class DapperContext
{
    private readonly string? _connectionString;

    public DapperContext(IOptions<MySqlOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
        {
            throw new InvalidOperationException("MySQL connection string is required in appsettings.json");
        }
        _connectionString = options.Value.ConnectionString;
    }

    public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
}

