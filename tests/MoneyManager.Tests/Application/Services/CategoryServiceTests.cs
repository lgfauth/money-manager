using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Domain.Seeds;

namespace MoneyManager.Tests.Application.Services;

public class CategoryServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ICategoryService _categoryService;

    public CategoryServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _categoryService = new CategoryService(_unitOfWorkMock);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateCategory()
    {
        // Arrange
        var userId = "user123";
        var request = new CreateCategoryRequestDto
        {
            Name = "Food",
            Type = CategoryType.Expense,
            Color = "#FF5733"
        };

        var categoryRepository = Substitute.For<IRepository<Category>>();
        categoryRepository.AddAsync(Arg.Any<Category>()).Returns(x => x.Arg<Category>());

        _unitOfWorkMock.Categories.Returns(categoryRepository);

        // Act
        var result = await _categoryService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Type, result.Type);
        await categoryRepository.Received(1).AddAsync(Arg.Any<Category>());
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUserCategories()
    {
        // Arrange
        var userId = "user123";
        var categories = new List<Category>
        {
            new Category { Id = "1", UserId = userId, Name = "Food", Type = CategoryType.Expense },
            new Category { Id = "2", UserId = userId, Name = "Income", Type = CategoryType.Income }
        };

        var categoryRepository = Substitute.For<IRepository<Category>>();
        categoryRepository.GetAllAsync().Returns(categories);

        _unitOfWorkMock.Categories.Returns(categoryRepository);

        // Act
        var result = await _categoryService.GetAllAsync(userId, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    // --- Categorias padrão ---

    private IRepository<Category> CategoriesReturning(params Category[] categories)
    {
        var repository = Substitute.For<IRepository<Category>>();
        repository.GetByUserIdAsync("user123").Returns(categories);
        _unitOfWorkMock.Categories.Returns(repository);
        return repository;
    }

    [Fact]
    public async Task CreateDefaultsAsync_WithoutCategories_ShouldCreateWholeDefaultList()
    {
        // Arrange
        var repository = CategoriesReturning();

        // Act
        var result = await _categoryService.CreateDefaultsAsync("user123");

        // Assert
        Assert.Equal(DefaultCategorySeed.Categories.Count, result.CreatedCount);
        Assert.Equal(DefaultCategorySeed.Categories.Count, result.Categories.Count);
        await repository.Received(DefaultCategorySeed.Categories.Count).AddAsync(Arg.Is<Category>(c => c.UserId == "user123"));
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CreateDefaultsAsync_ShouldSkipExistingReactivateDeletedAndKeepUserCategories()
    {
        // Arrange — "ALIMENTACAO" já existe (sem acento), "Transporte & Mobilidade" foi excluída, "Pets" é do usuário
        var existing = new Category { Id = "c1", UserId = "user123", Name = "ALIMENTACAO", Type = CategoryType.Expense };
        var deleted = new Category { Id = "c2", UserId = "user123", Name = "transporte & mobilidade", Type = CategoryType.Expense, IsDeleted = true };
        var custom = new Category { Id = "c3", UserId = "user123", Name = "Pets", Type = CategoryType.Expense };
        var repository = CategoriesReturning(existing, deleted, custom);

        // Act
        var result = await _categoryService.CreateDefaultsAsync("user123");

        // Assert
        Assert.Equal(DefaultCategorySeed.Categories.Count - 1, result.CreatedCount);
        Assert.False(deleted.IsDeleted);
        await repository.Received(1).UpdateAsync(deleted);
        await repository.DidNotReceive().AddAsync(Arg.Is<Category>(c => c.Name == "Alimentação" || c.Name == "Transporte & Mobilidade"));
        Assert.Contains(result.Categories, c => c.Id == "c3");
        Assert.Equal(DefaultCategorySeed.Categories.Count + 1, result.Categories.Count);
    }

    [Fact]
    public async Task CreateDefaultsAsync_ShouldCreateSameNameForIncomeAndExpense()
    {
        // Arrange — "Crédito & Empréstimo" existe só como despesa
        var repository = CategoriesReturning(
            new Category { UserId = "user123", Name = "Crédito & Empréstimo", Type = CategoryType.Expense });

        // Act
        await _categoryService.CreateDefaultsAsync("user123");

        // Assert
        await repository.Received(1).AddAsync(Arg.Is<Category>(c => c.Name == "Crédito & Empréstimo" && c.Type == CategoryType.Income));
        await repository.DidNotReceive().AddAsync(Arg.Is<Category>(c => c.Name == "Crédito & Empréstimo" && c.Type == CategoryType.Expense));
    }

    [Fact]
    public async Task CreateDefaultsAsync_WhenAllDefaultsExist_ShouldNotSave()
    {
        // Arrange
        var repository = CategoriesReturning(DefaultCategorySeed.Categories
            .Select(c => new Category { UserId = "user123", Name = c.Name, Type = c.Type })
            .ToArray());

        // Act
        var result = await _categoryService.CreateDefaultsAsync("user123");

        // Assert
        Assert.Equal(0, result.CreatedCount);
        await repository.DidNotReceive().AddAsync(Arg.Any<Category>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
    }
}
