using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using Xunit;

namespace MoneyManager.Tests.Domain.Entities;

public class FinancialHealthEntityTests
{
    // --- FinancialHealthSettings ---

    [Fact]
    public void FinancialHealthSettings_DefaultValues_ShouldBeValid()
    {
        // Act
        var settings = new FinancialHealthSettings();

        // Assert
        Assert.Equal("moderado", settings.ModeName);
        Assert.Equal(20, settings.InvestPercent);
        Assert.Equal(6, settings.ReserveMonths);
        Assert.Equal(250, settings.FireMultiplier);
        Assert.Equal(50, settings.FixedExpensePercent);
        Assert.Equal(30, settings.InstallmentPercent);
        Assert.False(settings.IsDeleted);
        Assert.NotEmpty(settings.Id);
    }

    [Fact]
    public void FinancialHealthSettings_Serialize_ShouldRoundTrip()
    {
        // Arrange
        var settings = new FinancialHealthSettings
        {
            UserId = "user-1",
            ModeName = "conservador",
            InvestPercent = 10,
            ReserveMonths = 12,
            FireMultiplier = 300,
            FixedExpensePercent = 40,
            InstallmentPercent = 20
        };

        // Act
        var doc = settings.ToBsonDocument();
        var deserialized = BsonSerializer.Deserialize<FinancialHealthSettings>(doc);

        // Assert
        Assert.Equal(settings.UserId, deserialized.UserId);
        Assert.Equal(settings.ModeName, deserialized.ModeName);
        Assert.Equal(settings.InvestPercent, deserialized.InvestPercent);
        Assert.Equal(settings.ReserveMonths, deserialized.ReserveMonths);
    }

    [Fact]
    public void FinancialHealthSettings_Deserialize_WithExtraFields_ShouldIgnoreUnknownFields()
    {
        // Arrange
        var document = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "userId", "user-abc" },
            { "modeName", "agressivo_fire" },
            { "investPercent", 40 },
            { "reserveMonths", 3 },
            { "fireMultiplier", 200 },
            { "fixedExpensePercent", 35 },
            { "installmentPercent", 15 },
            { "isDeleted", false },
            { "legacy_field_unknown", "some-value" }
        };

        // Act
        var entity = BsonSerializer.Deserialize<FinancialHealthSettings>(document);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal("user-abc", entity.UserId);
        Assert.Equal("agressivo_fire", entity.ModeName);
        Assert.Equal(40, entity.InvestPercent);
    }

    // --- PatrimonyBucket ---

    [Fact]
    public void PatrimonyBucket_DefaultValues_ShouldBeValid()
    {
        // Act
        var bucket = new PatrimonyBucket();

        // Assert
        Assert.Empty(bucket.Type);
        Assert.Equal(0m, bucket.InitialBalance);
        Assert.Empty(bucket.TrackedCategoryIds);
        Assert.False(bucket.IsDeleted);
        Assert.NotEmpty(bucket.Id);
    }

    [Fact]
    public void PatrimonyBucket_Serialize_ShouldRoundTrip()
    {
        // Arrange
        var bucket = new PatrimonyBucket
        {
            UserId = "user-1",
            Type = "emergency_reserve",
            InitialBalance = 10000m,
            TrackedCategoryIds = ["cat-1", "cat-2"],
            ExpectedAnnualRate = 0.105m
        };

        // Act
        var doc = bucket.ToBsonDocument();
        var deserialized = BsonSerializer.Deserialize<PatrimonyBucket>(doc);

        // Assert
        Assert.Equal(bucket.UserId, deserialized.UserId);
        Assert.Equal(bucket.Type, deserialized.Type);
        Assert.Equal(bucket.InitialBalance, deserialized.InitialBalance);
        Assert.Equal(bucket.TrackedCategoryIds, deserialized.TrackedCategoryIds);
        Assert.Equal(bucket.ExpectedAnnualRate, deserialized.ExpectedAnnualRate);
    }

    // --- MonthlySnapshot ---

    [Fact]
    public void MonthlySnapshot_DefaultValues_ShouldBeValid()
    {
        // Act
        var snapshot = new MonthlySnapshot();

        // Assert
        Assert.True(snapshot.Unconfirmed);
        Assert.False(snapshot.DismissedByUser);
        Assert.Null(snapshot.ConfirmedClosingBalance);
        Assert.Null(snapshot.ConfirmedAt);
        Assert.NotEmpty(snapshot.Id);
    }

    [Fact]
    public void MonthlySnapshot_Deserialize_WithExtraFields_ShouldIgnoreUnknownFields()
    {
        // Arrange
        var document = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "userId", "user-abc" },
            { "bucketId", "bucket-1" },
            { "referenceMonth", "2026-05" },
            { "openingBalance", 5000m },
            { "trackedContributions", 200m },
            { "estimatedYield", 50m },
            { "estimatedClosingBalance", 5250m },
            { "unconfirmed", true },
            { "dismissedByUser", false },
            { "isDeleted", false },
            { "legacy_unknown_field", "ignored" }
        };

        // Act
        var entity = BsonSerializer.Deserialize<MonthlySnapshot>(document);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal("user-abc", entity.UserId);
        Assert.Equal("2026-05", entity.ReferenceMonth);
        Assert.Equal(5250m, entity.EstimatedClosingBalance);
        Assert.True(entity.Unconfirmed);
    }
}
