using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Web.Services;

public class ReportService : IReportService
{
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IAccountService _accountService;

    public ReportService(
        ITransactionService transactionService,
        ICategoryService categoryService,
        IAccountService accountService)
    {
        _transactionService = transactionService;
        _categoryService = categoryService;
        _accountService = accountService;
    }

    public async Task<ReportSummary> GetReportSummaryAsync(int months = 1)
    {
        var summary = new ReportSummary();

        try
        {
            var transactions = await _transactionService.GetAllAsync();
            var categories = await _categoryService.GetAllAsync();
            var accounts = await _accountService.GetAllAsync();

            var transactionsList = transactions.ToList();
            var categoriesDict = categories.ToDictionary(c => c.Id, c => c.Name);
            var accountsList = accounts.ToList();

            // Data de referência (últimos X meses)
            var referenceDate = DateTime.Now.AddMonths(-months);

            var periodTransactions = transactionsList
                .Where(t => t.Date >= referenceDate)
                .ToList();

            // Calcular totais
            summary.TotalIncome = periodTransactions
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);

            summary.TotalExpenses = periodTransactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            summary.NetBalance = summary.TotalIncome - summary.TotalExpenses;

            // Taxa de economia
            if (summary.TotalIncome > 0)
            {
                summary.SavingsRate = (summary.NetBalance / summary.TotalIncome) * 100;
            }

            // Despesas por categoria
            var expensesByCategory = periodTransactions
                .Where(t => t.Type == TransactionType.Expense)
                .GroupBy(t => t.CategoryId ?? "uncategorized")
                .Select(g => new
                {
                    CategoryId = g.Key,
                    Amount = g.Sum(t => t.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            var totalExpenses = expensesByCategory.Sum(x => x.Amount);

            summary.ExpensesByCategory = expensesByCategory
                .Select(x => new CategoryExpenseDto
                {
                    CategoryId = x.CategoryId,
                    CategoryName = categoriesDict.ContainsKey(x.CategoryId) 
                        ? categoriesDict[x.CategoryId] 
                        : "Sem categoria",
                    Amount = x.Amount,
                    Percentage = totalExpenses > 0 ? (x.Amount / totalExpenses) * 100 : 0
                })
                .ToList();

            // Tendências mensais (últimos 6 meses)
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-i))
                .OrderBy(d => d)
                .ToList();

            summary.MonthlyTrends = last6Months
                .Select(month =>
                {
                    var monthTransactions = transactionsList
                        .Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month)
                        .ToList();

                    var monthIncome = monthTransactions
                        .Where(t => t.Type == TransactionType.Income)
                        .Sum(t => t.Amount);

                    var monthExpenses = monthTransactions
                        .Where(t => t.Type == TransactionType.Expense)
                        .Sum(t => t.Amount);

                    return new MonthlyTrendDto
                    {
                        Month = month.ToString("MMM/yy"),
                        Income = monthIncome,
                        Expenses = monthExpenses,
                        Balance = monthIncome - monthExpenses
                    };
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar relatórios: {ex.Message}");
        }

        return summary;
    }
}
