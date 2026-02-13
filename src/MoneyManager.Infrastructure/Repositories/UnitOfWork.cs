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
    private IRepository<Transaction>? _transactionRepository;
    private IRepository<Budget>? _budgetRepository;
    private IRepository<RecurringTransaction>? _recurringTransactionRepository;
    private IRepository<UserSettings>? _userSettingsRepository;
    private ICreditCardInvoiceRepository? _creditCardInvoiceRepository;
    private IInvestmentAssetRepository? _investmentAssetRepository;
    private IInvestmentTransactionRepository? _investmentTransactionRepository;

    public UnitOfWork(MongoContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _userRepository ??= new UserRepository(_context);
    public IRepository<Category> Categories => _categoryRepository ??= new Repository<Category>(_context, "categories");
    public IRepository<Account> Accounts => _accountRepository ??= new Repository<Account>(_context, "accounts");
    public IRepository<Transaction> Transactions => _transactionRepository ??= new Repository<Transaction>(_context, "transactions");
    public IRepository<Budget> Budgets => _budgetRepository ??= new Repository<Budget>(_context, "budgets");
    public IRepository<RecurringTransaction> RecurringTransactions => _recurringTransactionRepository ??= new Repository<RecurringTransaction>(_context, "recurring_transactions");
    public IRepository<UserSettings> UserSettings => _userSettingsRepository ??= new Repository<UserSettings>(_context, "user_settings");
    public ICreditCardInvoiceRepository CreditCardInvoices => _creditCardInvoiceRepository ??= new CreditCardInvoiceRepository(_context);
    public IInvestmentAssetRepository InvestmentAssets => _investmentAssetRepository ??= new InvestmentAssetRepository(_context);
    public IInvestmentTransactionRepository InvestmentTransactions => _investmentTransactionRepository ??= new InvestmentTransactionRepository(_context);

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
