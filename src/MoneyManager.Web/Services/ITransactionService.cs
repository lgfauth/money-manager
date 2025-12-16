using MoneyManager.Domain.Entities;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Web.Services;

public interface ITransactionService
{
    Task<IEnumerable<Transaction>> GetAllAsync();
    Task<Transaction?> GetByIdAsync(string id);
    Task<Transaction> CreateAsync(CreateTransactionRequestDto request);
    Task UpdateAsync(string id, CreateTransactionRequestDto request);
    Task DeleteAsync(string id);
}
