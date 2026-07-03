using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);

    // Listagem paginada para o admin — todos os usuários ativos, com ou sem assinatura.
    Task<IEnumerable<User>> GetPagedAsync(int skip, int take);
}
