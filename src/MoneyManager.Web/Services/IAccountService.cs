using MoneyManager.Domain.Entities;

namespace MoneyManager.Web.Services;

public interface IAccountService
{
    Task<IEnumerable<Account>> GetAllAsync();
    Task<Account?> GetByIdAsync(string id);
    Task<Account> CreateAsync(Account account);
    Task UpdateAsync(string id, Account account);
    Task DeleteAsync(string id);
}
