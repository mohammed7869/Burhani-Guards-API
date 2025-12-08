using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using BurhaniGuards.Api.Models;

namespace BurhaniGuards.Api.Persistence.Sqlite;

public sealed class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IOptions<SqliteOptions> options, IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        var databasePath = options.Value.DatabasePath;
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            databasePath = "Data\\burhani_guards.db";
        }

        var absolutePath = Path.Combine(environment.ContentRootPath, databasePath);
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = absolutePath,
            ForeignKeys = true
        };

        _connectionString = builder.ConnectionString;
    }

    public IDbConnection CreateConnection() => new SqliteConnection(_connectionString);
}

