using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Tests.Application.Services;

public class BudgetServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IBudgetService _budgetService;

    public BudgetServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _budgetService = new BudgetService(_unitOfWorkMock);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithNewBudget_ShouldCreateBudget()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        var items = new List<BudgetItemRequestDto>
        {
            new BudgetItemRequestDto { CategoryId = "cat1", LimitAmount = 1000m },
            new BudgetItemRequestDto { CategoryId = "cat2", LimitAmount = 500m }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget>());
        budgetRepo.AddAsync(Arg.Any<Budget>()).Returns(x => x.Arg<Budget>());
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetAllAsync().Returns(new List<Transaction>());
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.CreateOrUpdateAsync(userId, month, items);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(month, result.Month);
        Assert.Equal(2, result.Items.Count);
        await budgetRepo.Received(1).AddAsync(Arg.Any<Budget>());
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithExistingBudget_ShouldUpdateBudget()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        var existingBudget = new Budget
        {
            Id = "budget123",
            UserId = userId,
            Month = month,
            Items = new List<BudgetItem>()
        };

        var items = new List<BudgetItemRequestDto>
        {
            new BudgetItemRequestDto { CategoryId = "cat1", LimitAmount = 1500m }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget> { existingBudget });
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetAllAsync().Returns(new List<Transaction>());
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.CreateOrUpdateAsync(userId, month, items);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        await budgetRepo.Received(1).UpdateAsync(Arg.Any<Budget>());
    }

    [Fact]
    public async Task GetByMonthAsync_WithValidMonth_ShouldReturnBudget()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        var budget = new Budget
        {
            Id = "budget123",
            UserId = userId,
            Month = month,
            Items = new List<BudgetItem>
            {
                new BudgetItem { CategoryId = "cat1", LimitAmount = 1000m }
            }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget> { budget });
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetAllAsync().Returns(new List<Transaction>());
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.GetByMonthAsync(userId, month);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(month, result.Month);
    }

    [Fact]
    public async Task GetByMonthAsync_WithInvalidMonth_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget>());
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _budgetService.GetByMonthAsync(userId, "2024-99"));
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUserBudgets()
    {
        // Arrange
        var userId = "user123";
        var budgets = new List<Budget>
        {
            new Budget { Id = "1", UserId = userId, Month = "2024-01", Items = new List<BudgetItem>() },
            new Budget { Id = "2", UserId = userId, Month = "2024-02", Items = new List<BudgetItem>() },
            new Budget { Id = "3", UserId = "other", Month = "2024-01", Items = new List<BudgetItem>() }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(budgets);
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetAllAsync().Returns(new List<Transaction>());
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.GetAllAsync(userId);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task CreateOrUpdateAsync_ShouldCalculateSpentAmounts()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        var items = new List<BudgetItemRequestDto>
        {
            new BudgetItemRequestDto { CategoryId = "cat1", LimitAmount = 1000m }
        };

        var transactions = new List<Transaction>
        {
            new Transaction
            {
                UserId = userId,
                CategoryId = "cat1",
                Amount = 300m,
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 15),
                IsDeleted = false
            },
            new Transaction
            {
                UserId = userId,
                CategoryId = "cat1",
                Amount = 200m,
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 20),
                IsDeleted = false
            }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget>());
        budgetRepo.AddAsync(Arg.Any<Budget>()).Returns(x => x.Arg<Budget>());
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.CreateOrUpdateAsync(userId, month, items);

        // Assert
        Assert.NotNull(result);
        var item = result.Items.First();
        Assert.Equal(500m, item.SpentAmount);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_ShouldExcludeDeletedTransactions()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        var items = new List<BudgetItemRequestDto>
        {
            new BudgetItemRequestDto { CategoryId = "cat1", LimitAmount = 1000m }
        };

        var transactions = new List<Transaction>
        {
            new Transaction
            {
                UserId = userId,
                CategoryId = "cat1",
                Amount = 300m,
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 15),
                IsDeleted = false
            },
            new Transaction
            {
                UserId = userId,
                CategoryId = "cat1",
                Amount = 500m,
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 20),
                IsDeleted = true
            }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget>());
        budgetRepo.AddAsync(Arg.Any<Budget>()).Returns(x => x.Arg<Budget>());
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.CreateOrUpdateAsync(userId, month, items);

        // Assert
        var item = result.Items.First();
        Assert.Equal(300m, item.SpentAmount);
    }
}
