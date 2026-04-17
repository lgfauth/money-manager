using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class CreditCardRepository : Repository<CreditCard>, ICreditCardRepository
{
    public CreditCardRepository(MongoContext context)
        : base(context, "credit_cards")
    {
    }

    public async Task<IEnumerable<CreditCard>> GetByUserAsync(string userId)
    {
        var filter = Builders<CreditCard>.Filter.And(
            Builders<CreditCard>.Filter.Eq(c => c.UserId, userId),
            Builders<CreditCard>.Filter.Eq(c => c.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }
}
