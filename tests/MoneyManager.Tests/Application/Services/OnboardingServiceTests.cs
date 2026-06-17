using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace MoneyManager.Tests.Application.Services;

public class OnboardingServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepo;
    private readonly IRepository<Account> _accountRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<Budget> _budgetRepo;
    private readonly IRepository<RecurringTransaction> _recurringRepo;
    private readonly OnboardingService _service;

    private const string UserId = "user-abc";

    public OnboardingServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _userRepo = Substitute.For<IUserRepository>();
        _accountRepo = Substitute.For<IRepository<Account>>();
        _categoryRepo = Substitute.For<IRepository<Category>>();
        _budgetRepo = Substitute.For<IRepository<Budget>>();
        _recurringRepo = Substitute.For<IRepository<RecurringTransaction>>();

        _unitOfWork.Users.Returns(_userRepo);
        _unitOfWork.Accounts.Returns(_accountRepo);
        _unitOfWork.Categories.Returns(_categoryRepo);
        _unitOfWork.Budgets.Returns(_budgetRepo);
        _unitOfWork.RecurringTransactions.Returns(_recurringRepo);

        _service = new OnboardingService(_unitOfWork);
    }

    // --- GetOnboardingStatusAsync ---

    [Fact]
    public async Task GetOnboardingStatusAsync_WhenNoStepsCompleted_ShouldReturnZeroPercent()
    {
        // Arrange
        var user = new User { Id = UserId, Name = "Teste" };
        _userRepo.GetByIdAsync(UserId).Returns(user);
        _accountRepo.GetAllAsync().Returns([]);
        _categoryRepo.GetAllAsync().Returns([]);
        _budgetRepo.GetAllAsync().Returns([]);
        _recurringRepo.GetAllAsync().Returns([]);

        // Act
        var result = await _service.GetOnboardingStatusAsync(UserId);

        // Assert
        Assert.False(result.IsCompleted);
        Assert.Equal(0, result.CompletionPercentage);
        Assert.Equal(4, result.PendingSteps.Count);
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_WhenAllStepsCompleted_ShouldReturnHundredPercent()
    {
        // Arrange
        var user = new User { Id = UserId, Name = "Teste" };
        _userRepo.GetByIdAsync(UserId).Returns(user);
        _accountRepo.GetAllAsync().Returns([new Account { UserId = UserId, IsDeleted = false }]);
        _categoryRepo.GetAllAsync().Returns([new Category { UserId = UserId, IsDeleted = false }]);
        _budgetRepo.GetAllAsync().Returns([new Budget { UserId = UserId }]);
        _recurringRepo.GetAllAsync().Returns([new RecurringTransaction { UserId = UserId, IsDeleted = false }]);

        // Act
        var result = await _service.GetOnboardingStatusAsync(UserId);

        // Assert
        Assert.True(result.IsCompleted);
        Assert.Equal(100, result.CompletionPercentage);
        Assert.Empty(result.PendingSteps);
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_WhenUserNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _userRepo.GetByIdAsync(UserId).Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetOnboardingStatusAsync(UserId));
    }

    [Fact]
    public async Task GetOnboardingStatusAsync_WhenOnlyAccountCreated_ShouldReturnTwentyFivePercent()
    {
        // Arrange
        var user = new User { Id = UserId, Name = "Teste" };
        _userRepo.GetByIdAsync(UserId).Returns(user);
        _accountRepo.GetAllAsync().Returns([new Account { UserId = UserId, IsDeleted = false }]);
        _categoryRepo.GetAllAsync().Returns([]);
        _budgetRepo.GetAllAsync().Returns([]);
        _recurringRepo.GetAllAsync().Returns([]);

        // Act
        var result = await _service.GetOnboardingStatusAsync(UserId);

        // Assert
        Assert.Equal(25, result.CompletionPercentage);
        Assert.True(result.HasAccounts);
        Assert.False(result.HasCategories);
        Assert.Equal(3, result.PendingSteps.Count);
    }

    // --- GetCategorySuggestionsAsync ---

    [Fact]
    public async Task GetCategorySuggestionsAsync_ShouldReturnDefaultSuggestions()
    {
        // Act
        var result = await _service.GetCategorySuggestionsAsync();

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, s => s.Name == "Salário");
        Assert.Contains(result, s => s.Name == "Alimentação");
    }

    // --- CompleteOnboardingAsync ---

    [Fact]
    public async Task CompleteOnboardingAsync_WhenUserExists_ShouldComplete()
    {
        // Arrange
        var user = new User { Id = UserId, Name = "Teste" };
        _userRepo.GetByIdAsync(UserId).Returns(user);

        // Act — sem exceção esperada
        await _service.CompleteOnboardingAsync(UserId);
    }

    [Fact]
    public async Task CompleteOnboardingAsync_WhenUserNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _userRepo.GetByIdAsync(UserId).Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CompleteOnboardingAsync(UserId));
    }
}
