using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Entities;

public class Account
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    public AccountType Type { get; set; }

    [BsonElement("balance")]
    public decimal Balance { get; set; }

    [BsonElement("initialBalance")]
    public decimal InitialBalance { get; set; }

    [BsonElement("creditLimit")]
    public decimal? CreditLimit { get; set; }

    /// <summary>
    /// For credit card accounts, defines the invoice closing day (1..31).
    /// Installment purchases should post to the invoice as of (closing day + 1).
    /// </summary>
    [BsonElement("invoiceClosingDay")]
    public int? InvoiceClosingDay { get; set; } = 1;

    /// <summary>
    /// Dias entre o fechamento da fatura e o vencimento (padrão: 7 dias)
    /// </summary>
    [BsonElement("invoiceDueDayOffset")]
    public int InvoiceDueDayOffset { get; set; } = 7;

    /// <summary>
    /// Data/hora do último fechamento de fatura deste cartão
    /// </summary>
    [BsonElement("lastInvoiceClosedAt")]
    public DateTime? LastInvoiceClosedAt { get; set; }

    /// <summary>
    /// ID da fatura atualmente aberta (que está aceitando transações)
    /// </summary>
    [BsonElement("currentOpenInvoiceId")]
    public string? CurrentOpenInvoiceId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}
