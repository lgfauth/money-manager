using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;
using NSubstitute;
using Xunit;

namespace MoneyManager.Tests.Application.Services;

public class FinancialHealthServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessLogger _logger;
    private readonly IFinancialHealthSettingsRepository _settingsRepo;
    private readonly IPatrimonyBucketRepository _bucketsRepo;
    private readonly IMonthlySnapshotRepository _snapshotsRepo;
    private readonly ITransactionRepository _transactionsRepo;
    private readonly FinancialHealthService _service;

    private const string UserId = "user-abc";

    public FinancialHealthServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<IProcessLogger>();
        _settingsRepo = Substitute.For<IFinancialHealthSettingsRepository>();
        _bucketsRepo = Substitute.For<IPatrimonyBucketRepository>();
        _snapshotsRepo = Substitute.For<IMonthlySnapshotRepository>();
        _transactionsRepo = Substitute.For<ITransactionRepository>();

        _unitOfWork.FinancialHealthSettings.Returns(_settingsRepo);
        _unitOfWork.PatrimonyBuckets.Returns(_bucketsRepo);
        _unitOfWork.MonthlySnapshots.Returns(_snapshotsRepo);
        _unitOfWork.Transactions.Returns(_transactionsRepo);

        _service = new FinancialHealthService(_unitOfWork, _logger);
    }

    // --- GetSettingsAsync ---

    [Fact]
    public async Task GetSettingsAsync_WhenSettingsExist_ShouldReturnDto()
    {
        // Arrange
        var settings = new FinancialHealthSettings { UserId = UserId, ModeName = "moderado", InvestPercent = 20 };
        _settingsRepo.GetByUserIdAsync(UserId).Returns(settings);

        // Act
        var result = await _service.GetSettingsAsync(UserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("moderado", result.ModeName);
        Assert.Equal(20, result.InvestPercent);
    }

    [Fact]
    public async Task GetSettingsAsync_WhenNoSettings_ShouldReturnNull()
    {
        // Arrange
        _settingsRepo.GetByUserIdAsync(UserId).Returns((FinancialHealthSettings?)null);

        // Act
        var result = await _service.GetSettingsAsync(UserId);

        // Assert
        Assert.Null(result);
    }

    // --- UpsertSettingsAsync ---

    [Fact]
    public async Task UpsertSettingsAsync_WhenNoExistingSettings_ShouldCreate()
    {
        // Arrange
        _settingsRepo.GetByUserIdAsync(UserId).Returns((FinancialHealthSettings?)null);
        _settingsRepo.AddAsync(Arg.Any<FinancialHealthSettings>()).Returns(x => x.Arg<FinancialHealthSettings>());

        var request = new UpsertFinancialHealthSettingsRequestDto
        {
            ModeName = "conservador",
            InvestPercent = 10,
            ReserveMonths = 12,
            FireMultiplier = 300,
            FixedExpensePercent = 40,
            InstallmentPercent = 20
        };

        // Act
        var result = await _service.UpsertSettingsAsync(UserId, request);

        // Assert
        Assert.Equal("conservador", result.ModeName);
        Assert.Equal(10, result.InvestPercent);
        await _settingsRepo.Received(1).AddAsync(Arg.Any<FinancialHealthSettings>());
    }

    [Fact]
    public async Task UpsertSettingsAsync_WhenSettingsExist_ShouldUpdate()
    {
        // Arrange
        var existing = new FinancialHealthSettings { UserId = UserId, ModeName = "moderado", InvestPercent = 20 };
        _settingsRepo.GetByUserIdAsync(UserId).Returns(existing);

        var request = new UpsertFinancialHealthSettingsRequestDto
        {
            ModeName = "agressivo_fire",
            InvestPercent = 40,
            ReserveMonths = 3,
            FireMultiplier = 200,
            FixedExpensePercent = 35,
            InstallmentPercent = 15
        };

        // Act
        var result = await _service.UpsertSettingsAsync(UserId, request);

        // Assert
        Assert.Equal("agressivo_fire", result.ModeName);
        Assert.Equal(40, result.InvestPercent);
        await _settingsRepo.Received(1).UpdateAsync(Arg.Any<FinancialHealthSettings>());
        await _settingsRepo.DidNotReceive().AddAsync(Arg.Any<FinancialHealthSettings>());
    }

    // --- UpsertBucketAsync ---

    [Fact]
    public async Task UpsertBucketAsync_WhenNoBucket_ShouldCreate()
    {
        // Arrange
        _bucketsRepo.GetByUserAndTypeAsync(UserId, "emergency_reserve").Returns((PatrimonyBucket?)null);
        _bucketsRepo.AddAsync(Arg.Any<PatrimonyBucket>()).Returns(x => x.Arg<PatrimonyBucket>());

        var request = new UpsertPatrimonyBucketRequestDto
        {
            Type = "emergency_reserve",
            InitialBalance = 5000m,
            InitialBalanceDate = DateTime.UtcNow,
            TrackedCategoryIds = ["cat-1"],
            ExpectedAnnualRate = 0.105m
        };

        // Act
        var result = await _service.UpsertBucketAsync(UserId, request);

        // Assert
        Assert.Equal("emergency_reserve", result.Type);
        Assert.Equal(5000m, result.InitialBalance);
        await _bucketsRepo.Received(1).AddAsync(Arg.Any<PatrimonyBucket>());
    }

    [Fact]
    public async Task UpsertBucketAsync_WhenBucketExists_ShouldUpdate()
    {
        // Arrange
        var existing = new PatrimonyBucket { UserId = UserId, Type = "fire_investment", InitialBalance = 10000m };
        _bucketsRepo.GetByUserAndTypeAsync(UserId, "fire_investment").Returns(existing);

        var request = new UpsertPatrimonyBucketRequestDto
        {
            Type = "fire_investment",
            InitialBalance = 15000m,
            InitialBalanceDate = DateTime.UtcNow,
            TrackedCategoryIds = [],
            ExpectedAnnualRate = 0.12m
        };

        // Act
        var result = await _service.UpsertBucketAsync(UserId, request);

        // Assert
        Assert.Equal(15000m, result.InitialBalance);
        await _bucketsRepo.Received(1).UpdateAsync(Arg.Any<PatrimonyBucket>());
        await _bucketsRepo.DidNotReceive().AddAsync(Arg.Any<PatrimonyBucket>());
    }

    // --- GetCurrentSnapshotStatusAsync ---

    [Fact]
    public async Task GetCurrentSnapshotStatusAsync_WhenNoConfiguration_ShouldReturnHasConfigurationFalse()
    {
        // Arrange
        _settingsRepo.GetByUserIdAsync(UserId).Returns((FinancialHealthSettings?)null);
        _bucketsRepo.GetByUserIdAsync(UserId).Returns([]);

        // Act
        var result = await _service.GetCurrentSnapshotStatusAsync(UserId);

        // Assert
        Assert.False(result.HasConfiguration);
        Assert.False(result.ShowBanner);
    }

    [Fact]
    public async Task GetCurrentSnapshotStatusAsync_WhenPendingSnapshotsExist_ShouldShowBanner()
    {
        // Arrange
        var settings = new FinancialHealthSettings { UserId = UserId };
        var bucket = new PatrimonyBucket { Id = "bucket-1", UserId = UserId, Type = "emergency_reserve" };
        var snapshot = new MonthlySnapshot
        {
            BucketId = "bucket-1",
            UserId = UserId,
            Unconfirmed = true,
            DismissedByUser = false,
            EstimatedClosingBalance = 5000m
        };

        _settingsRepo.GetByUserIdAsync(UserId).Returns(settings);
        _bucketsRepo.GetByUserIdAsync(UserId).Returns([bucket]);
        _snapshotsRepo.GetByUserAndMonthAsync(UserId, Arg.Any<string>()).Returns([snapshot]);

        // Act
        var result = await _service.GetCurrentSnapshotStatusAsync(UserId);

        // Assert
        Assert.True(result.HasConfiguration);
        Assert.True(result.ShowBanner);
        Assert.Single(result.PendingBuckets);
    }

    [Fact]
    public async Task GetCurrentSnapshotStatusAsync_WhenSnapshotsDismissed_ShouldNotShowBanner()
    {
        // Arrange
        var settings = new FinancialHealthSettings { UserId = UserId };
        var bucket = new PatrimonyBucket { Id = "bucket-1", UserId = UserId, Type = "emergency_reserve" };
        var snapshot = new MonthlySnapshot
        {
            BucketId = "bucket-1",
            UserId = UserId,
            Unconfirmed = true,
            DismissedByUser = true
        };

        _settingsRepo.GetByUserIdAsync(UserId).Returns(settings);
        _bucketsRepo.GetByUserIdAsync(UserId).Returns([bucket]);
        _snapshotsRepo.GetByUserAndMonthAsync(UserId, Arg.Any<string>()).Returns([snapshot]);

        // Act
        var result = await _service.GetCurrentSnapshotStatusAsync(UserId);

        // Assert
        Assert.False(result.ShowBanner);
        Assert.Empty(result.PendingBuckets);
    }

    // --- ConfirmSnapshotAsync ---

    [Fact]
    public async Task ConfirmSnapshotAsync_WhenSnapshotExists_ShouldConfirm()
    {
        // Arrange
        var snapshot = new MonthlySnapshot
        {
            Id = "snap-1",
            UserId = UserId,
            BucketId = "bucket-1",
            Unconfirmed = true
        };

        _snapshotsRepo.GetByBucketAndMonthAsync("bucket-1", "2026-05").Returns(snapshot);

        var request = new ConfirmSnapshotRequestDto
        {
            Buckets = [new BucketConfirmationDto { BucketId = "bucket-1", ConfirmedBalance = 7000m }]
        };

        // Act
        await _service.ConfirmSnapshotAsync(UserId, 2026, 5, request);

        // Assert
        Assert.False(snapshot.Unconfirmed);
        Assert.Equal(7000m, snapshot.ConfirmedClosingBalance);
        await _snapshotsRepo.Received(1).UpdateAsync(snapshot);
    }

    [Fact]
    public async Task ConfirmSnapshotAsync_WhenSnapshotNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _snapshotsRepo.GetByBucketAndMonthAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((MonthlySnapshot?)null);

        var request = new ConfirmSnapshotRequestDto
        {
            Buckets = [new BucketConfirmationDto { BucketId = "bucket-x", ConfirmedBalance = 1000m }]
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.ConfirmSnapshotAsync(UserId, 2026, 5, request));
    }

    [Fact]
    public async Task ConfirmSnapshotAsync_WhenSnapshotBelongsToOtherUser_ShouldThrowUnauthorized()
    {
        // Arrange
        var snapshot = new MonthlySnapshot
        {
            Id = "snap-1",
            UserId = "outro-user",
            BucketId = "bucket-1",
            Unconfirmed = true
        };

        _snapshotsRepo.GetByBucketAndMonthAsync("bucket-1", "2026-05").Returns(snapshot);

        var request = new ConfirmSnapshotRequestDto
        {
            Buckets = [new BucketConfirmationDto { BucketId = "bucket-1", ConfirmedBalance = 5000m }]
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.ConfirmSnapshotAsync(UserId, 2026, 5, request));
    }

    // --- DismissSnapshotAsync ---

    [Fact]
    public async Task DismissSnapshotAsync_ShouldMarkAllSnapshotsAsDismissed()
    {
        // Arrange
        var snapshots = new List<MonthlySnapshot>
        {
            new() { Id = "s1", UserId = UserId, DismissedByUser = false },
            new() { Id = "s2", UserId = UserId, DismissedByUser = false }
        };

        _snapshotsRepo.GetByUserAndMonthAsync(UserId, "2026-05").Returns(snapshots);

        // Act
        await _service.DismissSnapshotAsync(UserId, 2026, 5);

        // Assert
        Assert.All(snapshots, s => Assert.True(s.DismissedByUser));
        await _snapshotsRepo.Received(2).UpdateAsync(Arg.Any<MonthlySnapshot>());
    }

    // --- GetHealthScoreAsync ---

    [Fact]
    public async Task GetHealthScoreAsync_WhenSettingsNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _settingsRepo.GetByUserIdAsync(UserId).Returns((FinancialHealthSettings?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetHealthScoreAsync(UserId));
    }

    [Fact]
    public async Task GetHealthScoreAsync_WhenNoTransactionsThisMonth_ShouldReturnHasDataFalse()
    {
        // Arrange
        var settings = new FinancialHealthSettings { UserId = UserId, InvestPercent = 20 };
        _settingsRepo.GetByUserIdAsync(UserId).Returns(settings);
        _bucketsRepo.GetByUserIdAsync(UserId).Returns([]);
        _transactionsRepo.GetByUserAndMonthAsync(UserId, Arg.Any<int>(), Arg.Any<int>()).Returns([]);
        _snapshotsRepo.GetLatestConfirmedByBucketAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((MonthlySnapshot?)null);

        // Act
        var result = await _service.GetHealthScoreAsync(UserId);

        // Assert
        Assert.False(result.HasData);
    }

    [Fact]
    public async Task GetHealthScoreAsync_WhenHasTransactions_ShouldReturnScore()
    {
        // Arrange
        var settings = new FinancialHealthSettings
        {
            UserId = UserId,
            InvestPercent = 20,
            ReserveMonths = 6,
            FireMultiplier = 250,
            FixedExpensePercent = 50,
            InstallmentPercent = 30
        };

        var transactions = new List<Transaction>
        {
            new() { UserId = UserId, Type = TransactionType.Income, Amount = 10000m },
            new() { UserId = UserId, Type = TransactionType.Expense, Amount = 3000m }
        };

        _settingsRepo.GetByUserIdAsync(UserId).Returns(settings);
        _bucketsRepo.GetByUserIdAsync(UserId).Returns([]);
        _transactionsRepo.GetByUserAndMonthAsync(UserId, Arg.Any<int>(), Arg.Any<int>()).Returns(transactions);

        // Act
        var result = await _service.GetHealthScoreAsync(UserId);

        // Assert
        Assert.True(result.HasData);
        Assert.Equal(10000m, result.TotalIncome);
        Assert.Equal(3000m, result.TotalExpenses);
        Assert.InRange(result.OverallScore, 0, 100);
    }
}
