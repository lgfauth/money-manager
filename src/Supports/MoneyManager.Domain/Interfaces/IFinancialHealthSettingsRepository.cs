using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface IFinancialHealthSettingsRepository : IRepository<FinancialHealthSettings>
{
    new Task<FinancialHealthSettings?> GetByUserIdAsync(string userId);
}
