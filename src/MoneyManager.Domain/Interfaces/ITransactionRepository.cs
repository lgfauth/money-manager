using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetByUserAndMonthAsync(string userId, int year, int month);
    Task<Transaction?> GetByClientRequestIdAsync(string userId, string clientRequestId);
    Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedByUserAsync(
        string userId,
        int page,
        int pageSize,
        DateTime? startDate = null,
        DateTime? endDate = null,
        TransactionType? type = null,
        string sortBy = "date_desc");
}
