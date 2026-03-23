using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoneyManager.Domain.Entities;

[BsonIgnoreExtraElements]
public class PushSubscription
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Push endpoint URL provided by the browser's push service.
    /// </summary>
    [BsonElement("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// P256DH key from the browser PushSubscription.
    /// </summary>
    [BsonElement("p256dh")]
    public string P256dh { get; set; } = string.Empty;

    /// <summary>
    /// Auth secret from the browser PushSubscription.
    /// </summary>
    [BsonElement("auth")]
    public string Auth { get; set; } = string.Empty;

    [BsonElement("userAgent")]
    public string? UserAgent { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}
