using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetByUserIdAsync(string userId);
    Task DeleteManyByUserIdAsync(string userId);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(string id);
}
