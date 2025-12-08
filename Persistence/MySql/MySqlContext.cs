using System.Data;
using MySqlConnector;
using Microsoft.Extensions.Options;
using BurhaniGuards.Api.Models;

namespace BurhaniGuards.Api.Persistence.MySql;

public sealed class MySqlContext
{
    private readonly string _connectionString;

    public MySqlContext(IOptions<MySqlOptions> options)
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









