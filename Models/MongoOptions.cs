namespace BurhaniGuards.Api.Models;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "burhaniguards";
    public string MembersCollection { get; set; } = "members";
}

