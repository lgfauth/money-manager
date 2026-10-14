using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class CreditCardTransactionRepository : Repository<CreditCardTransaction>, ICreditCardTransactionRepository
{
    public CreditCardTransactionRepository(MongoContext context)
        : base(context, "credit_card_transactions")
    {
    }

    public async Task<IEnumerable<CreditCardTransaction>> GetByUserAsync(string userId)
    {
        var filter = Builders<CreditCardTransaction>.Filter.And(
            Builders<CreditCardTransaction>.Filter.Eq(t => t.UserId, userId),
            Builders<CreditCardTransaction>.Filter.Eq(t => t.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<CreditCardTransaction>> GetByInvoiceAsync(string userId, string invoiceId)
    {
        var filter = Builders<CreditCardTransaction>.Filter.And(
            Builders<CreditCardTransaction>.Filter.Eq(t => t.UserId, userId),
            Builders<CreditCardTransaction>.Filter.Eq(t => t.InvoiceId, invoiceId),
            Builders<CreditCardTransaction>.Filter.Eq(t => t.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<CreditCardTransaction>> GetByCardAsync(string userId, string creditCardId)
    {
        var filter = Builders<CreditCardTransaction>.Filter.And(
            Builders<CreditCardTransaction>.Filter.Eq(t => t.UserId, userId),
            Builders<CreditCardTransaction>.Filter.Eq(t => t.CreditCardId, creditCardId),
            Builders<CreditCardTransaction>.Filter.Eq(t => t.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<CreditCardTransaction>> GetByParentAsync(string userId, string parentTransactionId)
    {
        var filter = Builders<CreditCardTransaction>.Filter.And(
            Builders<CreditCardTransaction>.Filter.Eq(t => t.UserId, userId),
            Builders<CreditCardTransaction>.Filter.Eq(t => t.ParentTransactionId, parentTransactionId),
            Builders<CreditCardTransaction>.Filter.Eq(t => t.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<CreditCardTransaction?> GetByExternalIdAsync(string userId, string externalId)
    {
        var filter = Builders<CreditCardTransaction>.Filter.And(
            Builders<CreditCardTransaction>.Filter.Eq(t => t.UserId, userId),
            Builders<CreditCardTransaction>.Filter.Eq(t => t.ExternalId, externalId),
            Builders<CreditCardTransaction>.Filter.Eq(t => t.IsDeleted, false)
        );

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    // Compras à vista só interessam a partir de fromDate; compras parceladas precisam de uma janela
    // retroativa maior (installmentsFromDate), pois suas parcelas caem meses depois da compra.
    public async Task<IEnumerable<CreditCardTransaction>> GetByUserFromPurchaseDateAsync(
        string userId, DateTime fromDate, DateTime installmentsFromDate)
    {
        var filter = Builders<CreditCardTransaction>.Filter.And(
            Builders<CreditCardTransaction>.Filter.Eq(t => t.UserId, userId),
            Builders<CreditCardTransaction>.Filter.Eq(t => t.IsDeleted, false),
            Builders<CreditCardTransaction>.Filter.Or(
                Builders<CreditCardTransaction>.Filter.Gte(t => t.PurchaseDate, fromDate.Date),
                Builders<CreditCardTransaction>.Filter.And(
                    Builders<CreditCardTransaction>.Filter.Gt(t => t.TotalInstallments, 1),
                    Builders<CreditCardTransaction>.Filter.Gte(t => t.PurchaseDate, installmentsFromDate.Date)))
        );

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<CreditCardTransaction>> GetByInvoicesAsync(string userId, IReadOnlyCollection<string> invoiceIds)
    {
        if (invoiceIds.Count == 0)
            return [];

        var filter = Builders<CreditCardTransaction>.Filter.And(
            Builders<CreditCardTransaction>.Filter.Eq(t => t.UserId, userId),
            Builders<CreditCardTransaction>.Filter.In(t => t.InvoiceId, invoiceIds),
            Builders<CreditCardTransaction>.Filter.Eq(t => t.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }
}
