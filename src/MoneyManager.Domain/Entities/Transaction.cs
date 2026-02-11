using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Entities;

public class Transaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("accountId")]
    public string AccountId { get; set; } = string.Empty;

    [BsonElement("categoryId")]
    public string? CategoryId { get; set; }

    [BsonElement("type")]
    public TransactionType Type { get; set; }

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = [];

    [BsonElement("toAccountId")]
    public string? ToAccountId { get; set; }

    /// <summary>
    /// ID da fatura de cartão de crédito à qual esta transação pertence (apenas para transações em cartões)
    /// </summary>
    [BsonElement("invoiceId")]
    public string? InvoiceId { get; set; }

    [BsonElement("status")]
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}
