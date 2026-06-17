using Microsoft.Extensions.Logging;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace MoneyManager.Tests.Application.Services;

public class AccountDeletionServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AccountDeletionService> _logger;
    private readonly IUserRepository _userRepo;
    private readonly IRepository<Account> _accountRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IRepository<Budget> _budgetRepo;
    private readonly IRepository<RecurringTransaction> _recurringRepo;
    private readonly AccountDeletionService _service;

    private const string UserId = "user-abc";

    public AccountDeletionServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<AccountDeletionService>>();
        _userRepo = Substitute.For<IUserRepository>();
        _accountRepo = Substitute.For<IRepository<Account>>();
        _categoryRepo = Substitute.For<IRepository<Category>>();
        _transactionRepo = Substitute.For<ITransactionRepository>();
        _budgetRepo = Substitute.For<IRepository<Budget>>();
        _recurringRepo = Substitute.For<IRepository<RecurringTransaction>>();

        _unitOfWork.Users.Returns(_userRepo);
        _unitOfWork.Accounts.Returns(_accountRepo);
        _unitOfWork.Categories.Returns(_categoryRepo);
        _unitOfWork.Transactions.Returns(_transactionRepo);
        _unitOfWork.Budgets.Returns(_budgetRepo);
        _unitOfWork.RecurringTransactions.Returns(_recurringRepo);

        _service = new AccountDeletionService(_unitOfWork, _logger);
    }

    // --- GetUserDataCountAsync ---

    [Fact]
    public async Task GetUserDataCountAsync_ShouldReturnSumOfAllEntities()
    {
        // Arrange
        _accountRepo.GetByUserIdAsync(UserId).Returns([new Account(), new Account()]);
        _categoryRepo.GetByUserIdAsync(UserId).Returns([new Category()]);
        _transactionRepo.GetByUserIdAsync(UserId).Returns([new Transaction(), new Transaction(), new Transaction()]);
        _budgetRepo.GetByUserIdAsync(UserId).Returns([]);
        _recurringRepo.GetByUserIdAsync(UserId).Returns([new RecurringTransaction()]);

        // Act
        var count = await _service.GetUserDataCountAsync(UserId);

        // Assert
        Assert.Equal(7, count);
    }

    [Fact]
    public async Task GetUserDataCountAsync_WhenNoData_ShouldReturnZero()
    {
        // Arrange
        _accountRepo.GetByUserIdAsync(UserId).Returns([]);
        _categoryRepo.GetByUserIdAsync(UserId).Returns([]);
        _transactionRepo.GetByUserIdAsync(UserId).Returns([]);
        _budgetRepo.GetByUserIdAsync(UserId).Returns([]);
        _recurringRepo.GetByUserIdAsync(UserId).Returns([]);

        // Act
        var count = await _service.GetUserDataCountAsync(UserId);

        // Assert
        Assert.Equal(0, count);
    }

    // --- DeleteUserAccountAsync ---

    [Fact]
    public async Task DeleteUserAccountAsync_WithCorrectPassword_ShouldDeleteAllDataAndReturnTrue()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("senha123");
        var user = new User { Id = UserId, PasswordHash = passwordHash };
        _userRepo.GetByIdAsync(UserId).Returns(user);

        // Act
        var result = await _service.DeleteUserAccountAsync(UserId, "senha123");

        // Assert
        Assert.True(result);
        await _recurringRepo.Received(1).DeleteManyByUserIdAsync(UserId);
        await _budgetRepo.Received(1).DeleteManyByUserIdAsync(UserId);
        await _transactionRepo.Received(1).DeleteManyByUserIdAsync(UserId);
        await _accountRepo.Received(1).DeleteManyByUserIdAsync(UserId);
        await _categoryRepo.Received(1).DeleteManyByUserIdAsync(UserId);
        await _userRepo.Received(1).DeleteAsync(UserId);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteUserAccountAsync_WhenUserNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _userRepo.GetByIdAsync(UserId).Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteUserAccountAsync(UserId, "qualquer-senha"));
    }

    [Fact]
    public async Task DeleteUserAccountAsync_WithWrongPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("senha-correta");
        var user = new User { Id = UserId, PasswordHash = passwordHash };
        _userRepo.GetByIdAsync(UserId).Returns(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.DeleteUserAccountAsync(UserId, "senha-errada"));
    }
}
