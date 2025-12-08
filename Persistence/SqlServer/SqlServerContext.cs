using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using BurhaniGuards.Api.Models;

namespace BurhaniGuards.Api.Persistence.SqlServer;

public sealed class SqlServerContext
{
    private readonly string _connectionString;

    public SqlServerContext(IOptions<SqlServerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
        {
            throw new InvalidOperationException("SQL Server connection string is required in appsettings.json");
        }
        _connectionString = options.Value.ConnectionString;
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}








