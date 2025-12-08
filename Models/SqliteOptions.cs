namespace BurhaniGuards.Api.Models;

public sealed class SqliteOptions
{
    public const string SectionName = "Sqlite";
    public string DatabasePath { get; set; } = "Data\\burhani_guards.db";
}

