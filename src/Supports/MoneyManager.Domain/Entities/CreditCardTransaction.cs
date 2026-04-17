using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoneyManager.Domain.Entities;

[BsonIgnoreExtraElements]
public class CreditCardTransaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("creditCardId")]
    public string CreditCardId { get; set; } = string.Empty;

    [BsonElement("invoiceId")]
    public string InvoiceId { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("categoryId")]
    public string? CategoryId { get; set; }

    [BsonElement("purchaseDate")]
    public DateTime PurchaseDate { get; set; }

    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }

    [BsonElement("installmentAmount")]
    public decimal InstallmentAmount { get; set; }

    [BsonElement("installmentNumber")]
    public int InstallmentNumber { get; set; }

    [BsonElement("totalInstallments")]
    public int TotalInstallments { get; set; }

    [BsonElement("parentTransactionId")]
    public string? ParentTransactionId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("version")]
    public int Version { get; set; } = 1;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}
