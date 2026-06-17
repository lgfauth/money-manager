using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoneyManager.Domain.Entities;

[BsonIgnoreExtraElements]
public class FinancialHealthSettings
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("modeName")]
    public string ModeName { get; set; } = "moderado";

    [BsonElement("investPercent")]
    public int InvestPercent { get; set; } = 20;

    [BsonElement("reserveMonths")]
    public int ReserveMonths { get; set; } = 6;

    [BsonElement("fireMultiplier")]
    public int FireMultiplier { get; set; } = 250;

    [BsonElement("fixedExpensePercent")]
    public int FixedExpensePercent { get; set; } = 50;

    [BsonElement("installmentPercent")]
    public int InstallmentPercent { get; set; } = 30;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
