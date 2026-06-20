using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class SubscriptionRepository : Repository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(MongoContext context) : base(context, "subscriptions") { }

    public new async Task<Subscription?> GetByUserIdAsync(string userId)
    {
        var filter = Builders<Subscription>.Filter.And(
            Builders<Subscription>.Filter.Eq(s => s.UserId, userId),
            Builders<Subscription>.Filter.Eq(s => s.IsDeleted, false));

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<Subscription?> GetByExternalSubscriptionIdAsync(string externalSubscriptionId)
    {
        var filter = Builders<Subscription>.Filter.Eq(s => s.ExternalSubscriptionId, externalSubscriptionId);
        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Subscription>> GetExpiringAsync(SubscriptionStatus status, DateTime before)
    {
        var dateFilter = status == SubscriptionStatus.PastDue
            ? Builders<Subscription>.Filter.Lt(s => s.GraceEndsAt, before)
            : status == SubscriptionStatus.Trial
                ? Builders<Subscription>.Filter.Lt(s => s.TrialEndsAt, before)
                : Builders<Subscription>.Filter.Lt(s => s.CurrentPeriodEnd, before);

        var filter = Builders<Subscription>.Filter.And(
            Builders<Subscription>.Filter.Eq(s => s.Status, status),
            dateFilter,
            Builders<Subscription>.Filter.Eq(s => s.IsDeleted, false));

        return await Collection.Find(filter).ToListAsync();
    }
}
