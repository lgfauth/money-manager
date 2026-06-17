using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class PatrimonyBucketRepository : Repository<PatrimonyBucket>, IPatrimonyBucketRepository
{
    public PatrimonyBucketRepository(MongoContext context) : base(context, "patrimony_buckets") { }

    public override async Task<IEnumerable<PatrimonyBucket>> GetByUserIdAsync(string userId)
    {
        var filter = Builders<PatrimonyBucket>.Filter.And(
            Builders<PatrimonyBucket>.Filter.Eq(b => b.UserId, userId),
            Builders<PatrimonyBucket>.Filter.Eq(b => b.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<PatrimonyBucket?> GetByUserAndTypeAsync(string userId, string type)
    {
        var filter = Builders<PatrimonyBucket>.Filter.And(
            Builders<PatrimonyBucket>.Filter.Eq(b => b.UserId, userId),
            Builders<PatrimonyBucket>.Filter.Eq(b => b.Type, type),
            Builders<PatrimonyBucket>.Filter.Eq(b => b.IsDeleted, false)
        );

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<PatrimonyBucket>> GetAllUsersWithBucketsAsync()
    {
        var filter = Builders<PatrimonyBucket>.Filter.Eq(b => b.IsDeleted, false);
        return await Collection.Find(filter).ToListAsync();
    }
}
