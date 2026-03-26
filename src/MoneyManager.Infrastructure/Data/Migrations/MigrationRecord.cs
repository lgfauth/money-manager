using MongoDB.Bson.Serialization.Attributes;

namespace MoneyManager.Infrastructure.Data.Migrations;

public class MigrationRecord
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("appliedAt")]
    public DateTime AppliedAt { get; set; }
}
