using BurhaniGuards.Api.Domain;
using BurhaniGuards.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BurhaniGuards.Api.Persistence.Mongo;

public sealed class MongoContext
{
    private readonly MongoOptions _options;
    public IMongoDatabase Database { get; }

    public MongoContext(IOptions<MongoOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("Mongo connection string is missing. Update appsettings.json.");
        }

        var client = new MongoClient(_options.ConnectionString);
        Database = client.GetDatabase(_options.DatabaseName);
    }

    public IMongoCollection<MemberDocument> Members =>
        Database.GetCollection<MemberDocument>(_options.MembersCollection);
}

