using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class CreditCardInvoiceImportRepository : Repository<CreditCardInvoiceImport>, ICreditCardInvoiceImportRepository
{
    public const string CollectionName = "credit_card_invoice_imports";

    private static readonly FilterDefinitionBuilder<CreditCardInvoiceImport> F = Builders<CreditCardInvoiceImport>.Filter;

    public CreditCardInvoiceImportRepository(MongoContext context)
        : base(context, CollectionName)
    {
    }

    public async Task<CreditCardInvoiceImport?> GetByIdForUserAsync(string userId, string importId)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(importId, out _))
            return null;

        var filter = F.And(
            F.Eq(i => i.Id, importId),
            F.Eq(i => i.UserId, userId),
            F.Eq(i => i.IsDeleted, false));

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<CreditCardInvoiceImport>> GetRecentByUserAsync(string userId, int limit)
    {
        var filter = F.And(
            F.Eq(i => i.UserId, userId),
            F.Eq(i => i.IsDeleted, false));

        return await Collection.Find(filter)
            .SortByDescending(i => i.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<CreditCardInvoiceImport?> GetActiveByCardAsync(string userId, string creditCardId)
    {
        var filter = F.And(
            F.Eq(i => i.UserId, userId),
            F.Eq(i => i.CreditCardId, creditCardId),
            F.In(i => i.Status, [InvoiceImportStatus.Queued, InvoiceImportStatus.Processing]),
            F.Eq(i => i.IsDeleted, false));

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<CreditCardInvoiceImport?> GetLastConfirmedByCardAsync(string userId, string creditCardId, string? referenceMonth = null)
    {
        var filter = F.And(
            F.Eq(i => i.UserId, userId),
            F.Eq(i => i.CreditCardId, creditCardId),
            F.Eq(i => i.Status, InvoiceImportStatus.Confirmed),
            F.Eq(i => i.IsDeleted, false));

        if (referenceMonth is not null)
            filter &= F.Eq(i => i.ReferenceMonth, referenceMonth);

        return await Collection.Find(filter)
            .SortByDescending(i => i.ConfirmedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<CreditCardInvoiceImport?> ClaimNextQueuedAsync(DateTime nowUtc)
    {
        var filter = F.And(
            F.Eq(i => i.Status, InvoiceImportStatus.Queued),
            F.Eq(i => i.IsDeleted, false));

        var update = Builders<CreditCardInvoiceImport>.Update
            .Set(i => i.Status, InvoiceImportStatus.Processing)
            .Set(i => i.ProcessingStartedAt, nowUtc)
            .Set(i => i.UpdatedAt, nowUtc)
            .Inc(i => i.Version, 1);

        var options = new FindOneAndUpdateOptions<CreditCardInvoiceImport>
        {
            Sort = Builders<CreditCardInvoiceImport>.Sort.Ascending(i => i.CreatedAt),
            ReturnDocument = ReturnDocument.After
        };

        return await Collection.FindOneAndUpdateAsync(filter, update, options);
    }

    public async Task<bool> AnyQueuedAsync()
    {
        var filter = F.And(
            F.Eq(i => i.Status, InvoiceImportStatus.Queued),
            F.Eq(i => i.IsDeleted, false));

        return await Collection.Find(filter).Limit(1).AnyAsync();
    }

    public async Task<IEnumerable<CreditCardInvoiceImport>> GetStuckProcessingAsync(DateTime startedBeforeUtc)
    {
        var filter = F.And(
            F.Eq(i => i.Status, InvoiceImportStatus.Processing),
            F.Lt(i => i.ProcessingStartedAt, startedBeforeUtc),
            F.Eq(i => i.IsDeleted, false));

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<CreditCardInvoiceImport>> GetQueuedCreatedBeforeAsync(DateTime createdBeforeUtc)
    {
        var filter = F.And(
            F.Eq(i => i.Status, InvoiceImportStatus.Queued),
            F.Lt(i => i.CreatedAt, createdBeforeUtc),
            F.Eq(i => i.IsDeleted, false));

        return await Collection.Find(filter).ToListAsync();
    }
}
