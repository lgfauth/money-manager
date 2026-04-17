using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class CreditCardInvoiceRepository : Repository<CreditCardInvoice>, ICreditCardInvoiceRepository
{
    public CreditCardInvoiceRepository(MongoContext context)
        : base(context, "credit_card_invoices")
    {
    }

    public async Task<IEnumerable<CreditCardInvoice>> GetByUserAsync(string userId)
    {
        var filter = Builders<CreditCardInvoice>.Filter.And(
            Builders<CreditCardInvoice>.Filter.Eq(i => i.UserId, userId),
            Builders<CreditCardInvoice>.Filter.Eq(i => i.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<CreditCardInvoice>> GetByCardAsync(string userId, string creditCardId)
    {
        var filter = Builders<CreditCardInvoice>.Filter.And(
            Builders<CreditCardInvoice>.Filter.Eq(i => i.UserId, userId),
            Builders<CreditCardInvoice>.Filter.Eq(i => i.CreditCardId, creditCardId),
            Builders<CreditCardInvoice>.Filter.Eq(i => i.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<CreditCardInvoice?> GetByCardAndReferenceAsync(string userId, string creditCardId, string referenceMonth)
    {
        var filter = Builders<CreditCardInvoice>.Filter.And(
            Builders<CreditCardInvoice>.Filter.Eq(i => i.UserId, userId),
            Builders<CreditCardInvoice>.Filter.Eq(i => i.CreditCardId, creditCardId),
            Builders<CreditCardInvoice>.Filter.Eq(i => i.ReferenceMonth, referenceMonth),
            Builders<CreditCardInvoice>.Filter.Eq(i => i.IsDeleted, false)
        );

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<CreditCardInvoice>> GetByStatusAsync(InvoiceStatus status)
    {
        var filter = Builders<CreditCardInvoice>.Filter.And(
            Builders<CreditCardInvoice>.Filter.Eq(i => i.Status, status),
            Builders<CreditCardInvoice>.Filter.Eq(i => i.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }
}
