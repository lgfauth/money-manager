using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace MoneyManager.Tests.Application.Services;

public class CreditCardImportEligibilityServiceTests
{
    private const string UserId = "user-1";

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICreditCardRepository _cards = Substitute.For<ICreditCardRepository>();
    private readonly IBankConnectionRepository _connections = Substitute.For<IBankConnectionRepository>();
    private readonly CreditCardImportEligibilityService _service;

    public CreditCardImportEligibilityServiceTests()
    {
        _unitOfWork.CreditCards.Returns(_cards);
        _unitOfWork.BankConnections.Returns(_connections);
        _connections.GetByUserIdAsync(UserId).Returns([]);
        _service = new CreditCardImportEligibilityService(_unitOfWork);
    }

    private static BankConnection Connection(BankConnectionStatus status, string entityType, string? accountId, bool deleted = false) => new()
    {
        UserId = UserId,
        Status = status,
        IsDeleted = deleted,
        SelectedAccounts = [new SelectedBankAccount { MoneyManagerEntityType = entityType, MoneyManagerAccountId = accountId }]
    };

    [Theory]
    [InlineData(BankConnectionStatus.Connected, true)]
    [InlineData(BankConnectionStatus.Error, true)]
    [InlineData(BankConnectionStatus.Disconnected, false)]
    public async Task IsSyncedByOpenFinanceAsync_ShouldDependOnConnectionStatus(BankConnectionStatus status, bool expected)
    {
        // Arrange
        _connections.GetByUserIdAsync(UserId).Returns([Connection(status, "CreditCard", "card-1")]);

        // Act
        var synced = await _service.IsSyncedByOpenFinanceAsync(UserId, "card-1");

        // Assert
        Assert.Equal(expected, synced);
    }

    [Fact]
    public async Task IsSyncedByOpenFinanceAsync_WithDeletedConnection_ShouldReturnFalse()
    {
        // Arrange
        _connections.GetByUserIdAsync(UserId).Returns([Connection(BankConnectionStatus.Connected, "CreditCard", "card-1", deleted: true)]);

        // Act & Assert
        Assert.False(await _service.IsSyncedByOpenFinanceAsync(UserId, "card-1"));
    }

    [Fact]
    public async Task IsSyncedByOpenFinanceAsync_WhenLinkIsCheckingAccountWithSameId_ShouldReturnFalse()
    {
        // Arrange
        _connections.GetByUserIdAsync(UserId).Returns([Connection(BankConnectionStatus.Connected, "Account", "card-1")]);

        // Act & Assert
        Assert.False(await _service.IsSyncedByOpenFinanceAsync(UserId, "card-1"));
    }

    [Fact]
    public async Task IsSyncedByOpenFinanceAsync_WhenAnotherCardIsLinked_ShouldReturnFalse()
    {
        // Arrange
        _connections.GetByUserIdAsync(UserId).Returns([Connection(BankConnectionStatus.Connected, "CreditCard", "card-2")]);

        // Act & Assert
        Assert.False(await _service.IsSyncedByOpenFinanceAsync(UserId, "card-1"));
    }

    [Fact]
    public async Task GetEligibleCardsAsync_ShouldReturnOnlyUserCardsNotSynced()
    {
        // Arrange
        var free = new CreditCard { Id = "card-free", UserId = UserId };
        var synced = new CreditCard { Id = "card-synced", UserId = UserId };
        var deleted = new CreditCard { Id = "card-deleted", UserId = UserId, IsDeleted = true };
        var formerlySynced = new CreditCard { Id = "card-old", UserId = UserId };
        _cards.GetByUserAsync(UserId).Returns([free, synced, deleted, formerlySynced]);
        _connections.GetByUserIdAsync(UserId).Returns([
            Connection(BankConnectionStatus.Error, "CreditCard", "card-synced"),
            Connection(BankConnectionStatus.Disconnected, "CreditCard", "card-old")
        ]);

        // Act
        var eligible = await _service.GetEligibleCardsAsync(UserId);

        // Assert
        Assert.Equal(["card-free", "card-old"], eligible.Select(c => c.Id).ToList());
    }
}
