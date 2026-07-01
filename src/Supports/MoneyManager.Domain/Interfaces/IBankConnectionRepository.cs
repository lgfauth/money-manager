using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface IBankConnectionRepository : IRepository<BankConnection>
{
    Task<BankConnection?> GetByUserIdAndIdAsync(string userId, string id);
    new Task<IEnumerable<BankConnection>> GetByUserIdAsync(string userId);
    Task<BankConnection?> GetByExternalConnectionIdAsync(string userId, string externalConnectionId);
    Task<IEnumerable<BankConnection>> GetAllConnectedAsync();
}
