using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Entities;

[BsonIgnoreExtraElements]
public class Account
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    public AccountType Type { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "BRL";

    [BsonElement("color")]
    public string Color { get; set; } = "#00C896";

    [BsonElement("balance")]
    public decimal Balance { get; set; }

    [BsonElement("initialBalance")]
    public decimal InitialBalance { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("version")]
    public int Version { get; set; } = 1;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}
