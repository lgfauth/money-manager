using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class FinancialHealthSettingsRepository : Repository<FinancialHealthSettings>, IFinancialHealthSettingsRepository
{
    public FinancialHealthSettingsRepository(MongoContext context) : base(context, "financial_health_settings") { }

    public new async Task<FinancialHealthSettings?> GetByUserIdAsync(string userId)
    {
        var filter = Builders<FinancialHealthSettings>.Filter.And(
            Builders<FinancialHealthSettings>.Filter.Eq(s => s.UserId, userId),
            Builders<FinancialHealthSettings>.Filter.Eq(s => s.IsDeleted, false)
        );

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }
}
