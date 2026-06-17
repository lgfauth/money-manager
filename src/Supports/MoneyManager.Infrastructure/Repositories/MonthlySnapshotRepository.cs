using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class MonthlySnapshotRepository : Repository<MonthlySnapshot>, IMonthlySnapshotRepository
{
    public MonthlySnapshotRepository(MongoContext context) : base(context, "monthly_snapshots") { }

    public async Task<IEnumerable<MonthlySnapshot>> GetByUserAndMonthAsync(string userId, string referenceMonth)
    {
        var filter = Builders<MonthlySnapshot>.Filter.And(
            Builders<MonthlySnapshot>.Filter.Eq(s => s.UserId, userId),
            Builders<MonthlySnapshot>.Filter.Eq(s => s.ReferenceMonth, referenceMonth),
            Builders<MonthlySnapshot>.Filter.Eq(s => s.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<MonthlySnapshot?> GetByBucketAndMonthAsync(string bucketId, string referenceMonth)
    {
        var filter = Builders<MonthlySnapshot>.Filter.And(
            Builders<MonthlySnapshot>.Filter.Eq(s => s.BucketId, bucketId),
            Builders<MonthlySnapshot>.Filter.Eq(s => s.ReferenceMonth, referenceMonth),
            Builders<MonthlySnapshot>.Filter.Eq(s => s.IsDeleted, false)
        );

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<MonthlySnapshot?> GetLatestConfirmedByBucketAsync(string bucketId, string beforeMonth)
    {
        var filter = Builders<MonthlySnapshot>.Filter.And(
            Builders<MonthlySnapshot>.Filter.Eq(s => s.BucketId, bucketId),
            Builders<MonthlySnapshot>.Filter.Lt(s => s.ReferenceMonth, beforeMonth),
            Builders<MonthlySnapshot>.Filter.Eq(s => s.IsDeleted, false)
        );

        return await Collection.Find(filter)
            .SortByDescending(s => s.ReferenceMonth)
            .Limit(1)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<MonthlySnapshot>> GetHistoryByUserAsync(string userId, int year)
    {
        var yearPrefix = year.ToString();
        var filter = Builders<MonthlySnapshot>.Filter.And(
            Builders<MonthlySnapshot>.Filter.Eq(s => s.UserId, userId),
            Builders<MonthlySnapshot>.Filter.Regex(s => s.ReferenceMonth, new MongoDB.Bson.BsonRegularExpression($"^{yearPrefix}")),
            Builders<MonthlySnapshot>.Filter.Eq(s => s.IsDeleted, false)
        );

        return await Collection.Find(filter)
            .SortByDescending(s => s.ReferenceMonth)
            .ToListAsync();
    }
}
