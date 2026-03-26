using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Tests.Application.Services;

public class TransactionServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IRepository<Account> _accountRepoMock;
    private readonly ITransactionRepository _transactionRepoMock;
    private readonly ICreditCardInvoiceService _invoiceServiceMock;
    private readonly ILogger<TransactionService> _loggerMock;
    private readonly ITransactionService _transactionService;

    public TransactionServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _accountRepoMock = Substitute.For<IRepository<Account>>();
        _transactionRepoMock = Substitute.For<ITransactionRepository>();
        _invoiceServiceMock = Substitute.For<ICreditCardInvoiceService>();
        _loggerMock = Substitute.For<ILogger<TransactionService>>();

        _unitOfWorkMock.Accounts.Returns(_accountRepoMock);
        _unitOfWorkMock.Transactions.Returns(_transactionRepoMock);

        _transactionRepoMock.AddAsync(Arg.Any<Transaction>()).Returns(x => x.Arg<Transaction>());

        _transactionService = new TransactionService(_unitOfWorkMock, _invoiceServiceMock, _loggerMock);
    }

    [Fact]
    public async Task CreateAsync_WithIncome_ShouldIncreaseBalance()
    {
        // Arrange
        var userId = "user123";
        var accountId = "acc123";
        var request = new CreateTransactionRequestDto
        {
            AccountId = accountId,
            CategoryId = "cat123",
            Type = 0, // Income
            Amount = 500m,
            Date = DateTime.UtcNow,
            Description = "Salary",
            Status = 0
        };

        var account = new Account { Id = accountId, UserId = userId, Balance = 1000m };
        _accountRepoMock.GetByIdAsync(accountId).Returns(account);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Amount, result.Amount);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(1500m, account.Balance);
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == accountId));
        await _transactionRepoMock.Received(1).AddAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task CreateAsync_WithExpense_ShouldDecreaseBalance()
    {
        // Arrange
        var userId = "user123";
        var accountId = "acc123";
        var request = new CreateTransactionRequestDto
        {
            AccountId = accountId,
            CategoryId = "cat123",
            Type = 1, // Expense
            Amount = 200m,
            Date = DateTime.UtcNow,
            Description = "Groceries",
            Status = 0
        };

        var account = new Account { Id = accountId, UserId = userId, Balance = 1000m };
        _accountRepoMock.GetByIdAsync(accountId).Returns(account);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(800m, account.Balance);
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == accountId));
    }

    [Fact]
    public async Task CreateAsync_WithTransfer_ShouldUpdateBothAccounts()
    {
        // Arrange
        var userId = "user123";
        var fromAccountId = "acc123";
        var toAccountId = "acc456";
        var request = new CreateTransactionRequestDto
        {
            AccountId = fromAccountId,
            ToAccountId = toAccountId,
            Type = 2, // Transfer
            Amount = 300m,
            Date = DateTime.UtcNow,
            Description = "Transfer",
            Status = 0
        };

        var fromAccount = new Account { Id = fromAccountId, UserId = userId, Balance = 1000m, Type = AccountType.Checking };
        var toAccount = new Account { Id = toAccountId, UserId = userId, Balance = 500m, Type = AccountType.Checking };

        _accountRepoMock.GetByIdAsync(fromAccountId).Returns(fromAccount);
        _accountRepoMock.GetByIdAsync(toAccountId).Returns(toAccount);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(700m, fromAccount.Balance);
        Assert.Equal(800m, toAccount.Balance);
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == fromAccountId));
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == toAccountId));
    }

    [Fact]
    public async Task CreateAsync_WithTransfer_ToCreditCard_ShouldReduceDebt()
    {
        // Arrange
        var userId = "user123";
        var fromAccountId = "acc123";
        var toAccountId = "cc456";
        var request = new CreateTransactionRequestDto
        {
            AccountId = fromAccountId,
            ToAccountId = toAccountId,
            Type = 2, // Transfer
            Amount = 300m,
            Date = DateTime.UtcNow,
            Description = "Payment",
            Status = 0
        };

        var fromAccount = new Account { Id = fromAccountId, UserId = userId, Balance = 1000m, Type = AccountType.Checking };
        var toAccount = new Account { Id = toAccountId, UserId = userId, Balance = 500m, Type = AccountType.CreditCard };

        _accountRepoMock.GetByIdAsync(fromAccountId).Returns(fromAccount);
        _accountRepoMock.GetByIdAsync(toAccountId).Returns(toAccount);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(700m, fromAccount.Balance);
        Assert.Equal(200m, toAccount.Balance); // 500 - 300 = 200 (debt reduced)
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == fromAccountId));
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == toAccountId));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidAccount_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var request = new CreateTransactionRequestDto
        {
            AccountId = "invalid",
            Type = 0,
            Amount = 100m,
            Date = DateTime.UtcNow,
            Status = 0
        };

        _accountRepoMock.GetByIdAsync("invalid").Returns((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _transactionService.CreateAsync(userId, request));
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUserTransactions()
    {
        // Arrange
        var userId = "user123";
        var transactions = new List<Transaction>
        {
            new Transaction { Id = "1", UserId = userId, Amount = 100, Type = TransactionType.Income },
            new Transaction { Id = "2", UserId = userId, Amount = 50, Type = TransactionType.Expense },
            new Transaction { Id = "3", UserId = "other", Amount = 200, Type = TransactionType.Income }
        };

        _transactionRepoMock.GetAllAsync().Returns(transactions);

        // Act
        var result = await _transactionService.GetAllAsync(userId);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnTransaction()
    {
        // Arrange
        var userId = "user123";
        var transactionId = "trans123";
        var transaction = new Transaction
        {
            Id = transactionId,
            UserId = userId,
            Amount = 100,
            Type = TransactionType.Income
        };

        _transactionRepoMock.GetByIdAsync(transactionId).Returns(transaction);

        // Act
        var result = await _transactionService.GetByIdAsync(userId, transactionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transactionId, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRevertAndApplyNewImpact()
    {
        // Arrange
        var userId = "user123";
        var transactionId = "trans123";
        var existingTransaction = new Transaction
        {
            Id = transactionId,
            UserId = userId,
            AccountId = "acc123",
            Amount = 100m,
            Type = TransactionType.Income
        };

        var updateRequest = new CreateTransactionRequestDto
        {
            AccountId = "acc123",
            CategoryId = "cat123",
            Type = 0,
            Amount = 150m,
            Date = DateTime.UtcNow,
            Description = "Updated",
            Status = 0
        };

        _transactionRepoMock.GetByIdAsync(transactionId).Returns(existingTransaction);

        var account = new Account { Id = "acc123", UserId = userId, Balance = 1000m };
        _accountRepoMock.GetByIdAsync("acc123").Returns(account);

        // Act
        var result = await _transactionService.UpdateAsync(userId, transactionId, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150m, result.Amount);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRevertImpactAndMarkAsDeleted()
    {
        // Arrange
        var userId = "user123";
        var transactionId = "trans123";
        var transaction = new Transaction
        {
            Id = transactionId,
            UserId = userId,
            AccountId = "acc123",
            Amount = 200m,
            Type = TransactionType.Expense
        };

        _transactionRepoMock.GetByIdAsync(transactionId).Returns(transaction);

        var account = new Account { Id = "acc123", UserId = userId, Balance = 800m };
        _accountRepoMock.GetByIdAsync("acc123").Returns(account);

        // Act
        await _transactionService.DeleteAsync(userId, transactionId);

        // Assert
        Assert.Equal(1000m, account.Balance); // 800 + 200 reverted
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == "acc123"));
        await _transactionRepoMock.Received(1).UpdateAsync(Arg.Is<Transaction>(t => t.IsDeleted));
    }
}
