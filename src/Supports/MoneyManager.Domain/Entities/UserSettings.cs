using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoneyManager.Domain.Entities;

[BsonIgnoreExtraElements]
public class UserSettings
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    // Preferências Financeiras
    [BsonElement("currency")]
    public string Currency { get; set; } = "BRL";

    [BsonElement("dateFormat")]
    public string DateFormat { get; set; } = "dd/MM/yyyy";

    [BsonElement("monthClosingDay")]
    public int MonthClosingDay { get; set; } = 1;

    [BsonElement("defaultBudget")]
    public decimal? DefaultBudget { get; set; }

    // Aparência
    [BsonElement("theme")]
    public string Theme { get; set; } = "auto"; // light, dark, auto

    [BsonElement("primaryColor")]
    public string PrimaryColor { get; set; } = "#667eea";

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
