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

    // item_id retornado pelo Banco MCP — identifica a conexão no workspace.
    [BsonElement("externalConnectionId")]
    public string ExternalConnectionId { get; set; } = string.Empty;

    // connector_id numérico (ex: "612" para Nubank).
    [BsonElement("connectorId")]
    public string ConnectorId { get; set; } = string.Empty;

    // Campo "bank" do Banco MCP — nome de exibição (não o campo "name" que é razão social).
    [BsonElement("institutionName")]
    public string InstitutionName { get; set; } = string.Empty;

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

    public void MarkError()
    {
        Status = BankConnectionStatus.Error;
        UpdatedAt = DateTime.UtcNow;
    }

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
    // account_id do Banco MCP.
    [BsonElement("externalAccountId")]
    public string ExternalAccountId { get; set; } = string.Empty;

    // Campo "bank" do Banco MCP (nome de exibição).
    [BsonElement("bankName")]
    public string BankName { get; set; } = string.Empty;

    // "CHECKING_ACCOUNT" | "CREDIT_CARD"
    [BsonElement("subtype")]
    public string Subtype { get; set; } = string.Empty;

    // "BANK" | "CREDIT"
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    // Número da conta/cartão (ex: "41581457-6" ou "4841").
    [BsonElement("number")]
    public string Number { get; set; } = string.Empty;

    // ID da Account existente no MoneyManager mapeada pelo usuário.
    [BsonElement("moneyManagerAccountId")]
    public string? MoneyManagerAccountId { get; set; }

    [BsonElement("moneyManagerEntityType")]
    public string MoneyManagerEntityType { get; set; } = "Account";

    [BsonElement("lastSyncAt")]
    public DateTime? LastSyncAt { get; set; }
}
