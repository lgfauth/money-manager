using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Tests.Application.Services;

public class AccountServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IRepository<Account> _accountRepoMock;
    private readonly ITransactionRepository _transactionRepoMock;
    private readonly ICreditCardInvoiceRepository _invoiceRepoMock;
    private readonly IAccountService _accountService;

    public AccountServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _accountRepoMock = Substitute.For<IRepository<Account>>();
        _transactionRepoMock = Substitute.For<ITransactionRepository>();
        _invoiceRepoMock = Substitute.For<ICreditCardInvoiceRepository>();

        _unitOfWorkMock.Accounts.Returns(_accountRepoMock);
        _unitOfWorkMock.Transactions.Returns(_transactionRepoMock);
        _unitOfWorkMock.CreditCardInvoices.Returns(_invoiceRepoMock);

        _accountRepoMock.AddAsync(Arg.Any<Account>()).Returns(x => x.Arg<Account>());

        _accountService = new AccountService(_unitOfWorkMock);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateAccount()
    {
        // Arrange
        var userId = "user123";
        var request = new CreateAccountRequestDto
        {
            Name = "Checking Account",
            Type = AccountType.Checking,
            InitialBalance = 1000m,
            Color = "#123456"
        };

        // Act
        var result = await _accountService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.InitialBalance, result.Balance);
        Assert.Equal(request.Color, result.Color);
        await _accountRepoMock.Received(1).AddAsync(Arg.Any<Account>());
    }

    [Fact]
    public async Task CreateAsync_CreditCard_ShouldSetCreditCardFields()
    {
        // Arrange
        var userId = "user123";
        var request = new CreateAccountRequestDto
        {
            Name = "My Credit Card",
            Type = AccountType.CreditCard,
            InitialBalance = 0m,
            CreditLimit = 5000m,
            InvoiceClosingDay = 15,
            InvoiceDueDayOffset = 10
        };

        // Act
        var result = await _accountService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5000m, result.CreditLimit);
        Assert.Equal(15, result.InvoiceClosingDay);
        Assert.Equal(10, result.InvoiceDueDayOffset);
    }

    [Fact]
    public async Task CreateAsync_NonCreditCard_ShouldNotSetCreditCardFields()
    {
        // Arrange
        var userId = "user123";
        var request = new CreateAccountRequestDto
        {
            Name = "Checking",
            Type = AccountType.Checking,
            InitialBalance = 1000m,
            CreditLimit = 9999m // Should be ignored
        };

        // Act
        var result = await _accountService.CreateAsync(userId, request);

        // Assert
        Assert.Null(result.CreditLimit);
        Assert.Null(result.InvoiceClosingDay);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUserAccounts()
    {
        // Arrange
        var userId = "user123";
        var accounts = new List<Account>
        {
            new Account { Id = "1", UserId = userId, Name = "Checking", Type = AccountType.Checking, Balance = 1000 },
            new Account { Id = "2", UserId = userId, Name = "Savings", Type = AccountType.Savings, Balance = 5000 }
        };

        _accountRepoMock.GetAllAsync().Returns(accounts);

        // Act
        var result = await _accountService.GetAllAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_ShouldExcludeDeletedAccounts()
    {
        // Arrange
        var userId = "user123";
        var accounts = new List<Account>
        {
            new Account { Id = "1", UserId = userId, Name = "Active", IsDeleted = false },
            new Account { Id = "2", UserId = userId, Name = "Deleted", IsDeleted = true }
        };

        _accountRepoMock.GetAllAsync().Returns(accounts);

        // Act
        var result = await _accountService.GetAllAsync(userId);

        // Assert
        Assert.Single(result);
        Assert.Equal("Active", result.First().Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnAccount()
    {
        // Arrange
        var userId = "user123";
        var account = new Account { Id = "acc1", UserId = userId, Name = "Test", Balance = 500m };
        _accountRepoMock.GetByIdAsync("acc1").Returns(account);

        // Act
        var result = await _accountService.GetByIdAsync(userId, "acc1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("acc1", result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithDeletedAccount_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var account = new Account { Id = "acc1", UserId = userId, IsDeleted = true };
        _accountRepoMock.GetByIdAsync("acc1").Returns(account);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _accountService.GetByIdAsync(userId, "acc1"));
    }

    [Fact]
    public async Task GetByIdAsync_WithWrongUser_ShouldThrowException()
    {
        // Arrange
        var account = new Account { Id = "acc1", UserId = "other-user" };
        _accountRepoMock.GetByIdAsync("acc1").Returns(account);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _accountService.GetByIdAsync("user123", "acc1"));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAccountFields()
    {
        // Arrange
        var userId = "user123";
        var account = new Account
        {
            Id = "acc1",
            UserId = userId,
            Name = "Old Name",
            Type = AccountType.Checking,
            Balance = 1000m
        };
        _accountRepoMock.GetByIdAsync("acc1").Returns(account);

        var request = new CreateAccountRequestDto
        {
            Name = "New Name",
            Type = AccountType.Checking,
            Color = "#654321"
        };

        // Act
        var result = await _accountService.UpdateAsync(userId, "acc1", request);

        // Assert
        Assert.Equal("New Name", result.Name);
        Assert.Equal("#654321", result.Color);
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Name == "New Name" && a.Color == "#654321"));
    }

    [Fact]
    public async Task UpdateAsync_NonExistent_ShouldThrowException()
    {
        // Arrange
        _accountRepoMock.GetByIdAsync("invalid").Returns((Account?)null);

        var request = new CreateAccountRequestDto { Name = "Test", Type = AccountType.Checking };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _accountService.UpdateAsync("user123", "invalid", request));
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteAccount()
    {
        // Arrange
        var userId = "user123";
        var account = new Account { Id = "acc1", UserId = userId };
        _accountRepoMock.GetByIdAsync("acc1").Returns(account);
        _transactionRepoMock.GetAllAsync().Returns(new List<Transaction>());
        _invoiceRepoMock.GetByAccountIdAsync("acc1").Returns(new List<CreditCardInvoice>());

        // Act
        await _accountService.DeleteAsync(userId, "acc1");

        // Assert
        Assert.True(account.IsDeleted);
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.IsDeleted));
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ShouldThrowException()
    {
        // Arrange
        _accountRepoMock.GetByIdAsync("invalid").Returns((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _accountService.DeleteAsync("user123", "invalid"));
    }

    [Fact]
    public async Task DeleteAsync_ShouldCascadeSoftDeleteTransactions()
    {
        // Arrange
        var userId = "user123";
        var accountId = "acc1";
        var account = new Account { Id = accountId, UserId = userId };

        var transactions = new List<Transaction>
        {
            new Transaction { Id = "t1", UserId = userId, AccountId = accountId, IsDeleted = false },
            new Transaction { Id = "t2", UserId = userId, AccountId = accountId, IsDeleted = false },
            new Transaction { Id = "t3", UserId = userId, AccountId = "other-acc", IsDeleted = false }, // Different account
            new Transaction { Id = "t4", UserId = userId, AccountId = accountId, IsDeleted = true } // Already deleted
        };

        _accountRepoMock.GetByIdAsync(accountId).Returns(account);
        _transactionRepoMock.GetAllAsync().Returns(transactions);
        _invoiceRepoMock.GetByAccountIdAsync(accountId).Returns(new List<CreditCardInvoice>());

        // Act
        await _accountService.DeleteAsync(userId, accountId);

        // Assert — only t1 and t2 should be soft deleted (not t3 different account, not t4 already deleted)
        await _transactionRepoMock.Received(2).UpdateAsync(Arg.Any<Transaction>());
        Assert.True(transactions[0].IsDeleted); // t1
        Assert.True(transactions[1].IsDeleted); // t2
        Assert.False(transactions[2].IsDeleted); // t3 unchanged
    }

    [Fact]
    public async Task DeleteAsync_ShouldCascadeSoftDeleteInvoices()
    {
        // Arrange
        var userId = "user123";
        var accountId = "cc1";
        var account = new Account { Id = accountId, UserId = userId, Type = AccountType.CreditCard };

        var invoices = new List<CreditCardInvoice>
        {
            new CreditCardInvoice { Id = "inv1", AccountId = accountId, UserId = userId, IsDeleted = false },
            new CreditCardInvoice { Id = "inv2", AccountId = accountId, UserId = userId, IsDeleted = false },
            new CreditCardInvoice { Id = "inv3", AccountId = accountId, UserId = userId, IsDeleted = true } // Already deleted
        };

        _accountRepoMock.GetByIdAsync(accountId).Returns(account);
        _transactionRepoMock.GetAllAsync().Returns(new List<Transaction>());
        _invoiceRepoMock.GetByAccountIdAsync(accountId).Returns(invoices);

        // Act
        await _accountService.DeleteAsync(userId, accountId);

        // Assert — only inv1 and inv2 should be soft deleted
        await _invoiceRepoMock.Received(2).UpdateAsync(Arg.Any<CreditCardInvoice>());
        Assert.True(invoices[0].IsDeleted);
        Assert.True(invoices[1].IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_FullCascade_ShouldDeleteAccountTransactionsAndInvoices()
    {
        // Arrange
        var userId = "user123";
        var accountId = "cc1";
        var account = new Account { Id = accountId, UserId = userId, Type = AccountType.CreditCard };

        var transactions = new List<Transaction>
        {
            new Transaction { Id = "t1", UserId = userId, AccountId = accountId, IsDeleted = false },
            new Transaction { Id = "t2", UserId = userId, AccountId = accountId, IsDeleted = false }
        };

        var invoices = new List<CreditCardInvoice>
        {
            new CreditCardInvoice { Id = "inv1", AccountId = accountId, UserId = userId, IsDeleted = false }
        };

        _accountRepoMock.GetByIdAsync(accountId).Returns(account);
        _transactionRepoMock.GetAllAsync().Returns(transactions);
        _invoiceRepoMock.GetByAccountIdAsync(accountId).Returns(invoices);

        // Act
        await _accountService.DeleteAsync(userId, accountId);

        // Assert
        Assert.True(account.IsDeleted);
        Assert.True(transactions[0].IsDeleted);
        Assert.True(transactions[1].IsDeleted);
        Assert.True(invoices[0].IsDeleted);
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateBalanceAsync_ShouldAddAmount()
    {
        // Arrange
        var userId = "user123";
        var account = new Account { Id = "acc1", UserId = userId, Balance = 1000m };
        _accountRepoMock.GetByIdAsync("acc1").Returns(account);

        // Act
        await _accountService.UpdateBalanceAsync(userId, "acc1", 500m);

        // Assert
        Assert.Equal(1500m, account.Balance);
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Balance == 1500m));
    }

    [Fact]
    public async Task UpdateBalanceAsync_NonExistent_ShouldThrowException()
    {
        // Arrange
        _accountRepoMock.GetByIdAsync("invalid").Returns((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _accountService.UpdateBalanceAsync("user123", "invalid", 100m));
    }
}
