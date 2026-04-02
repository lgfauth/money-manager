using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class PushSubscriptionRepository : Repository<PushSubscription>, IPushSubscriptionRepository
{
    public PushSubscriptionRepository(MongoContext context) : base(context, "push_subscriptions") { }

    public async Task<IEnumerable<PushSubscription>> GetByUserIdAsync(string userId)
    {
        var filter = Builders<PushSubscription>.Filter.And(
            Builders<PushSubscription>.Filter.Eq(s => s.UserId, userId),
            Builders<PushSubscription>.Filter.Eq(s => s.IsDeleted, false)
        );
        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<PushSubscription?> GetByEndpointAsync(string endpoint)
    {
        var filter = Builders<PushSubscription>.Filter.And(
            Builders<PushSubscription>.Filter.Eq(s => s.Endpoint, endpoint),
            Builders<PushSubscription>.Filter.Eq(s => s.IsDeleted, false)
        );
        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<PushSubscription>> GetAllActiveAsync()
    {
        var filter = Builders<PushSubscription>.Filter.Eq(s => s.IsDeleted, false);
        return await Collection.Find(filter).ToListAsync();
    }
}
