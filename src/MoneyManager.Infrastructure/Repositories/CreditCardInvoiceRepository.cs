using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de faturas de cartão de crédito para MongoDB
/// </summary>
public class CreditCardInvoiceRepository : Repository<CreditCardInvoice>, ICreditCardInvoiceRepository
{
    public CreditCardInvoiceRepository(MongoContext context)
        : base(context, "credit_card_invoices")
    {
    }

    public async Task<CreditCardInvoice?> GetOpenInvoiceByAccountIdAsync(string accountId)
    {
        var filter = Builders<CreditCardInvoice>.Filter.And(
            Builders<CreditCardInvoice>.Filter.Eq(x => x.AccountId, accountId),
            Builders<CreditCardInvoice>.Filter.Eq(x => x.Status, InvoiceStatus.Open),
            Builders<CreditCardInvoice>.Filter.Eq(x => x.IsDeleted, false)
        );

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<CreditCardInvoice>> GetByAccountIdAsync(string accountId)
    {
        var filter = Builders<CreditCardInvoice>.Filter.And(
            Builders<CreditCardInvoice>.Filter.Eq(x => x.AccountId, accountId),
            Builders<CreditCardInvoice>.Filter.Eq(x => x.IsDeleted, false)
        );

        var sort = Builders<CreditCardInvoice>.Sort.Descending(x => x.PeriodEnd);

        return await Collection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<IEnumerable<CreditCardInvoice>> GetClosedUnpaidInvoicesAsync(string userId)
    {
        var filter = Builders<CreditCardInvoice>.Filter.And(
            Builders<CreditCardInvoice>.Filter.Eq(x => x.UserId, userId),
            Builders<CreditCardInvoice>.Filter.In(x => x.Status, new[] { InvoiceStatus.Closed, InvoiceStatus.PartiallyPaid }),
            Builders<CreditCardInvoice>.Filter.Eq(x => x.IsDeleted, false)
        );

        var sort = Builders<CreditCardInvoice>.Sort.Ascending(x => x.DueDate);

        return await Collection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<IEnumerable<CreditCardInvoice>> GetOverdueInvoicesAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;

        var filter = Builders<CreditCardInvoice>.Filter.And(
            Builders<CreditCardInvoice>.Filter.Eq(x => x.UserId, userId),
            Builders<CreditCardInvoice>.Filter.Or(
                Builders<CreditCardInvoice>.Filter.Eq(x => x.Status, InvoiceStatus.Overdue),
                Builders<CreditCardInvoice>.Filter.And(
                    Builders<CreditCardInvoice>.Filter.In(x => x.Status, new[] { InvoiceStatus.Closed, InvoiceStatus.PartiallyPaid }),
                    Builders<CreditCardInvoice>.Filter.Lt(x => x.DueDate, today)
                )
            ),
            Builders<CreditCardInvoice>.Filter.Eq(x => x.IsDeleted, false)
        );

        var sort = Builders<CreditCardInvoice>.Sort.Ascending(x => x.DueDate);

        return await Collection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<CreditCardInvoice?> GetByReferenceMonthAsync(string accountId, string referenceMonth)
    {
        var filter = Builders<CreditCardInvoice>.Filter.And(
            Builders<CreditCardInvoice>.Filter.Eq(x => x.AccountId, accountId),
            Builders<CreditCardInvoice>.Filter.Eq(x => x.ReferenceMonth, referenceMonth),
            Builders<CreditCardInvoice>.Filter.Eq(x => x.IsDeleted, false)
        );

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<CreditCardInvoice>> GetByPeriodAsync(string accountId, DateTime start, DateTime end)
    {
        var filter = Builders<CreditCardInvoice>.Filter.And(
            Builders<CreditCardInvoice>.Filter.Eq(x => x.AccountId, accountId),
            Builders<CreditCardInvoice>.Filter.Gte(x => x.PeriodStart, start),
            Builders<CreditCardInvoice>.Filter.Lte(x => x.PeriodEnd, end),
            Builders<CreditCardInvoice>.Filter.Eq(x => x.IsDeleted, false)
        );

        var sort = Builders<CreditCardInvoice>.Sort.Ascending(x => x.PeriodStart);

        return await Collection.Find(filter).Sort(sort).ToListAsync();
    }
}
