using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(MongoContext context)
        : base(context, "transactions")
    {
    }

    public async Task<IEnumerable<Transaction>> GetByUserAndMonthAsync(string userId, int year, int month)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var filter = Builders<Transaction>.Filter.And(
            Builders<Transaction>.Filter.Eq(t => t.UserId, userId),
            Builders<Transaction>.Filter.Gte(t => t.Date, startDate),
            Builders<Transaction>.Filter.Lt(t => t.Date, endDate),
            Builders<Transaction>.Filter.Eq(t => t.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<Transaction?> GetByClientRequestIdAsync(string userId, string clientRequestId)
    {
        var filter = Builders<Transaction>.Filter.And(
            Builders<Transaction>.Filter.Eq(t => t.UserId, userId),
            Builders<Transaction>.Filter.Eq(t => t.ClientRequestId, clientRequestId),
            Builders<Transaction>.Filter.Eq(t => t.IsDeleted, false)
        );

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }
}
