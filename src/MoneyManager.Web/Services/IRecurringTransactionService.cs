using MoneyManager.Domain.Entities;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Web.Services;

public interface IRecurringTransactionService
{
    Task<IEnumerable<RecurringTransaction>> GetAllAsync();
    Task<RecurringTransaction?> GetByIdAsync(string id);
    Task<RecurringTransaction> CreateAsync(CreateRecurringTransactionRequestDto request);
    Task UpdateAsync(string id, CreateRecurringTransactionRequestDto request);
    Task DeleteAsync(string id);
}
