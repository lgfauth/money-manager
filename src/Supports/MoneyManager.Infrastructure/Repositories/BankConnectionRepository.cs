using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class BankConnectionRepository : Repository<BankConnection>, IBankConnectionRepository
{
    public BankConnectionRepository(MongoContext context) : base(context, "bank_connections") { }

    public async Task<BankConnection?> GetByUserIdAndIdAsync(string userId, string id)
    {
        var filter = Builders<BankConnection>.Filter.And(
            Builders<BankConnection>.Filter.Eq(c => c.Id, id),
            Builders<BankConnection>.Filter.Eq(c => c.UserId, userId),
            Builders<BankConnection>.Filter.Eq(c => c.IsDeleted, false));

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public new async Task<IEnumerable<BankConnection>> GetByUserIdAsync(string userId)
    {
        var filter = Builders<BankConnection>.Filter.And(
            Builders<BankConnection>.Filter.Eq(c => c.UserId, userId),
            Builders<BankConnection>.Filter.Eq(c => c.IsDeleted, false));

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<BankConnection?> GetByExternalConnectionIdAsync(string userId, string externalConnectionId)
    {
        var filter = Builders<BankConnection>.Filter.And(
            Builders<BankConnection>.Filter.Eq(c => c.UserId, userId),
            Builders<BankConnection>.Filter.Eq(c => c.ExternalConnectionId, externalConnectionId),
            Builders<BankConnection>.Filter.Eq(c => c.IsDeleted, false));

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<BankConnection>> GetAllConnectedAsync()
    {
        var filter = Builders<BankConnection>.Filter.And(
            Builders<BankConnection>.Filter.Eq(c => c.Status, BankConnectionStatus.Connected),
            Builders<BankConnection>.Filter.Eq(c => c.IsDeleted, false));

        return await Collection.Find(filter).ToListAsync();
    }
}
