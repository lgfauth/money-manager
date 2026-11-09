using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using Xunit;

namespace MoneyManager.Tests.Domain.Entities;

public class CreditCardInvoiceImportBsonTests
{
    [Fact]
    public void Serialize_ImportWithItems_ShouldRoundTripAllFields()
    {
        // Arrange
        var closing = new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc);
        var import = new CreditCardInvoiceImport
        {
            UserId = "user-1",
            CreditCardId = ObjectId.GenerateNewId().ToString(),
            Status = InvoiceImportStatus.AwaitingReview,
            SourceType = InvoiceImportSourceType.Images,
            FileCount = 2,
            Attempts = 1,
            ReferenceMonth = "2026-11",
            Invoice = new InvoiceImportDocumentInfo
            {
                Issuer = "Banco X",
                CardLastDigits = "1234",
                ClosingDate = closing,
                ClosingDateCalculated = true,
                DueDate = closing.AddDays(7),
                ReferenceMonth = "2026-11",
                TotalAmount = 1500.75m,
                IsOpenOrPartial = false
            },
            Holders =
            [
                new InvoiceImportHolder
                {
                    Name = "MARIA SILVA",
                    Type = InvoiceImportHolderType.Additional,
                    CardLastDigits = ["1234", "9876"],
                    ItemCount = 3,
                    TotalAmount = 300m,
                    IsSuggested = true
                }
            ],
            Warnings =
            [
                new InvoiceImportWarning { Code = InvoiceImportWarningCode.ClosingDayMismatch, Message = "Ajuste o cartão", IsBlocking = true }
            ],
            Items =
            [
                new InvoiceImportItem
                {
                    Date = closing.AddDays(-5),
                    Description = "Loja 3/10",
                    Amount = 100m,
                    Type = CreditCardTransactionType.Refund,
                    InstallmentNumber = 3,
                    TotalInstallments = 10,
                    HolderName = "MARIA SILVA",
                    HolderType = InvoiceImportHolderType.Additional,
                    CardLastDigits = "9876",
                    CardType = InvoiceImportCardType.Virtual,
                    Source = new InvoiceImportSourceReference { FileIndex = 1, Page = 2 },
                    CategoryHint = "Compras",
                    Confidence = 0.9m,
                    Notes = "USD 20,00 cotação 5,00",
                    Warnings = [InvoiceImportWarningCode.PreviousInstallmentsNotRegistered, InvoiceImportWarningCode.PossibleDuplicate],
                    IsSelectedByDefault = false
                }
            ],
            SelectedHolders = ["MARIA SILVA"],
            Result = new InvoiceImportConfirmationResult { CreatedCount = 1, SkippedCount = 2, AffectedInvoiceIds = ["inv-1"] }
        };

        // Act
        var document = import.ToBsonDocument();
        var restored = BsonSerializer.Deserialize<CreditCardInvoiceImport>(document);

        // Assert
        Assert.Equal("AwaitingReview", document["status"].AsString);
        Assert.Equal("Images", document["sourceType"].AsString);
        Assert.Equal("PreviousInstallmentsNotRegistered", document["items"][0]["warnings"][0].AsString);
        Assert.Equal(InvoiceImportStatus.AwaitingReview, restored.Status);
        Assert.Equal("2026-11", restored.ReferenceMonth);
        Assert.Equal(closing, restored.Invoice!.ClosingDate);
        Assert.True(restored.Invoice.ClosingDateCalculated);
        Assert.Equal(["1234", "9876"], restored.Holders[0].CardLastDigits);
        Assert.True(restored.Warnings[0].IsBlocking);
        var item = Assert.Single(restored.Items);
        Assert.Equal(import.Items[0].Id, item.Id);
        Assert.Equal(CreditCardTransactionType.Refund, item.Type);
        Assert.Equal(3, item.InstallmentNumber);
        Assert.Equal(InvoiceImportCardType.Virtual, item.CardType);
        Assert.Equal(1, item.Source!.FileIndex);
        Assert.Equal(
            [InvoiceImportWarningCode.PreviousInstallmentsNotRegistered, InvoiceImportWarningCode.PossibleDuplicate],
            item.Warnings);
        Assert.False(item.IsSelectedByDefault);
        Assert.Equal(1, restored.Result!.CreatedCount);
    }

    [Fact]
    public void Serialize_QueuedImportWithoutOptionalData_ShouldOmitNullFields()
    {
        // Arrange
        var import = new CreditCardInvoiceImport { UserId = "user-1", CreditCardId = "card-1" };

        // Act
        var document = import.ToBsonDocument();

        // Assert
        Assert.Equal("Queued", document["status"].AsString);
        Assert.False(document.Contains("invoice"));
        Assert.False(document.Contains("errorMessage"));
        Assert.False(document.Contains("referenceMonth"));
        Assert.False(document.Contains("result"));
        Assert.Equal(1, document["version"].AsInt32);
        Assert.False(document["isDeleted"].AsBoolean);
    }

    [Fact]
    public void Deserialize_ImportWithUnknownField_ShouldIgnoreIt()
    {
        // Arrange
        var document = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "userId", "user-1" },
            { "creditCardId", "card-1" },
            { "status", "Failed" },
            { "sourceType", "Pdf" },
            { "errorMessage", "A fatura ainda não foi fechada." },
            { "campoFuturo", "x" }
        };

        // Act
        var import = BsonSerializer.Deserialize<CreditCardInvoiceImport>(document);

        // Assert
        Assert.Equal(InvoiceImportStatus.Failed, import.Status);
        Assert.Equal(InvoiceImportSourceType.Pdf, import.SourceType);
        Assert.Equal("A fatura ainda não foi fechada.", import.ErrorMessage);
        Assert.Empty(import.Items);
    }

    [Fact]
    public void Serialize_ImportFile_ShouldRoundTripContent()
    {
        // Arrange
        var file = new CreditCardInvoiceImportFile
        {
            UserId = "user-1",
            ImportId = "import-1",
            Order = 2,
            ContentType = "image/jpeg",
            SizeBytes = 3,
            Content = [0xFF, 0xD8, 0xFF]
        };

        // Act
        var document = file.ToBsonDocument();
        var restored = BsonSerializer.Deserialize<CreditCardInvoiceImportFile>(document);

        // Assert
        Assert.Equal(BsonType.DateTime, document["createdAt"].BsonType);
        Assert.Equal("import-1", restored.ImportId);
        Assert.Equal(2, restored.Order);
        Assert.Equal(file.Content, restored.Content);
    }
}
