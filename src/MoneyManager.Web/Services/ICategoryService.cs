using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Web.Services;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<IEnumerable<Category>> GetAllAsync(CategoryType type);
    Task<Category?> GetByIdAsync(string id);
    Task<Category> CreateAsync(Category category);
    Task UpdateAsync(string id, Category category);
    Task DeleteAsync(string id);
}
