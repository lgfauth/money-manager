using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Tests.Application.Services;

public class RecurringTransactionServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ITransactionService _transactionServiceMock;
    private readonly ILogger<RecurringTransactionService> _loggerMock;
    private readonly IRecurringTransactionService _recurringTransactionService;

    public RecurringTransactionServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _transactionServiceMock = Substitute.For<ITransactionService>();
        _loggerMock = Substitute.For<ILogger<RecurringTransactionService>>();
        _recurringTransactionService = new RecurringTransactionService(_unitOfWorkMock, _transactionServiceMock, _loggerMock);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateRecurringTransaction()
    {
        // Arrange
        var userId = "user123";
        var accountId = "acc123";
        var request = new CreateRecurringTransactionRequestDto
        {
            AccountId = accountId,
            CategoryId = "cat123",
            Type = TransactionType.Expense,
            Amount = 1000m,
            Description = "Monthly rent",
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow,
            DayOfMonth = 5,
            Tags = new List<string> { "rent", "fixed" }
        };

        var account = new Account { Id = accountId, UserId = userId };
        var recurringRepo = Substitute.For<IRepository<RecurringTransaction>>();
        recurringRepo.AddAsync(Arg.Any<RecurringTransaction>()).Returns(x => x.Arg<RecurringTransaction>());

        _unitOfWorkMock.Accounts.GetByIdAsync(accountId).Returns(account);
        _unitOfWorkMock.RecurringTransactions.Returns(recurringRepo);

        // Act
        var result = await _recurringTransactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Amount, result.Amount);
        Assert.Equal(request.Description, result.Description);
        Assert.True(result.IsActive);
        await recurringRepo.Received(1).AddAsync(Arg.Any<RecurringTransaction>());
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUserRecurringTransactions()
    {
        // Arrange
        var userId = "user123";
        var recurring = new List<RecurringTransaction>
        {
            new RecurringTransaction { Id = "1", UserId = userId, Amount = 1000m, IsActive = true },
            new RecurringTransaction { Id = "2", UserId = userId, Amount = 500m, IsActive = true },
            new RecurringTransaction { Id = "3", UserId = "other", Amount = 2000m, IsActive = true }
        };

        var recurringRepo = Substitute.For<IRepository<RecurringTransaction>>();
        recurringRepo.GetAllAsync().Returns(recurring);
        _unitOfWorkMock.RecurringTransactions.Returns(recurringRepo);

        // Act
        var result = await _recurringTransactionService.GetAllAsync(userId);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnRecurringTransaction()
    {
        // Arrange
        var userId = "user123";
        var recurringId = "rec123";
        var recurring = new RecurringTransaction
        {
            Id = recurringId,
            UserId = userId,
            Amount = 1000m,
            Description = "Rent"
        };

        var recurringRepo = Substitute.For<IRepository<RecurringTransaction>>();
        recurringRepo.GetByIdAsync(recurringId).Returns(recurring);
        _unitOfWorkMock.RecurringTransactions.Returns(recurringRepo);

        // Act
        var result = await _recurringTransactionService.GetByIdAsync(userId, recurringId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(recurringId, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateRecurringTransaction()
    {
        // Arrange
        var userId = "user123";
        var recurringId = "rec123";
        var existing = new RecurringTransaction
        {
            Id = recurringId,
            UserId = userId,
            Amount = 1000m
        };

        var request = new CreateRecurringTransactionRequestDto
        {
            AccountId = "acc123",
            CategoryId = "cat123",
            Type = TransactionType.Expense,
            Amount = 1200m,
            Description = "Updated rent",
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow,
            Tags = new List<string> { "rent" }
        };

        var recurringRepo = Substitute.For<IRepository<RecurringTransaction>>();
        recurringRepo.GetByIdAsync(recurringId).Returns(existing);
        _unitOfWorkMock.RecurringTransactions.Returns(recurringRepo);

        // Act
        var result = await _recurringTransactionService.UpdateAsync(userId, recurringId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1200m, result.Amount);
        await recurringRepo.Received(1).UpdateAsync(Arg.Any<RecurringTransaction>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldMarkAsDeleted()
    {
        // Arrange
        var userId = "user123";
        var recurringId = "rec123";
        var recurring = new RecurringTransaction
        {
            Id = recurringId,
            UserId = userId,
            Amount = 1000m
        };

        var recurringRepo = Substitute.For<IRepository<RecurringTransaction>>();
        recurringRepo.GetByIdAsync(recurringId).Returns(recurring);
        _unitOfWorkMock.RecurringTransactions.Returns(recurringRepo);

        // Act
        await _recurringTransactionService.DeleteAsync(userId, recurringId);

        // Assert
        await recurringRepo.Received(1).UpdateAsync(Arg.Is<RecurringTransaction>(r => r.IsDeleted && !r.IsActive));
    }

    [Fact]
    public async Task CalculateNextOccurrence_Monthly_ShouldReturnNextMonth()
    {
        // Arrange
        var currentDate = new DateTime(2024, 1, 15);

        // Act
        var nextDate = await _recurringTransactionService.CalculateNextOccurrence(
            currentDate, RecurrenceFrequency.Monthly, 15);

        // Assert
        Assert.Equal(new DateTime(2024, 2, 15), nextDate);
    }

    [Fact]
    public async Task CalculateNextOccurrence_Weekly_ShouldReturnNextWeek()
    {
        // Arrange
        var currentDate = new DateTime(2024, 1, 15);

        // Act
        var nextDate = await _recurringTransactionService.CalculateNextOccurrence(
            currentDate, RecurrenceFrequency.Weekly);

        // Assert
        Assert.Equal(new DateTime(2024, 1, 22), nextDate);
    }

    [Fact]
    public async Task ProcessDueRecurrencesAsync_ShouldCreateTransactions()
    {
        // Arrange
        var userId = "user123";
        var today = DateTime.UtcNow.Date;
        
        var dueRecurrence = new RecurringTransaction
        {
            Id = "rec123",
            UserId = userId,
            AccountId = "acc123",
            CategoryId = "cat123",
            Type = TransactionType.Expense,
            Amount = 1000m,
            Description = "Monthly rent",
            Frequency = RecurrenceFrequency.Monthly,
            NextOccurrenceDate = today.AddDays(-1),
            IsActive = true,
            IsDeleted = false
        };

        var recurringList = new List<RecurringTransaction> { dueRecurrence };
        var recurringRepo = Substitute.For<IRepository<RecurringTransaction>>();
        recurringRepo.GetAllAsync().Returns(recurringList);
        _unitOfWorkMock.RecurringTransactions.Returns(recurringRepo);

        // Act
        await _recurringTransactionService.ProcessDueRecurrencesAsync();

        // Assert
        await _transactionServiceMock.Received(1).CreateAsync(Arg.Is<string>(u => u == userId), Arg.Any<CreateTransactionRequestDto>());
        await recurringRepo.Received(1).UpdateAsync(Arg.Any<RecurringTransaction>());
    }
}
