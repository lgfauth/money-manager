using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetByUserAndMonthAsync(string userId, int year, int month);
    Task<Transaction?> GetByClientRequestIdAsync(string userId, string clientRequestId);
}
