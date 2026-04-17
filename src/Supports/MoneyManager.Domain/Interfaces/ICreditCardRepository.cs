using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface ICreditCardRepository : IRepository<CreditCard>
{
    Task<IEnumerable<CreditCard>> GetByUserAsync(string userId);
}
