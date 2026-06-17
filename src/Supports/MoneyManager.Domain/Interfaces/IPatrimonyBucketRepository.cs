using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface IPatrimonyBucketRepository : IRepository<PatrimonyBucket>
{
    Task<PatrimonyBucket?> GetByUserAndTypeAsync(string userId, string type);
    Task<IEnumerable<PatrimonyBucket>> GetAllUsersWithBucketsAsync();
}
