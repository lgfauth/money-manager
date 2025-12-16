using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Tests.Application.Services;

public class TransactionServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IAccountService _accountServiceMock;
    private readonly ITransactionService _transactionService;

    public TransactionServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _accountServiceMock = Substitute.For<IAccountService>();
        _transactionService = new TransactionService(_unitOfWorkMock, _accountServiceMock);
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
        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.AddAsync(Arg.Any<Transaction>()).Returns(x => x.Arg<Transaction>());

        _unitOfWorkMock.Accounts.GetByIdAsync(accountId).Returns(account);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Amount, result.Amount);
        Assert.Equal(request.Description, result.Description);
        await _accountServiceMock.Received(1).UpdateBalanceAsync(userId, accountId, 500m);
        await transactionRepo.Received(1).AddAsync(Arg.Any<Transaction>());
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
        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.AddAsync(Arg.Any<Transaction>()).Returns(x => x.Arg<Transaction>());

        _unitOfWorkMock.Accounts.GetByIdAsync(accountId).Returns(account);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        await _accountServiceMock.Received(1).UpdateBalanceAsync(userId, accountId, -200m);
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

        var fromAccount = new Account { Id = fromAccountId, UserId = userId, Balance = 1000m };
        var toAccount = new Account { Id = toAccountId, UserId = userId, Balance = 500m };
        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.AddAsync(Arg.Any<Transaction>()).Returns(x => x.Arg<Transaction>());

        _unitOfWorkMock.Accounts.GetByIdAsync(fromAccountId).Returns(fromAccount);
        _unitOfWorkMock.Accounts.GetByIdAsync(toAccountId).Returns(toAccount);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        await _accountServiceMock.Received(1).UpdateBalanceAsync(userId, fromAccountId, -300m);
        await _accountServiceMock.Received(1).UpdateBalanceAsync(userId, toAccountId, 300m);
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

        _unitOfWorkMock.Accounts.GetByIdAsync("invalid").Returns((Account?)null);

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

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

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

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetByIdAsync(transactionId).Returns(transaction);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

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

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetByIdAsync(transactionId).Returns(existingTransaction);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        var account = new Account { Id = "acc123", UserId = userId };
        _unitOfWorkMock.Accounts.GetByIdAsync("acc123").Returns(account);

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

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetByIdAsync(transactionId).Returns(transaction);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        await _transactionService.DeleteAsync(userId, transactionId);

        // Assert
        await _accountServiceMock.Received(1).UpdateBalanceAsync(userId, "acc123", 200m);
        await transactionRepo.Received(1).UpdateAsync(Arg.Is<Transaction>(t => t.IsDeleted));
    }
}
