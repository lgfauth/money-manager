using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Entities;

[BsonIgnoreExtraElements]
public class BankConnection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("externalConnectionId")]
    public string ExternalConnectionId { get; set; } = string.Empty;

    [BsonElement("institutionName")]
    public string InstitutionName { get; set; } = string.Empty;

    [BsonElement("institutionLogo")]
    public string? InstitutionLogo { get; set; }

    [BsonElement("status")]
    public BankConnectionStatus Status { get; set; } = BankConnectionStatus.Connected;

    [BsonElement("selectedAccounts")]
    public List<SelectedBankAccount> SelectedAccounts { get; set; } = [];

    [BsonElement("onboardingStrategy")]
    public OnboardingStrategy? OnboardingStrategy { get; set; }

    [BsonElement("cutoffDate")]
    public DateTime? CutoffDate { get; set; }

    [BsonElement("lastSyncAt")]
    public DateTime? LastSyncAt { get; set; }

    [BsonElement("connectedAt")]
    public DateTime? ConnectedAt { get; set; }

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void Disconnect()
    {
        Status = BankConnectionStatus.Disconnected;
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

// Documento embutido — não é uma entidade separada.
public class SelectedBankAccount
{
    [BsonElement("externalAccountId")]
    public string ExternalAccountId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("moneyManagerAccountId")]
    public string? MoneyManagerAccountId { get; set; }

    [BsonElement("lastSyncAt")]
    public DateTime? LastSyncAt { get; set; }
}
