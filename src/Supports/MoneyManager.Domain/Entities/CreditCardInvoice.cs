using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Entities;

[BsonIgnoreExtraElements]
public class CreditCardInvoice
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("creditCardId")]
    public string CreditCardId { get; set; } = string.Empty;

    [BsonElement("referenceMonth")]
    public string ReferenceMonth { get; set; } = string.Empty;

    [BsonElement("closingDate")]
    public DateTime ClosingDate { get; set; }

    [BsonElement("dueDate")]
    public DateTime DueDate { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }

    [BsonElement("paidAt")]
    public DateTime? PaidAt { get; set; }

    [BsonElement("paidWithAccountId")]
    public string? PaidWithAccountId { get; set; }

    [BsonElement("paidAmount")]
    public decimal? PaidAmount { get; set; }

    [BsonElement("paymentTransactionId")]
    public string? PaymentTransactionId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("version")]
    public int Version { get; set; } = 1;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}
