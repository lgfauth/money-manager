using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public interface ICategoryService
{
    Task<CategoryResponseDto> CreateAsync(string userId, CreateCategoryRequestDto request);
    Task<IEnumerable<CategoryResponseDto>> GetAllAsync(string userId, CategoryType? type);
    Task<CategoryResponseDto> GetByIdAsync(string userId, string id);
    Task<CategoryResponseDto> UpdateAsync(string userId, string id, CreateCategoryRequestDto request);
    Task DeleteAsync(string userId, string id);
}

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryResponseDto> CreateAsync(string userId, CreateCategoryRequestDto request)
    {
        var category = new Category
        {
            UserId = userId,
            Name = request.Name,
            Type = (CategoryType)request.Type,
            Color = request.Color
        };

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync(string userId, CategoryType? type)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        var filtered = categories.Where(c => c.UserId == userId && !c.IsDeleted);

        if (type.HasValue)
            filtered = filtered.Where(c => c.Type == type.Value);

        return filtered.Select(MapToDto);
    }

    public async Task<CategoryResponseDto> GetByIdAsync(string userId, string id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category == null || category.UserId != userId || category.IsDeleted)
            throw new KeyNotFoundException("Category not found");

        return MapToDto(category);
    }

    public async Task<CategoryResponseDto> UpdateAsync(string userId, string id, CreateCategoryRequestDto request)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category == null || category.UserId != userId || category.IsDeleted)
            throw new KeyNotFoundException("Category not found");

        category.Name = request.Name;
        category.Type = (CategoryType)request.Type;
        category.Color = request.Color;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Categories.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task DeleteAsync(string userId, string id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category == null || category.UserId != userId || category.IsDeleted)
            throw new KeyNotFoundException("Category not found");

        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Categories.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();
    }

    private static CategoryResponseDto MapToDto(Category category)
    {
        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = (int)category.Type,
            Color = category.Color,
            CreatedAt = category.CreatedAt
        };
    }
}
