using MoneyManager.Domain.Enums;

namespace MoneyManager.Web.Services;

public class DashboardSummary
{
    public decimal TotalBalance { get; set; }
    public decimal LiquidBalance { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal BudgetUsedPercentage { get; set; }
    public decimal PlannedBudget { get; set; }
    public List<AccountBalanceDto> AccountBalances { get; set; } = new();
    public List<AccountBalanceDto> CreditCardBalances { get; set; } = new();
    public List<AccountBalanceDto> InvestmentBalances { get; set; } = new();
    public List<CreditCardLimitDto> CreditCardLimits { get; set; } = new();
    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
}

public class AccountBalanceDto
{
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public AccountType Type { get; set; }
}

public class CreditCardLimitDto
{
    public string CardName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal UsedAmount { get; set; }
    public decimal AvailableAmount { get; set; }
    public decimal UsedPercentage { get; set; }
}

public class RecentTransactionDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
}
