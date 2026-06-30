using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Interfaces;

public interface ISubscriptionRepository : IRepository<Subscription>
{
    // Retorna a assinatura única do usuário (1-para-1); oculta intencionalmente o GetByUserIdAsync herdado que retorna IEnumerable.
    new Task<Subscription?> GetByUserIdAsync(string userId);
    Task<Subscription?> GetByExternalSubscriptionIdAsync(string externalSubscriptionId);
    Task<IEnumerable<Subscription>> GetExpiringAsync(SubscriptionStatus status, DateTime before);
    Task<IEnumerable<Subscription>> GetAllAsync(int skip, int take);
    Task<long> CountAsync();
}
