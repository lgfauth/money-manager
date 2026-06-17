using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoneyManager.Domain.Entities;

[BsonIgnoreExtraElements]
public class PatrimonyBucket
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("initialBalance")]
    public decimal InitialBalance { get; set; }

    [BsonElement("initialBalanceDate")]
    public DateTime InitialBalanceDate { get; set; }

    [BsonElement("trackedCategoryIds")]
    public List<string> TrackedCategoryIds { get; set; } = [];

    [BsonElement("expectedAnnualRate")]
    public decimal ExpectedAnnualRate { get; set; }

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
