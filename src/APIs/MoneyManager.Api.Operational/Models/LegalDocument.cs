using MongoDB.Bson.Serialization.Attributes;

namespace MoneyManager.Presentation.Models;

public sealed class LegalDocument
{
    [BsonId]
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = string.Empty;
}
