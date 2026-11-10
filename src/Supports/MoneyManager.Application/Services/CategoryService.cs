using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Domain.Seeds;

namespace MoneyManager.Application.Services;

public interface ICategoryService
{
    Task<CategoryResponseDto> CreateAsync(string userId, CreateCategoryRequestDto request);
    Task<IEnumerable<CategoryResponseDto>> GetAllAsync(string userId, CategoryType? type);
    Task<CategoryResponseDto> GetByIdAsync(string userId, string id);
    Task<CategoryResponseDto> UpdateAsync(string userId, string id, CreateCategoryRequestDto request);
    Task DeleteAsync(string userId, string id);

    /// <summary>
    /// Cria as categorias da lista padrão que o usuário ainda não tem (mesmo nome e tipo, sem
    /// diferenciar acentos e maiúsculas). Categorias padrão excluídas antes são reativadas. Idempotente.
    /// </summary>
    Task<CreateDefaultCategoriesResponseDto> CreateDefaultsAsync(string userId);
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
            Type = request.Type,
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
        category.Type = request.Type;
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

    public async Task<CreateDefaultCategoriesResponseDto> CreateDefaultsAsync(string userId)
    {
        var categories = (await _unitOfWork.Categories.GetByUserIdAsync(userId))
            .Where(c => c.UserId == userId)
            .ToList();

        static string Key(string name, CategoryType type) => $"{(int)type}:{InvoiceImportText.Normalize(name)}";

        var active = categories.Where(c => !c.IsDeleted).Select(c => Key(c.Name, c.Type)).ToHashSet();
        var deleted = categories
            .Where(c => c.IsDeleted)
            .GroupBy(c => Key(c.Name, c.Type))
            .ToDictionary(g => g.Key, g => g.First());

        var createdCount = 0;
        foreach (var seed in DefaultCategorySeed.Categories)
        {
            var key = Key(seed.Name, seed.Type);
            if (!active.Add(key))
                continue;

            if (deleted.TryGetValue(key, out var previous))
            {
                previous.IsDeleted = false;
                previous.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Categories.UpdateAsync(previous);
            }
            else
            {
                var category = new Category
                {
                    UserId = userId,
                    Name = seed.Name,
                    Type = seed.Type,
                    Color = seed.Color
                };
                await _unitOfWork.Categories.AddAsync(category);
                categories.Add(category);
            }

            createdCount++;
        }

        if (createdCount > 0)
            await _unitOfWork.SaveChangesAsync();

        return new CreateDefaultCategoriesResponseDto
        {
            CreatedCount = createdCount,
            Categories = categories.Where(c => !c.IsDeleted).Select(MapToDto).ToList()
        };
    }

    private static CategoryResponseDto MapToDto(Category category)
    {
        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            Color = category.Color,
            CreatedAt = category.CreatedAt
        };
    }
}
