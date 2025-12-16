using NSubstitute;
using Xunit;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Tests.Application.Services;

public class ReportServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IReportService _reportService;

    public ReportServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _reportService = new ReportService(_unitOfWorkMock);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_ShouldCalculateTotals()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        
        var transactions = new List<Transaction>
        {
            new Transaction 
            { 
                UserId = userId, 
                Amount = 5000m, 
                Type = TransactionType.Income,
                Date = new DateTime(2024, 1, 15)
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 2000m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 20)
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 1000m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 25)
            }
        };

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _reportService.GetMonthlySummaryAsync(userId, month);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5000m, result.TotalIncome);
        Assert.Equal(3000m, result.TotalExpense);
        Assert.Equal(2000m, result.Balance);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_ShouldFilterByMonth()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        
        var transactions = new List<Transaction>
        {
            new Transaction 
            { 
                UserId = userId, 
                Amount = 1000m, 
                Type = TransactionType.Income,
                Date = new DateTime(2024, 1, 15)
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 2000m, 
                Type = TransactionType.Income,
                Date = new DateTime(2024, 2, 15) // Different month
            }
        };

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _reportService.GetMonthlySummaryAsync(userId, month);

        // Assert
        Assert.Equal(1000m, result.TotalIncome);
    }

    [Fact]
    public async Task GetExpensesByCategoryAsync_ShouldGroupExpenses()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        
        var transactions = new List<Transaction>
        {
            new Transaction 
            { 
                UserId = userId, 
                Amount = 500m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 5),
                CategoryId = "cat1"
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 300m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 10),
                CategoryId = "cat1"
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 200m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 12),
                CategoryId = "cat2"
            }
        };

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _reportService.GetExpensesByCategoryAsync(userId, month);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        var cat1Expense = resultList.First(r => r.CategoryId == "cat1");
        Assert.Equal(800m, cat1Expense.Total);
    }

    [Fact]
    public async Task GetExpensesByCategoryAsync_ShouldFilterDeletedTransactions()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        
        var transactions = new List<Transaction>
        {
            new Transaction 
            { 
                UserId = userId, 
                Amount = 500m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 5),
                CategoryId = "cat1",
                IsDeleted = false
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 300m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 10),
                CategoryId = "cat1",
                IsDeleted = true
            }
        };

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _reportService.GetExpensesByCategoryAsync(userId, month);

        // Assert
        var resultList = result.ToList();
        var cat1Expense = resultList.First(r => r.CategoryId == "cat1");
        Assert.Equal(500m, cat1Expense.Total);
    }

    [Fact]
    public async Task GetExpensesByCategoryAsync_ShouldExcludeIncomeTransactions()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        
        var transactions = new List<Transaction>
        {
            new Transaction 
            { 
                UserId = userId, 
                Amount = 1000m, 
                Type = TransactionType.Income,
                Date = new DateTime(2024, 1, 5),
                CategoryId = "cat1"
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 500m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 10),
                CategoryId = "cat2"
            }
        };

        var transactionRepo = Substitute.For<IRepository<Transaction>>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _reportService.GetExpensesByCategoryAsync(userId, month);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal("cat2", resultList.First().CategoryId);
    }
}
