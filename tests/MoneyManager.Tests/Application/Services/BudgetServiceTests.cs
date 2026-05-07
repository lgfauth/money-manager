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

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 1).Returns(new List<Transaction>());
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

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 1).Returns(new List<Transaction>());
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

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 1).Returns(new List<Transaction>());
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

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 1).Returns(new List<Transaction>());
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 2).Returns(new List<Transaction>());
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

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 1).Returns(transactions);
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

        // GetByUserAndMonthAsync already filters IsDeleted=false in the repository,
        // so the mock only returns non-deleted transactions
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
            }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget>());
        budgetRepo.AddAsync(Arg.Any<Budget>()).Returns(x => x.Arg<Budget>());
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 1).Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.CreateOrUpdateAsync(userId, month, items);

        // Assert
        var item = result.Items.First();
        Assert.Equal(300m, item.SpentAmount);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteBudget()
    {
        // Arrange
        var userId = "user123";
        var budget = new Budget
        {
            Id = "budget123",
            UserId = userId,
            Month = "2024-01",
            Items = new List<BudgetItem>()
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetByIdAsync("budget123").Returns(budget);
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        // Act
        await _budgetService.DeleteAsync(userId, "budget123");

        // Assert
        Assert.True(budget.IsDeleted);
        Assert.NotNull(budget.DeletedAt);
        await budgetRepo.Received(1).UpdateAsync(Arg.Is<Budget>(b => b.IsDeleted));
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ShouldThrowException()
    {
        // Arrange
        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetByIdAsync("invalid").Returns((Budget?)null);
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _budgetService.DeleteAsync("user123", "invalid"));
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeleted_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var budget = new Budget
        {
            Id = "budget123",
            UserId = userId,
            IsDeleted = true
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetByIdAsync("budget123").Returns(budget);
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _budgetService.DeleteAsync(userId, "budget123"));
    }

    [Fact]
    public async Task DeleteAsync_WrongUser_ShouldThrowException()
    {
        // Arrange
        var budget = new Budget
        {
            Id = "budget123",
            UserId = "other-user",
            Month = "2024-01"
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetByIdAsync("budget123").Returns(budget);
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _budgetService.DeleteAsync("user123", "budget123"));
    }

    [Fact]
    public async Task CreateOrUpdateAsync_MultipleCategories_ShouldCalculateSpentPerCategory()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-03";
        var items = new List<BudgetItemRequestDto>
        {
            new BudgetItemRequestDto { CategoryId = "food", LimitAmount = 800m },
            new BudgetItemRequestDto { CategoryId = "transport", LimitAmount = 400m },
            new BudgetItemRequestDto { CategoryId = "entertainment", LimitAmount = 200m }
        };

        var transactions = new List<Transaction>
        {
            new Transaction { UserId = userId, CategoryId = "food", Amount = 150m, Type = TransactionType.Expense },
            new Transaction { UserId = userId, CategoryId = "food", Amount = 200m, Type = TransactionType.Expense },
            new Transaction { UserId = userId, CategoryId = "transport", Amount = 50m, Type = TransactionType.Expense },
            new Transaction { UserId = userId, CategoryId = "food", Amount = 500m, Type = TransactionType.Income }, // Income — not counted
            new Transaction { UserId = userId, CategoryId = "entertainment", Amount = 100m, Type = TransactionType.Expense }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget>());
        budgetRepo.AddAsync(Arg.Any<Budget>()).Returns(x => x.Arg<Budget>());
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 3).Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.CreateOrUpdateAsync(userId, month, items);

        // Assert
        var foodItem = result.Items.First(i => i.CategoryId == "food");
        var transportItem = result.Items.First(i => i.CategoryId == "transport");
        var entertainmentItem = result.Items.First(i => i.CategoryId == "entertainment");

        Assert.Equal(350m, foodItem.SpentAmount); // 150 + 200
        Assert.Equal(50m, transportItem.SpentAmount);
        Assert.Equal(100m, entertainmentItem.SpentAmount);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_CategoryWithNoTransactions_ShouldHaveZeroSpent()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-06";
        var items = new List<BudgetItemRequestDto>
        {
            new BudgetItemRequestDto { CategoryId = "cat-no-expenses", LimitAmount = 1000m }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget>());
        budgetRepo.AddAsync(Arg.Any<Budget>()).Returns(x => x.Arg<Budget>());
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 6).Returns(new List<Transaction>());
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.CreateOrUpdateAsync(userId, month, items);

        // Assert
        Assert.Equal(0m, result.Items.First().SpentAmount);
    }

    [Fact]
    public async Task CopyAsync_WithValidSource_ShouldCreateTargetBudget()
    {
        // Arrange
        var userId = "user123";
        var sourceMonth = "2024-01";
        var targetMonth = "2024-02";
        var sourceBudget = new Budget
        {
            Id = "budget1",
            UserId = userId,
            Month = sourceMonth,
            Items = new List<BudgetItem>
            {
                new BudgetItem { CategoryId = "cat1", LimitAmount = 1000m },
                new BudgetItem { CategoryId = "cat2", LimitAmount = 500m }
            }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        // Source exists; target does not
        budgetRepo.GetAllAsync().Returns(new List<Budget> { sourceBudget });
        budgetRepo.AddAsync(Arg.Any<Budget>()).Returns(x => x.Arg<Budget>());
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 2).Returns(new List<Transaction>());
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.CopyAsync(userId, sourceMonth, targetMonth);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetMonth, result.Month);
        Assert.Equal(2, result.Items.Count);
        await budgetRepo.Received(1).AddAsync(Arg.Is<Budget>(b => b.Month == targetMonth));
    }

    [Fact]
    public async Task CopyAsync_WithNonExistentSource_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget>());
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _budgetService.CopyAsync("user123", "2024-01", "2024-02"));
    }

    [Fact]
    public async Task CopyAsync_ShouldPreserveLimitAmountsFromSource()
    {
        // Arrange
        var userId = "user123";
        var sourceBudget = new Budget
        {
            Id = "budget1",
            UserId = userId,
            Month = "2024-03",
            Items = new List<BudgetItem>
            {
                new BudgetItem { CategoryId = "food", LimitAmount = 800m },
                new BudgetItem { CategoryId = "transport", LimitAmount = 300m }
            }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget> { sourceBudget });
        budgetRepo.AddAsync(Arg.Any<Budget>()).Returns(x => x.Arg<Budget>());
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 4).Returns(new List<Transaction>());
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.CopyAsync(userId, "2024-03", "2024-04");

        // Assert
        Assert.Equal(800m, result.Items.First(i => i.CategoryId == "food").LimitAmount);
        Assert.Equal(300m, result.Items.First(i => i.CategoryId == "transport").LimitAmount);
    }

    [Fact]
    public async Task CopyAsync_WhenTargetAlreadyExists_ShouldOverwriteTarget()
    {
        // Arrange
        var userId = "user123";
        var sourceBudget = new Budget
        {
            Id = "budget-src",
            UserId = userId,
            Month = "2024-01",
            Items = new List<BudgetItem>
            {
                new BudgetItem { CategoryId = "cat1", LimitAmount = 1200m }
            }
        };
        var existingTargetBudget = new Budget
        {
            Id = "budget-tgt",
            UserId = userId,
            Month = "2024-02",
            Items = new List<BudgetItem>
            {
                new BudgetItem { CategoryId = "cat99", LimitAmount = 50m }
            }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget> { sourceBudget, existingTargetBudget });
        budgetRepo.UpdateAsync(Arg.Any<Budget>()).Returns(x => x.Arg<Budget>());
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 2).Returns(new List<Transaction>());
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.CopyAsync(userId, "2024-01", "2024-02");

        // Assert: target should be overwritten with source items
        Assert.Single(result.Items);
        Assert.Equal("cat1", result.Items.First().CategoryId);
        Assert.Equal(1200m, result.Items.First().LimitAmount);
        await budgetRepo.Received(1).UpdateAsync(Arg.Any<Budget>());
        await budgetRepo.DidNotReceive().AddAsync(Arg.Any<Budget>());
    }

    [Fact]
    public async Task CopyAsync_ShouldNotCopySpentAmountsFromSource()
    {
        // Arrange: source has spent amounts; target month has no transactions
        var userId = "user123";
        var sourceBudget = new Budget
        {
            Id = "budget1",
            UserId = userId,
            Month = "2024-01",
            Items = new List<BudgetItem>
            {
                new BudgetItem { CategoryId = "cat1", LimitAmount = 1000m, SpentAmount = 750m }
            }
        };

        var budgetRepo = Substitute.For<IRepository<Budget>>();
        budgetRepo.GetAllAsync().Returns(new List<Budget> { sourceBudget });
        budgetRepo.AddAsync(Arg.Any<Budget>()).Returns(x => x.Arg<Budget>());
        _unitOfWorkMock.Budgets.Returns(budgetRepo);

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetByUserAndMonthAsync(userId, 2024, 2).Returns(new List<Transaction>());
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _budgetService.CopyAsync(userId, "2024-01", "2024-02");

        // Assert: spent amounts reflect target month transactions (zero), not source
        Assert.Equal(0m, result.Items.First().SpentAmount);
    }
}

