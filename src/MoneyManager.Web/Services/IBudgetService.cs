using MoneyManager.Domain.Entities;

namespace MoneyManager.Web.Services;

public interface IBudgetService
{
    Task<IEnumerable<Budget>> GetAllAsync();
    Task<Budget?> GetByIdAsync(string id);
    Task<Budget?> CreateAsync(MoneyManager.Application.DTOs.Request.CreateBudgetRequestDto request);
    Task<bool> UpdateAsync(string id, Budget budget);
    Task<Budget?> UpdateAsync(string month, MoneyManager.Application.DTOs.Request.CreateBudgetRequestDto request);
    Task<bool> DeleteAsync(string id);
}
