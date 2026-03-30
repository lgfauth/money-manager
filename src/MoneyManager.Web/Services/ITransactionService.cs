using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;

namespace MoneyManager.Web.Services;

public interface ITransactionService
{
    Task<IEnumerable<Transaction>> GetAllAsync();
    Task<PagedResultDto<Transaction>> GetAllPagedAsync(
        int page = 1,
        int pageSize = 50,
        DateTime? startDate = null,
        DateTime? endDate = null,
        TransactionType? type = null,
        string sortBy = "date_desc");
    Task<Transaction?> GetByIdAsync(string id);
    Task<Transaction> CreateAsync(CreateTransactionRequestDto request);
    Task UpdateAsync(string id, CreateTransactionRequestDto request);
    Task DeleteAsync(string id);
}
