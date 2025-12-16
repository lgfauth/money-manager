using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Web.Services;

public class DashboardService : IDashboardService
{
    private readonly IAccountService _accountService;
    private readonly ITransactionService _transactionService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryService _categoryService;

    public DashboardService(
        IAccountService accountService,
        ITransactionService transactionService,
        IBudgetService budgetService,
        ICategoryService categoryService)
    {
        _accountService = accountService;
        _transactionService = transactionService;
        _budgetService = budgetService;
        _categoryService = categoryService;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync()
    {
        var summary = new DashboardSummary();

        try
        {
            var accounts = await _accountService.GetAllAsync();
            var transactions = await _transactionService.GetAllAsync();
            var budgets = await _budgetService.GetAllAsync();
            var categories = await _categoryService.GetAllAsync();

            var accountsList = accounts.ToList();
            var transactionsList = transactions.ToList();
            var categoriesDict = categories.ToDictionary(c => c.Id, c => c.Name);

            // Separar contas por tipo
            var liquidAccounts = accountsList.Where(a => 
                a.Type == AccountType.Checking || 
                a.Type == AccountType.Savings || 
                a.Type == AccountType.Cash).ToList();

            var creditCards = accountsList.Where(a => a.Type == AccountType.CreditCard).ToList();
            var investments = accountsList.Where(a => a.Type == AccountType.Investment).ToList();

            // Saldo líquido (soma das contas correntes, poupança e dinheiro)
            summary.LiquidBalance = liquidAccounts.Sum(a => a.Balance);

            // Saldo total (inclui investimentos, mas não cartões de crédito)
            summary.TotalBalance = liquidAccounts.Sum(a => a.Balance) + investments.Sum(a => a.Balance);

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var monthlyTransactions = transactionsList
                .Where(t => t.Date.Month == currentMonth && t.Date.Year == currentYear)
                .ToList();

            summary.MonthlyIncome = monthlyTransactions
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);

            summary.MonthlyExpenses = monthlyTransactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            var currentMonthStr = $"{currentYear}-{currentMonth:D2}";
            var currentBudget = budgets.FirstOrDefault(b => b.Month == currentMonthStr);

            if (currentBudget != null && currentBudget.Items != null && currentBudget.Items.Any())
            {
                summary.PlannedBudget = currentBudget.Items.Sum(i => i.LimitAmount);
                
                if (summary.PlannedBudget > 0)
                {
                    summary.BudgetUsedPercentage = (summary.MonthlyExpenses / summary.PlannedBudget) * 100;
                }
            }
            else
            {
                summary.PlannedBudget = 0;
                summary.BudgetUsedPercentage = 0;
            }

            // Contas líquidas (Corrente, Poupança, Dinheiro)
            summary.AccountBalances = liquidAccounts
                .Select(a => new AccountBalanceDto
                {
                    AccountName = a.Name,
                    Balance = a.Balance,
                    Type = a.Type
                })
                .OrderByDescending(a => a.Balance)
                .ToList();

            // Cartões de crédito
            summary.CreditCardBalances = creditCards
                .Select(a => new AccountBalanceDto
                {
                    AccountName = a.Name,
                    Balance = a.Balance,
                    Type = a.Type
                })
                .OrderByDescending(a => a.Balance)
                .ToList();

            // Investimentos
            summary.InvestmentBalances = investments
                .Select(a => new AccountBalanceDto
                {
                    AccountName = a.Name,
                    Balance = a.Balance,
                    Type = a.Type
                })
                .OrderByDescending(a => a.Balance)
                .ToList();

            // Calcular limite disponível dos cartões de crédito
            summary.CreditCardLimits = creditCards
                .Where(cc => cc.CreditLimit.HasValue && cc.CreditLimit.Value > 0)
                .Select(cc =>
                {
                    var usedAmount = Math.Abs(cc.Balance); // Balance negativo = valor usado
                    var creditLimit = cc.CreditLimit!.Value;
                    var availableAmount = creditLimit - usedAmount;
                    var usedPercentage = creditLimit > 0 ? (usedAmount / creditLimit) * 100 : 0;

                    return new CreditCardLimitDto
                    {
                        CardName = cc.Name,
                        CreditLimit = creditLimit,
                        UsedAmount = usedAmount,
                        AvailableAmount = availableAmount,
                        UsedPercentage = usedPercentage
                    };
                })
                .ToList();

            summary.RecentTransactions = transactionsList
                .OrderByDescending(t => t.Date)
                .Take(5)
                .Select(t => new RecentTransactionDto
                {
                    Date = t.Date,
                    Description = t.Description,
                    Category = categoriesDict.ContainsKey(t.CategoryId) 
                        ? categoriesDict[t.CategoryId] 
                        : "Sem categoria",
                    Account = accountsList.FirstOrDefault(a => a.Id == t.AccountId)?.Name ?? "Conta desconhecida",
                    Amount = t.Amount,
                    Type = t.Type.ToString()
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar dashboard: {ex.Message}");
        }

        return summary;
    }
}
