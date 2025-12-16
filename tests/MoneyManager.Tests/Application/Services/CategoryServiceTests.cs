using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

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
            Type = 1,
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
}
