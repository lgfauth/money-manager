using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoneyManager.Domain.Entities;

[BsonIgnoreExtraElements]
public class MonthlySnapshot
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("bucketId")]
    public string BucketId { get; set; } = string.Empty;

    [BsonElement("referenceMonth")]
    public string ReferenceMonth { get; set; } = string.Empty;

    [BsonElement("openingBalance")]
    public decimal OpeningBalance { get; set; }

    [BsonElement("trackedContributions")]
    public decimal TrackedContributions { get; set; }

    [BsonElement("estimatedYield")]
    public decimal EstimatedYield { get; set; }

    [BsonElement("estimatedClosingBalance")]
    public decimal EstimatedClosingBalance { get; set; }

    [BsonElement("confirmedClosingBalance")]
    public decimal? ConfirmedClosingBalance { get; set; }

    [BsonElement("trackedCategoryIds")]
    public List<string> TrackedCategoryIds { get; set; } = [];

    [BsonElement("unconfirmed")]
    public bool Unconfirmed { get; set; } = true;

    [BsonElement("dismissedByUser")]
    public bool DismissedByUser { get; set; }

    [BsonElement("confirmedAt")]
    public DateTime? ConfirmedAt { get; set; }

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
