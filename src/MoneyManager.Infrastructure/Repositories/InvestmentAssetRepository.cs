using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de ativos de investimento usando MongoDB.
/// </summary>
public class InvestmentAssetRepository : Repository<InvestmentAsset>, IInvestmentAssetRepository
{
    public InvestmentAssetRepository(MongoContext context) : base(context, "investment_assets")
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InvestmentAsset>> GetByUserIdAsync(string userId)
    {
        var filter = Builders<InvestmentAsset>.Filter.Eq(a => a.UserId, userId);
        return await Collection.Find(filter).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InvestmentAsset>> GetByAccountIdAsync(string accountId)
    {
        var filter = Builders<InvestmentAsset>.Filter.Eq(a => a.AccountId, accountId);
        return await Collection.Find(filter).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<InvestmentAsset?> GetByTickerAsync(string userId, string ticker)
    {
        var filter = Builders<InvestmentAsset>.Filter.And(
            Builders<InvestmentAsset>.Filter.Eq(a => a.UserId, userId),
            Builders<InvestmentAsset>.Filter.Eq(a => a.Ticker, ticker),
            Builders<InvestmentAsset>.Filter.Eq(a => a.IsDeleted, false)
        );
        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InvestmentAsset>> GetActiveByUserIdAsync(string userId)
    {
        var filter = Builders<InvestmentAsset>.Filter.And(
            Builders<InvestmentAsset>.Filter.Eq(a => a.UserId, userId),
            Builders<InvestmentAsset>.Filter.Eq(a => a.IsDeleted, false)
        );
        return await Collection.Find(filter).ToListAsync();
    }
}
