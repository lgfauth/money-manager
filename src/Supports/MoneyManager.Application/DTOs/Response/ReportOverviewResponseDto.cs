using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.DTOs.Response;

public class ReportOverviewResponseDto
{
    public ReportBasis Basis { get; set; }
    public string Currency { get; set; } = "BRL";
    public ReportPeriodDto Period { get; set; } = new();
    public ReportPeriodDto PreviousPeriod { get; set; } = new();
    public ReportTotalsDto Totals { get; set; } = new();
    public ReportTotalsDto PreviousTotals { get; set; } = new();
    public List<ReportCategoryBreakdownDto> ExpensesByCategory { get; set; } = [];
    public List<ReportCategoryBreakdownDto> IncomesByCategory { get; set; } = [];
    public List<ReportMonthlyTrendDto> MonthlyTrends { get; set; } = [];
    public List<ReportDailyPointDto> DailyPoints { get; set; } = [];
    public List<ReportAccountBreakdownDto> ByAccount { get; set; } = [];
    public List<ReportCreditCardBreakdownDto> ByCreditCard { get; set; } = [];
    public List<ReportTopExpenseDto> TopExpenses { get; set; } = [];
    public List<ReportUpcomingInstallmentDto> UpcomingInstallments { get; set; } = [];
    public List<ReportCurrencyTotalsDto> OtherCurrencies { get; set; } = [];
    public ReportDataQualityDto DataQuality { get; set; } = new();
}

public class ReportPeriodDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class ReportTotalsDto
{
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal NetBalance { get; set; }
    public decimal? SavingsRate { get; set; }
}

public class ReportCategoryBreakdownDto
{
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal Percentage { get; set; }
    public decimal PreviousTotal { get; set; }
    public decimal MonthlyAverage { get; set; }
    public decimal? BudgetLimit { get; set; }
    public int TransactionCount { get; set; }
}

public class ReportMonthlyTrendDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal NetBalance { get; set; }
}

public class ReportDailyPointDto
{
    public DateTime Date { get; set; }
    public decimal Balance { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

public class ReportAccountBreakdownDto
{
    public string AccountId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

public class ReportCreditCardBreakdownDto
{
    public string CreditCardId { get; set; } = string.Empty;
    public string CreditCardName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int TransactionCount { get; set; }
}

public class ReportTopExpenseDto
{
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string? InstallmentLabel { get; set; }
}

public class ReportUpcomingInstallmentDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int InstallmentCount { get; set; }
}

public class ReportCurrencyTotalsDto
{
    public string Currency { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

public class ReportDataQualityDto
{
    public int UncategorizedCount { get; set; }
    public decimal UncategorizedTotal { get; set; }
    public int PendingCardTransactionsCount { get; set; }
    public int ExcludedTransfersCount { get; set; }
    public int ExcludedInvoicePaymentsCount { get; set; }
    public int OtherCurrencyEntriesCount { get; set; }
}
