using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface IPushSubscriptionRepository : IRepository<PushSubscription>
{
    /// <summary>Returns all active (non-deleted) subscriptions for a user.</summary>
    Task<IEnumerable<PushSubscription>> GetByUserIdAsync(string userId);

    /// <summary>Returns the subscription that matches the given endpoint URL, or null.</summary>
    Task<PushSubscription?> GetByEndpointAsync(string endpoint);

    /// <summary>Returns all active subscriptions in the system (used for broadcast).</summary>
    Task<IEnumerable<PushSubscription>> GetAllActiveAsync();
}
