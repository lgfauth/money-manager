using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MoneyManager.Domain.Entities;
using Xunit;

namespace MoneyManager.Tests.Domain.Entities;

public class CreditCardTransactionBsonTests
{
    [Fact]
    public void Deserialize_LegacyDocumentWithoutImportFields_ShouldLeaveThemNull()
    {
        // Arrange — documento gravado antes da importação de fatura existir
        var document = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "userId", "user-1" },
            { "creditCardId", "card-1" },
            { "invoiceId", "invoice-1" },
            { "description", "Mercado" },
            { "purchaseDate", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc) },
            { "totalAmount", 50m },
            { "installmentAmount", 50m },
            { "installmentNumber", 1 },
            { "totalInstallments", 1 },
            { "type", 0 },
            { "source", "manual" },
            { "isDeleted", false }
        };

        // Act
        var transaction = BsonSerializer.Deserialize<CreditCardTransaction>(document);

        // Assert
        Assert.Equal("Mercado", transaction.Description);
        Assert.Null(transaction.InvoiceImportId);
        Assert.Null(transaction.ImportFingerprint);
    }

    [Fact]
    public void Serialize_WithoutImportFields_ShouldNotWriteThem()
    {
        // Arrange
        var transaction = new CreditCardTransaction { UserId = "user-1", Description = "Mercado" };

        // Act
        var document = transaction.ToBsonDocument();

        // Assert
        Assert.False(document.Contains("invoiceImportId"));
        Assert.False(document.Contains("importFingerprint"));
    }

    [Fact]
    public void Serialize_WithImportFields_ShouldRoundTrip()
    {
        // Arrange
        var transaction = new CreditCardTransaction
        {
            UserId = "user-1",
            Source = "invoice_import",
            InvoiceImportId = "import-1",
            ImportFingerprint = "abc123"
        };

        // Act
        var restored = BsonSerializer.Deserialize<CreditCardTransaction>(transaction.ToBsonDocument());

        // Assert
        Assert.Equal("import-1", restored.InvoiceImportId);
        Assert.Equal("abc123", restored.ImportFingerprint);
    }
}
