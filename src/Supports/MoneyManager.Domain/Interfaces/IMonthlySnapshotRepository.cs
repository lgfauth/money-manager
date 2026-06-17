using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface IMonthlySnapshotRepository : IRepository<MonthlySnapshot>
{
    Task<IEnumerable<MonthlySnapshot>> GetByUserAndMonthAsync(string userId, string referenceMonth);
    Task<MonthlySnapshot?> GetByBucketAndMonthAsync(string bucketId, string referenceMonth);
    Task<MonthlySnapshot?> GetLatestConfirmedByBucketAsync(string bucketId, string beforeMonth);
    Task<IEnumerable<MonthlySnapshot>> GetHistoryByUserAsync(string userId, int year);
}
