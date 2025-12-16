namespace MoneyManager.Domain.Interfaces;

using MoneyManager.Domain.Entities;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRepository<Category> Categories { get; }
    IRepository<Account> Accounts { get; }
    IRepository<Transaction> Transactions { get; }
    IRepository<Budget> Budgets { get; }
    IRepository<RecurringTransaction> RecurringTransactions { get; }
    IRepository<UserSettings> UserSettings { get; }
    Task<int> SaveChangesAsync();
}
