using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
