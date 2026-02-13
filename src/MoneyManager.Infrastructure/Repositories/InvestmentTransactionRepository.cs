using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de transações de investimento usando MongoDB.
/// </summary>
public class InvestmentTransactionRepository : Repository<InvestmentTransaction>, IInvestmentTransactionRepository
{
    public InvestmentTransactionRepository(MongoContext context) : base(context, "investment_transactions")
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InvestmentTransaction>> GetByAssetIdAsync(string assetId)
    {
        var filter = Builders<InvestmentTransaction>.Filter.And(
            Builders<InvestmentTransaction>.Filter.Eq(t => t.AssetId, assetId),
            Builders<InvestmentTransaction>.Filter.Eq(t => t.IsDeleted, false)
        );
        var sort = Builders<InvestmentTransaction>.Sort.Descending(t => t.Date);
        return await Collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InvestmentTransaction>> GetByUserIdAsync(string userId, DateTime startDate, DateTime endDate)
    {
        var filter = Builders<InvestmentTransaction>.Filter.And(
            Builders<InvestmentTransaction>.Filter.Eq(t => t.UserId, userId),
            Builders<InvestmentTransaction>.Filter.Gte(t => t.Date, startDate),
            Builders<InvestmentTransaction>.Filter.Lte(t => t.Date, endDate),
            Builders<InvestmentTransaction>.Filter.Eq(t => t.IsDeleted, false)
        );
        var sort = Builders<InvestmentTransaction>.Sort.Descending(t => t.Date);
        return await Collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InvestmentTransaction>> GetByUserIdAsync(string userId)
    {
        var filter = Builders<InvestmentTransaction>.Filter.And(
            Builders<InvestmentTransaction>.Filter.Eq(t => t.UserId, userId),
            Builders<InvestmentTransaction>.Filter.Eq(t => t.IsDeleted, false)
        );
        var sort = Builders<InvestmentTransaction>.Sort.Descending(t => t.Date);
        return await Collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InvestmentTransaction>> GetByAccountIdAsync(string accountId)
    {
        var filter = Builders<InvestmentTransaction>.Filter.And(
            Builders<InvestmentTransaction>.Filter.Eq(t => t.AccountId, accountId),
            Builders<InvestmentTransaction>.Filter.Eq(t => t.IsDeleted, false)
        );
        var sort = Builders<InvestmentTransaction>.Sort.Descending(t => t.Date);
        return await Collection.Find(filter).Sort(sort).ToListAsync();
    }
}
