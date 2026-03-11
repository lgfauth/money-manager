using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using Xunit;

namespace MoneyManager.Tests.Domain.Entities;

public class RecurringTransactionBsonTests
{
    [Fact]
    public void Deserialize_WithLegacyExtraField_ShouldIgnoreUnknownField()
    {
        var document = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "userId", "user-123" },
            { "accountId", "account-123" },
            { "type", (int)TransactionType.Expense },
            { "amount", 150.75m },
            { "description", "Legacy recurring" },
            { "frequency", (int)RecurrenceFrequency.Monthly },
            { "startDate", DateTime.UtcNow },
            { "nextOccurrenceDate", DateTime.UtcNow.AddDays(1) },
            { "isDeleted", false },
            { "linkedInvestmentAssetId", "legacy-investment-field" }
        };

        var entity = BsonSerializer.Deserialize<RecurringTransaction>(document);

        Assert.NotNull(entity);
        Assert.Equal("user-123", entity.UserId);
        Assert.Equal("account-123", entity.AccountId);
        Assert.Equal(TransactionType.Expense, entity.Type);
        Assert.Equal(150.75m, entity.Amount);
    }
}
