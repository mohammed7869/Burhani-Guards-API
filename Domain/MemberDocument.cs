using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BurhaniGuards.Api.Domain;

public sealed class MemberDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } = "member";

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}

