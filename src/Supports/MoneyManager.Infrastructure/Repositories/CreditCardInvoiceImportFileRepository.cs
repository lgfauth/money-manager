using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class CreditCardInvoiceImportFileRepository : Repository<CreditCardInvoiceImportFile>, ICreditCardInvoiceImportFileRepository
{
    public const string CollectionName = "credit_card_invoice_import_files";

    private static readonly FilterDefinitionBuilder<CreditCardInvoiceImportFile> F = Builders<CreditCardInvoiceImportFile>.Filter;

    public CreditCardInvoiceImportFileRepository(MongoContext context)
        : base(context, CollectionName)
    {
    }

    public async Task AddManyAsync(IEnumerable<CreditCardInvoiceImportFile> files)
    {
        var list = files.ToList();
        if (list.Count == 0)
            return;

        await Collection.InsertManyAsync(list);
    }

    public async Task<IEnumerable<CreditCardInvoiceImportFile>> GetByImportAsync(string userId, string importId)
    {
        return await Collection.Find(ByImport(userId, importId))
            .SortBy(f => f.Order)
            .ToListAsync();
    }

    public async Task<long> CountByImportAsync(string userId, string importId)
    {
        return await Collection.CountDocumentsAsync(ByImport(userId, importId));
    }

    public async Task DeleteByImportAsync(string userId, string importId)
    {
        await Collection.DeleteManyAsync(ByImport(userId, importId));
    }

    private static FilterDefinition<CreditCardInvoiceImportFile> ByImport(string userId, string importId) =>
        F.And(
            F.Eq(f => f.UserId, userId),
            F.Eq(f => f.ImportId, importId));
}
