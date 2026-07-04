using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Domain.Seeds;

namespace MoneyManager.Tests.Application.Services;

public class OpenBankingCategoryMigrationServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IUserRepository _userRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICreditCardTransactionRepository _creditCardTransactionRepo;
    private readonly OpenBankingCategoryMigrationService _service;

    private const string UserId = "user1";

    public OpenBankingCategoryMigrationServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _userRepo = Substitute.For<IUserRepository>();
        _categoryRepo = Substitute.For<IRepository<Category>>();
        _transactionRepo = Substitute.For<ITransactionRepository>();
        _creditCardTransactionRepo = Substitute.For<ICreditCardTransactionRepository>();

        _unitOfWorkMock.Users.Returns(_userRepo);
        _unitOfWorkMock.Categories.Returns(_categoryRepo);
        _unitOfWorkMock.Transactions.Returns(_transactionRepo);
        _unitOfWorkMock.CreditCardTransactions.Returns(_creditCardTransactionRepo);

        _categoryRepo.AddAsync(Arg.Any<Category>()).Returns(x => x.Arg<Category>());
        _categoryRepo.UpdateAsync(Arg.Any<Category>()).Returns(x => x.Arg<Category>());
        _transactionRepo.GetByUserIdAsync(UserId).Returns(new List<Transaction>());
        _creditCardTransactionRepo.GetByUserAsync(UserId).Returns(new List<CreditCardTransaction>());

        _service = new OpenBankingCategoryMigrationService(
            _unitOfWorkMock,
            Substitute.For<ILogger<OpenBankingCategoryMigrationService>>());
    }

    [Fact]
    public async Task MigrateUser_WhenAlreadyMigrated_ShouldDoNothing()
    {
        _userRepo.GetByIdAsync(UserId).Returns(new User { Id = UserId, HasMigratedOpenBankingCategories = true });

        var result = await _service.MigrateUserToOpenBankingCategoriesAsync(UserId);

        Assert.False(result);
        await _categoryRepo.DidNotReceive().AddAsync(Arg.Any<Category>());
    }

    [Fact]
    public async Task MigrateUser_ShouldDeactivateOldCategoriesAndCreateSeed()
    {
        var user = new User { Id = UserId };
        _userRepo.GetByIdAsync(UserId).Returns(user);

        var oldCategory = new Category { Id = "cat1", UserId = UserId, Name = "Minha categoria" };
        _categoryRepo.GetByUserIdAsync(UserId).Returns(new List<Category> { oldCategory });

        var result = await _service.MigrateUserToOpenBankingCategoriesAsync(UserId);

        Assert.True(result);
        Assert.True(oldCategory.IsDeleted);
        Assert.True(user.HasMigratedOpenBankingCategories);
        await _categoryRepo.Received(OpenBankingCategorySeed.Categories.Count).AddAsync(
            Arg.Is<Category>(c => c.UserId == UserId && !string.IsNullOrEmpty(c.OpenBankingCategoryId)));
    }

    [Fact]
    public async Task MigrateUser_WithExistingOpenBankingCategory_ShouldReuseInsteadOfDuplicate()
    {
        var user = new User { Id = UserId };
        _userRepo.GetByIdAsync(UserId).Returns(user);

        // Fallback "Outros" criada antes da migração não pode ser duplicada (índice único parcial).
        var fallback = new Category
        {
            Id = "cat-outros",
            UserId = UserId,
            Name = "Outros",
            OpenBankingCategoryId = OpenBankingCategorySeed.FallbackCategoryId
        };
        _categoryRepo.GetByUserIdAsync(UserId).Returns(new List<Category> { fallback });

        await _service.MigrateUserToOpenBankingCategoriesAsync(UserId);

        await _categoryRepo.DidNotReceive().AddAsync(
            Arg.Is<Category>(c => c.OpenBankingCategoryId == OpenBankingCategorySeed.FallbackCategoryId));
        await _categoryRepo.Received(OpenBankingCategorySeed.Categories.Count - 1).AddAsync(Arg.Any<Category>());
    }

    [Fact]
    public async Task MigrateUser_SeedTypes_ShouldDeriveIncomeForIncomeAndInvestments()
    {
        var user = new User { Id = UserId };
        _userRepo.GetByIdAsync(UserId).Returns(user);
        _categoryRepo.GetByUserIdAsync(UserId).Returns(new List<Category>());

        var created = new List<Category>();
        _categoryRepo.AddAsync(Arg.Do<Category>(created.Add)).Returns(x => x.Arg<Category>());

        await _service.MigrateUserToOpenBankingCategoriesAsync(UserId);

        Assert.All(created.Where(c => c.OpenBankingCategoryId!.StartsWith("01") || c.OpenBankingCategoryId!.StartsWith("03")),
            c => Assert.Equal(CategoryType.Income, c.Type));
        Assert.All(created.Where(c => !c.OpenBankingCategoryId!.StartsWith("01") && !c.OpenBankingCategoryId!.StartsWith("03")),
            c => Assert.Equal(CategoryType.Expense, c.Type));
    }

    [Fact]
    public async Task ResolveUserCategoryId_WhenCategoryExists_ShouldReturnItsId()
    {
        _categoryRepo.GetByUserIdAsync(UserId).Returns(new List<Category>
        {
            new() { Id = "cat-edu", UserId = UserId, OpenBankingCategoryId = "07020002" }
        });

        var result = await _service.ResolveUserCategoryIdAsync(UserId, "07020002");

        Assert.Equal("cat-edu", result);
    }

    [Fact]
    public async Task ResolveUserCategoryId_WhenNotFound_ShouldCreateFallbackOutros()
    {
        _categoryRepo.GetByUserIdAsync(UserId).Returns(new List<Category>());

        var result = await _service.ResolveUserCategoryIdAsync(UserId, "07020002");

        Assert.False(string.IsNullOrEmpty(result));
        await _categoryRepo.Received(1).AddAsync(Arg.Is<Category>(c =>
            c.OpenBankingCategoryId == OpenBankingCategorySeed.FallbackCategoryId && c.Name == "Outros"));
    }

    [Fact]
    public async Task ResolveUserCategoryId_WhenFallbackIsDeleted_ShouldReactivateInsteadOfCreate()
    {
        var fallback = new Category
        {
            Id = "cat-outros",
            UserId = UserId,
            Name = "Outros",
            OpenBankingCategoryId = OpenBankingCategorySeed.FallbackCategoryId,
            IsDeleted = true
        };
        _categoryRepo.GetByUserIdAsync(UserId).Returns(new List<Category> { fallback });

        var result = await _service.ResolveUserCategoryIdAsync(UserId, "07020002");

        Assert.Equal("cat-outros", result);
        Assert.False(fallback.IsDeleted);
        await _categoryRepo.DidNotReceive().AddAsync(Arg.Any<Category>());
    }

    [Fact]
    public async Task RecategorizeExistingTransactions_ShouldOnlyUpdateUncategorizedWithOriginData()
    {
        _categoryRepo.GetByUserIdAsync(UserId).Returns(new List<Category>
        {
            new() { Id = "cat-mercado", UserId = UserId, OpenBankingCategoryId = "10000000" }
        });

        var recategorizable = new Transaction { UserId = UserId, OpenBankingCategoryId = "10000000" };
        var withoutOrigin = new Transaction { UserId = UserId };
        var alreadyCategorized = new Transaction { UserId = UserId, CategoryId = "cat-x", OpenBankingCategoryId = "10000000" };
        _transactionRepo.GetByUserIdAsync(UserId).Returns(new List<Transaction>
        {
            recategorizable, withoutOrigin, alreadyCategorized
        });

        var ccRecategorizable = new CreditCardTransaction { UserId = UserId, OpenBankingCategoryId = "10000000" };
        _creditCardTransactionRepo.GetByUserAsync(UserId).Returns(new List<CreditCardTransaction> { ccRecategorizable });

        var count = await _service.RecategorizeExistingTransactionsAsync(UserId);

        Assert.Equal(2, count);
        Assert.Equal("cat-mercado", recategorizable.CategoryId);
        Assert.Equal("cat-mercado", ccRecategorizable.CategoryId);
        Assert.Null(withoutOrigin.CategoryId);
        Assert.Equal("cat-x", alreadyCategorized.CategoryId);
    }

    [Fact]
    public async Task RecategorizeExistingTransactions_WhenCategoryMissing_ShouldFallbackToOutros()
    {
        var categories = new List<Category>
        {
            new() { Id = "cat-outros", UserId = UserId, Name = "Outros", OpenBankingCategoryId = OpenBankingCategorySeed.FallbackCategoryId }
        };
        _categoryRepo.GetByUserIdAsync(UserId).Returns(categories);

        var transaction = new Transaction { UserId = UserId, OpenBankingCategoryId = "10000000" };
        _transactionRepo.GetByUserIdAsync(UserId).Returns(new List<Transaction> { transaction });

        var count = await _service.RecategorizeExistingTransactionsAsync(UserId);

        Assert.Equal(1, count);
        Assert.Equal("cat-outros", transaction.CategoryId);
    }
}
