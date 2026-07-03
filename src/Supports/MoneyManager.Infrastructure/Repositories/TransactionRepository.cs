using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(MongoContext context)
        : base(context, "transactions")
    {
    }

    public async Task<IEnumerable<Transaction>> GetByUserAndMonthAsync(string userId, int year, int month)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var filter = Builders<Transaction>.Filter.And(
            Builders<Transaction>.Filter.Eq(t => t.UserId, userId),
            Builders<Transaction>.Filter.Gte(t => t.Date, startDate),
            Builders<Transaction>.Filter.Lt(t => t.Date, endDate),
            Builders<Transaction>.Filter.Eq(t => t.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }

    public async Task<Transaction?> GetByClientRequestIdAsync(string userId, string clientRequestId)
    {
        var filter = Builders<Transaction>.Filter.And(
            Builders<Transaction>.Filter.Eq(t => t.UserId, userId),
            Builders<Transaction>.Filter.Eq(t => t.ClientRequestId, clientRequestId),
            Builders<Transaction>.Filter.Eq(t => t.IsDeleted, false)
        );

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedByUserAsync(
        string userId,
        int page,
        int pageSize,
        DateTime? startDate = null,
        DateTime? endDate = null,
        TransactionType? type = null,
        string? accountId = null,
        string sortBy = "date_desc")
    {
        var filters = new List<FilterDefinition<Transaction>>
        {
            Builders<Transaction>.Filter.Eq(t => t.UserId, userId),
            Builders<Transaction>.Filter.Eq(t => t.IsDeleted, false)
        };

        if (startDate.HasValue)
            filters.Add(Builders<Transaction>.Filter.Gte(t => t.Date, startDate.Value.Date));

        if (endDate.HasValue)
            filters.Add(Builders<Transaction>.Filter.Lt(t => t.Date, endDate.Value.Date.AddDays(1)));

        if (type.HasValue)
            filters.Add(Builders<Transaction>.Filter.Eq(t => t.Type, type.Value));

        if (!string.IsNullOrEmpty(accountId))
            filters.Add(Builders<Transaction>.Filter.Eq(t => t.AccountId, accountId));

        var combinedFilter = Builders<Transaction>.Filter.And(filters);

        var sortDefinition = sortBy switch
        {
            "date_asc" => Builders<Transaction>.Sort.Ascending(t => t.Date),
            "amount_asc" => Builders<Transaction>.Sort.Ascending(t => t.Amount),
            "amount_desc" => Builders<Transaction>.Sort.Descending(t => t.Amount),
            _ => Builders<Transaction>.Sort.Descending(t => t.Date)
        };

        var totalCount = await Collection.CountDocumentsAsync(combinedFilter);

        var skip = (page - 1) * pageSize;
        var items = await Collection.Find(combinedFilter)
            .Sort(sortDefinition)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();

        return (items, (int)totalCount);
    }

    // Busca transação pelo ExternalId para deduplicação no upsert.
    public async Task<Transaction?> GetByExternalIdAsync(string userId, string externalId)
    {
        var filter = Builders<Transaction>.Filter.And(
            Builders<Transaction>.Filter.Eq(t => t.UserId, userId),
            Builders<Transaction>.Filter.Eq(t => t.ExternalId, externalId),
            Builders<Transaction>.Filter.Eq(t => t.IsDeleted, false));

        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    // Retorna transações com Source = "manual" (ou sem Source) — para CleanSlate.
    public async Task<IEnumerable<Transaction>> GetManualByUserIdAsync(string userId)
    {
        var filter = Builders<Transaction>.Filter.And(
            Builders<Transaction>.Filter.Eq(t => t.UserId, userId),
            Builders<Transaction>.Filter.Eq(t => t.IsDeleted, false),
            Builders<Transaction>.Filter.Or(
                Builders<Transaction>.Filter.Eq(t => t.Source, "manual"),
                Builders<Transaction>.Filter.Exists(t => t.Source, false)));

        return await Collection.Find(filter).ToListAsync();
    }

    // Retorna data do último lançamento manual — para cálculo do CutoffDate.
    public async Task<DateTime?> GetLastManualDateAsync(string userId)
    {
        var filter = Builders<Transaction>.Filter.And(
            Builders<Transaction>.Filter.Eq(t => t.UserId, userId),
            Builders<Transaction>.Filter.Eq(t => t.IsDeleted, false),
            Builders<Transaction>.Filter.Or(
                Builders<Transaction>.Filter.Eq(t => t.Source, "manual"),
                Builders<Transaction>.Filter.Exists(t => t.Source, false)));

        var last = await Collection.Find(filter)
            .SortByDescending(t => t.Date)
            .FirstOrDefaultAsync();

        return last?.Date;
    }
}
