using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Infrastructure.Repositories;

namespace MoneyManager.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly MongoContext _context;
    private IUserRepository? _userRepository;
    private IRepository<Category>? _categoryRepository;
    private IRepository<Account>? _accountRepository;
    private ITransactionRepository? _transactionRepository;
    private IRepository<Budget>? _budgetRepository;
    private IRepository<RecurringTransaction>? _recurringTransactionRepository;
    private IRepository<UserSettings>? _userSettingsRepository;
    private IPushSubscriptionRepository? _pushSubscriptionRepository;
    private IRepository<UserReport>? _userReportRepository;
    private ICreditCardRepository? _creditCardRepository;
    private ICreditCardInvoiceRepository? _creditCardInvoiceRepository;
    private ICreditCardTransactionRepository? _creditCardTransactionRepository;

    public UnitOfWork(MongoContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _userRepository ??= new UserRepository(_context);
    public IRepository<Category> Categories => _categoryRepository ??= new Repository<Category>(_context, "categories");
    public IRepository<Account> Accounts => _accountRepository ??= new Repository<Account>(_context, "accounts");
    public ITransactionRepository Transactions => _transactionRepository ??= new TransactionRepository(_context);
    public IRepository<Budget> Budgets => _budgetRepository ??= new Repository<Budget>(_context, "budgets");
    public IRepository<RecurringTransaction> RecurringTransactions => _recurringTransactionRepository ??= new Repository<RecurringTransaction>(_context, "recurring_transactions");
    public IRepository<UserSettings> UserSettings => _userSettingsRepository ??= new Repository<UserSettings>(_context, "user_settings");
    public IPushSubscriptionRepository PushSubscriptions => _pushSubscriptionRepository ??= new PushSubscriptionRepository(_context);
    public IRepository<UserReport> UserReports => _userReportRepository ??= new Repository<UserReport>(_context, "user_reports");
    public ICreditCardRepository CreditCards => _creditCardRepository ??= new CreditCardRepository(_context);
    public ICreditCardInvoiceRepository CreditCardInvoices => _creditCardInvoiceRepository ??= new CreditCardInvoiceRepository(_context);
    public ICreditCardTransactionRepository CreditCardTransactions => _creditCardTransactionRepository ??= new CreditCardTransactionRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        // MongoDB handles changes automatically, so this is a no-op
        // In a real scenario, you might handle transactions here
        return await Task.FromResult(0);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
