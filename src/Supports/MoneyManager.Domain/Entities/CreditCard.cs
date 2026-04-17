using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoneyManager.Domain.Entities;

[BsonIgnoreExtraElements]
public class CreditCard
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("limit")]
    public decimal Limit { get; set; }

    [BsonElement("closingDay")]
    public int ClosingDay { get; set; }

    [BsonElement("billingDueDay")]
    public int BillingDueDay { get; set; }

    [BsonElement("bestPurchaseDay")]
    public int BestPurchaseDay { get; set; }

    [BsonElement("color")]
    public string Color { get; set; } = "#4E9BFF";

    [BsonElement("currency")]
    public string Currency { get; set; } = "BRL";

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("version")]
    public int Version { get; set; } = 1;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}
