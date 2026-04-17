namespace MoneyManager.Domain.Interfaces;

using MoneyManager.Domain.Entities;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRepository<Category> Categories { get; }
    IRepository<Account> Accounts { get; }
    ITransactionRepository Transactions { get; }
    IRepository<Budget> Budgets { get; }
    IRepository<RecurringTransaction> RecurringTransactions { get; }
    IRepository<UserSettings> UserSettings { get; }
    IPushSubscriptionRepository PushSubscriptions { get; }
    IRepository<UserReport> UserReports { get; }
    ICreditCardRepository CreditCards { get; }
    ICreditCardInvoiceRepository CreditCardInvoices { get; }
    ICreditCardTransactionRepository CreditCardTransactions { get; }
    Task<int> SaveChangesAsync();
}
