using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public interface IReportService
{
    Task<SummaryResponseDto> GetMonthlySummaryAsync(string userId, string month);
    Task<IEnumerable<(string CategoryId, decimal Total)>> GetExpensesByCategoryAsync(string userId, string month);
    Task<ReportOverviewResponseDto> GetOverviewAsync(string userId, ReportOverviewRequestDto request);
}

public class ReportService : IReportService
{
    private const string DefaultCurrency = "BRL";
    private const string UncategorizedId = "uncategorized";
    private const string UncategorizedName = "Sem categoria";
    private const string UncategorizedColor = "#64748b";
    private const int TopExpensesLimit = 10;
    private const int UpcomingInstallmentMonthsLimit = 12;

    // Janela retroativa para encontrar compras parceladas cujas parcelas ainda caem no período.
    private const int MaxInstallmentLookbackMonths = 48;

    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SummaryResponseDto> GetMonthlySummaryAsync(string userId, string month)
    {
        var transactions = await _unitOfWork.Transactions.GetAllAsync();
        var monthTransactions = transactions
            .Where(t => t.UserId == userId &&
                        t.Date.Year.ToString("D4") + "-" + t.Date.Month.ToString("D2") == month &&
                        !t.IsDeleted)
            .ToList();

        var totalIncome = monthTransactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = monthTransactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        return new SummaryResponseDto
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = totalIncome - totalExpense,
            ByCurrency = monthTransactions
                .GroupBy(t => t.Currency)
                .Select(g => new CurrencySummaryDto
                {
                    Currency = g.Key,
                    TotalIncome = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                    TotalExpense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
                    Balance = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount)
                          - g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
                })
                .ToList()
        };
    }

    public async Task<IEnumerable<(string CategoryId, decimal Total)>> GetExpensesByCategoryAsync(string userId, string month)
    {
        var transactions = await _unitOfWork.Transactions.GetAllAsync();
        var monthTransactions = transactions
            .Where(t => t.UserId == userId &&
                        t.Date.Year.ToString("D4") + "-" + t.Date.Month.ToString("D2") == month &&
                        t.Type == TransactionType.Expense &&
                        !t.IsDeleted)
            .ToList();

        return monthTransactions
            .GroupBy(t => t.CategoryId ?? "uncategorized")
            .Select(g => (g.Key, g.Sum(t => t.Amount)))
            .ToList();
    }

    public async Task<ReportOverviewResponseDto> GetOverviewAsync(string userId, ReportOverviewRequestDto request)
    {
        if (request.EndDate.Date < request.StartDate.Date)
            throw new ArgumentException("A data final deve ser igual ou posterior à data inicial.");

        var today = DateTime.UtcNow.Date;
        var period = ResolveEffectivePeriod(request, today);
        var previousPeriod = ResolvePreviousPeriod(period, request.EndDate.Date);
        var sourceData = await LoadSourceDataAsync(userId, previousPeriod.Start, today);

        var reportWindow = new ReportRange(previousPeriod.Start, period.End);
        var classifiedTransactions = ClassifyAccountTransactions(sourceData);
        var entries = (await BuildEntriesAsync(userId, request.Basis, sourceData, classifiedTransactions, reportWindow))
            .Where(entry => reportWindow.Contains(entry.Date))
            .ToList();

        var primaryEntries = entries.Where(entry => entry.Currency == sourceData.Currency).ToList();
        var currentEntries = primaryEntries.Where(entry => period.Contains(entry.Date)).ToList();
        var previousEntries = primaryEntries.Where(entry => previousPeriod.Contains(entry.Date)).ToList();
        var otherCurrencyEntries = entries
            .Where(entry => entry.Currency != sourceData.Currency && period.Contains(entry.Date))
            .ToList();

        var monthCount = CountMonths(period.Start, period.End);
        var budgetLimits = SumBudgetLimitsByCategory(sourceData.Budgets, period);

        return new ReportOverviewResponseDto
        {
            Basis = request.Basis,
            Currency = sourceData.Currency,
            Period = new ReportPeriodDto { StartDate = period.Start, EndDate = period.End },
            PreviousPeriod = new ReportPeriodDto { StartDate = previousPeriod.Start, EndDate = previousPeriod.End },
            Totals = BuildTotals(currentEntries),
            PreviousTotals = BuildTotals(previousEntries),
            ExpensesByCategory = BuildCategoryBreakdown(currentEntries, previousEntries, false, sourceData.Categories, monthCount, budgetLimits),
            IncomesByCategory = BuildCategoryBreakdown(currentEntries, previousEntries, true, sourceData.Categories, monthCount, null),
            MonthlyTrends = BuildMonthlyTrends(currentEntries, period),
            DailyPoints = BuildDailyPoints(currentEntries, sourceData, period),
            ByAccount = BuildAccountBreakdown(currentEntries, sourceData.Accounts),
            ByCreditCard = BuildCreditCardBreakdown(currentEntries, sourceData.Cards),
            TopExpenses = BuildTopExpenses(currentEntries, sourceData),
            UpcomingInstallments = BuildUpcomingInstallments(sourceData, today),
            OtherCurrencies = BuildOtherCurrencies(otherCurrencyEntries),
            DataQuality = BuildDataQuality(currentEntries, classifiedTransactions, period, otherCurrencyEntries.Count)
        };
    }

    // Períodos que avançam além de hoje são cortados em hoje, para que a comparação com o
    // período anterior use a mesma quantidade de dias (ex.: 1–13/out vs 1–13/set).
    private static ReportRange ResolveEffectivePeriod(ReportOverviewRequestDto request, DateTime today)
    {
        var start = request.StartDate.Date;
        var end = request.EndDate.Date;

        if (start <= today && end > today)
            end = today;

        return new ReportRange(start, end);
    }

    private static ReportRange ResolvePreviousPeriod(ReportRange period, DateTime requestedEnd)
    {
        if (period.Start.Day == 1)
        {
            var monthSpan = CountMonths(period.Start, requestedEnd);
            var previousEnd = period.End.AddMonths(-monthSpan);

            if (IsLastDayOfMonth(period.End))
                previousEnd = new DateTime(previousEnd.Year, previousEnd.Month, DateTime.DaysInMonth(previousEnd.Year, previousEnd.Month));

            return new ReportRange(period.Start.AddMonths(-monthSpan), previousEnd);
        }

        var dayCount = (period.End - period.Start).Days + 1;
        var previousPeriodEnd = period.Start.AddDays(-1);
        return new ReportRange(previousPeriodEnd.AddDays(1 - dayCount), previousPeriodEnd);
    }

    // As consultas são independentes entre si e rodam em paralelo.
    private async Task<ReportSourceData> LoadSourceDataAsync(string userId, DateTime fromDate, DateTime today)
    {
        var installmentLookbackStart = (fromDate < today ? fromDate : today).AddMonths(-MaxInstallmentLookbackMonths);

        var settingsTask = _unitOfWork.UserSettings.GetByUserIdAsync(userId);
        var accountsTask = _unitOfWork.Accounts.GetByUserIdAsync(userId);
        var cardsTask = _unitOfWork.CreditCards.GetByUserAsync(userId);
        var categoriesTask = _unitOfWork.Categories.GetByUserIdAsync(userId);
        var budgetsTask = _unitOfWork.Budgets.GetByUserIdAsync(userId);
        var invoicesTask = _unitOfWork.CreditCardInvoices.GetByUserAsync(userId);
        var accountTransactionsTask = _unitOfWork.Transactions.GetByUserFromDateAsync(userId, fromDate);
        var cardItemsTask = _unitOfWork.CreditCardTransactions.GetByUserFromPurchaseDateAsync(
            userId, fromDate, installmentLookbackStart);

        await Task.WhenAll(
            settingsTask, accountsTask, cardsTask, categoriesTask,
            budgetsTask, invoicesTask, accountTransactionsTask, cardItemsTask);

        // As tarefas já terminaram; o await apenas extrai os resultados.
        var settings = (await settingsTask).FirstOrDefault();

        return new ReportSourceData(
            string.IsNullOrWhiteSpace(settings?.Currency) ? DefaultCurrency : settings.Currency,
            (await accountsTask).Where(account => !account.IsDeleted).ToDictionary(account => account.Id),
            (await cardsTask).Where(card => !card.IsDeleted).ToDictionary(card => card.Id),
            (await categoriesTask).ToDictionary(category => category.Id),
            (await budgetsTask).Where(budget => !budget.IsDeleted).ToList(),
            (await invoicesTask).ToList(),
            (await accountTransactionsTask).ToList(),
            (await cardItemsTask).ToList());
    }

    private static List<ClassifiedTransaction> ClassifyAccountTransactions(ReportSourceData sourceData)
    {
        var invoicePaymentTransactionIds = sourceData.Invoices
            .Where(invoice => !string.IsNullOrEmpty(invoice.PaymentTransactionId))
            .Select(invoice => invoice.PaymentTransactionId!)
            .ToHashSet();
        var hasCreditCards = sourceData.Cards.Count > 0;

        return sourceData.AccountTransactions
            .Where(transaction => sourceData.Accounts.ContainsKey(transaction.AccountId))
            .Select(transaction => new ClassifiedTransaction(
                transaction,
                ReportEntryBuilder.ClassifyAccountTransaction(transaction, invoicePaymentTransactionIds, hasCreditCards)))
            .ToList();
    }

    private async Task<List<ReportEntry>> BuildEntriesAsync(
        string userId,
        ReportBasis basis,
        ReportSourceData sourceData,
        IEnumerable<ClassifiedTransaction> classifiedTransactions,
        ReportRange reportWindow)
    {
        var entries = classifiedTransactions
            .Where(classified => classified.Kind is AccountTransactionKind.Income or AccountTransactionKind.Expense)
            .Select(classified => ReportEntryBuilder.CreateAccountEntry(classified.Transaction, classified.Kind))
            .ToList();

        if (basis == ReportBasis.Accrual)
        {
            entries.AddRange(ReportEntryBuilder.BuildAccrualCardEntries(sourceData.AccrualCardItems, sourceData.Cards));
            return entries;
        }

        var paidInvoices = sourceData.Invoices
            .Where(invoice => invoice.Status == InvoiceStatus.Paid
                              && reportWindow.Contains(ReportEntryBuilder.GetInvoicePaymentDate(invoice)))
            .ToList();
        var invoiceItems = await _unitOfWork.CreditCardTransactions.GetByInvoicesAsync(
            userId,
            paidInvoices.Select(invoice => invoice.Id).ToList());

        entries.AddRange(ReportEntryBuilder.BuildCashCardEntries(
            paidInvoices,
            invoiceItems.ToLookup(item => item.InvoiceId),
            sourceData.Cards));

        return entries;
    }

    private static ReportTotalsDto BuildTotals(IReadOnlyCollection<ReportEntry> entries)
    {
        var income = entries.Where(entry => entry.IsIncome).Sum(entry => entry.Amount);
        var expense = entries.Where(entry => !entry.IsIncome).Sum(entry => entry.Amount);

        return new ReportTotalsDto
        {
            Income = income,
            Expense = expense,
            NetBalance = income - expense,
            SavingsRate = income > 0 ? Math.Round((income - expense) / income * 100, 2) : null
        };
    }

    private static List<ReportCategoryBreakdownDto> BuildCategoryBreakdown(
        IReadOnlyCollection<ReportEntry> currentEntries,
        IReadOnlyCollection<ReportEntry> previousEntries,
        bool isIncome,
        IReadOnlyDictionary<string, Category> categories,
        int monthCount,
        IReadOnlyDictionary<string, decimal>? budgetLimits)
    {
        var currentByCategory = currentEntries
            .Where(entry => entry.IsIncome == isIncome)
            .GroupBy(entry => ResolveCategoryKey(entry.CategoryId, categories))
            .ToList();
        var previousTotals = previousEntries
            .Where(entry => entry.IsIncome == isIncome)
            .GroupBy(entry => ResolveCategoryKey(entry.CategoryId, categories))
            .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Amount));
        var grandTotal = currentByCategory.Sum(group => group.Sum(entry => entry.Amount));

        return currentByCategory
            .Select(group =>
            {
                var total = group.Sum(entry => entry.Amount);
                var (name, color) = ResolveCategoryDisplay(group.Key, categories);

                return new ReportCategoryBreakdownDto
                {
                    CategoryId = group.Key,
                    CategoryName = name,
                    Color = color,
                    Total = total,
                    Percentage = grandTotal > 0 ? Math.Round(total / grandTotal * 100, 2) : 0,
                    PreviousTotal = previousTotals.GetValueOrDefault(group.Key),
                    MonthlyAverage = Math.Round(total / monthCount, 2),
                    BudgetLimit = budgetLimits is not null && budgetLimits.TryGetValue(group.Key, out var limit) ? limit : null,
                    TransactionCount = group.Count()
                };
            })
            .Where(category => category.Total != 0)
            .OrderByDescending(category => category.Total)
            .ToList();
    }

    private static List<ReportMonthlyTrendDto> BuildMonthlyTrends(IReadOnlyCollection<ReportEntry> entries, ReportRange period)
    {
        var entriesByMonth = entries.ToLookup(entry => FormatMonth(entry.Date));

        return EnumerateMonths(period)
            .Select(month =>
            {
                var income = entriesByMonth[month].Where(entry => entry.IsIncome).Sum(entry => entry.Amount);
                var expense = entriesByMonth[month].Where(entry => !entry.IsIncome).Sum(entry => entry.Amount);

                return new ReportMonthlyTrendDto
                {
                    Month = month,
                    Income = income,
                    Expense = expense,
                    NetBalance = income - expense
                };
            })
            .ToList();
    }

    /// <summary>
    /// Reconstrói o saldo diário das contas partindo do saldo atual e desfazendo, dia a dia,
    /// o efeito das transações posteriores — mesma regra de impacto aplicada na criação da transação.
    /// </summary>
    private static List<ReportDailyPointDto> BuildDailyPoints(
        IReadOnlyCollection<ReportEntry> entries,
        ReportSourceData sourceData,
        ReportRange period)
    {
        var trackedAccounts = sourceData.Accounts.Values
            .Where(account => account.Currency == sourceData.Currency)
            .ToDictionary(account => account.Id);
        var balanceEffectByDay = new Dictionary<DateTime, decimal>();

        void AddBalanceEffect(string? accountId, DateTime date, decimal effect)
        {
            if (accountId is null || !trackedAccounts.ContainsKey(accountId))
                return;

            balanceEffectByDay[date.Date] = balanceEffectByDay.GetValueOrDefault(date.Date) + effect;
        }

        foreach (var transaction in sourceData.AccountTransactions)
        {
            var effect = transaction.Type == TransactionType.Income ? transaction.Amount : -transaction.Amount;
            AddBalanceEffect(transaction.AccountId, transaction.Date, effect);

            if (transaction.Type == TransactionType.Transfer)
                AddBalanceEffect(transaction.ToAccountId, transaction.Date, transaction.Amount);
        }

        var currentBalance = trackedAccounts.Values.Sum(account => account.Balance);
        var effectAfterPeriod = balanceEffectByDay
            .Where(effect => effect.Key > period.End)
            .Sum(effect => effect.Value);
        var runningBalance = currentBalance - effectAfterPeriod;

        var incomeByDay = SumByDay(entries.Where(entry => entry.IsIncome));
        var expenseByDay = SumByDay(entries.Where(entry => !entry.IsIncome));
        var points = new List<ReportDailyPointDto>();

        for (var day = period.End; day >= period.Start; day = day.AddDays(-1))
        {
            points.Add(new ReportDailyPointDto
            {
                Date = day,
                Balance = runningBalance,
                Income = incomeByDay.GetValueOrDefault(day),
                Expense = expenseByDay.GetValueOrDefault(day)
            });

            runningBalance -= balanceEffectByDay.GetValueOrDefault(day);
        }

        points.Reverse();
        return points;
    }

    private static List<ReportAccountBreakdownDto> BuildAccountBreakdown(
        IReadOnlyCollection<ReportEntry> entries,
        IReadOnlyDictionary<string, Account> accounts)
    {
        return entries
            .Where(entry => entry.Source == ReportEntrySource.Account)
            .GroupBy(entry => entry.SourceId)
            .Select(group =>
            {
                var account = accounts[group.Key];
                return new ReportAccountBreakdownDto
                {
                    AccountId = account.Id,
                    AccountName = account.Name,
                    Color = account.Color,
                    Income = group.Where(entry => entry.IsIncome).Sum(entry => entry.Amount),
                    Expense = group.Where(entry => !entry.IsIncome).Sum(entry => entry.Amount)
                };
            })
            .OrderByDescending(account => account.Income + account.Expense)
            .ToList();
    }

    private static List<ReportCreditCardBreakdownDto> BuildCreditCardBreakdown(
        IReadOnlyCollection<ReportEntry> entries,
        IReadOnlyDictionary<string, CreditCard> cards)
    {
        return entries
            .Where(entry => entry.Source == ReportEntrySource.CreditCard)
            .GroupBy(entry => entry.SourceId)
            .Select(group =>
            {
                var card = cards[group.Key];
                return new ReportCreditCardBreakdownDto
                {
                    CreditCardId = card.Id,
                    CreditCardName = card.Name,
                    Color = card.Color,
                    Total = group.Sum(entry => entry.Amount),
                    TransactionCount = group.Count()
                };
            })
            .OrderByDescending(card => card.Total)
            .ToList();
    }

    private static List<ReportTopExpenseDto> BuildTopExpenses(IReadOnlyCollection<ReportEntry> entries, ReportSourceData sourceData)
    {
        return entries
            .Where(entry => !entry.IsIncome && entry.Amount > 0)
            .OrderByDescending(entry => entry.Amount)
            .Take(TopExpensesLimit)
            .Select(entry =>
            {
                var (categoryName, categoryColor) = ResolveCategoryDisplay(
                    ResolveCategoryKey(entry.CategoryId, sourceData.Categories),
                    sourceData.Categories);
                var sourceName = entry.Source == ReportEntrySource.Account
                    ? sourceData.Accounts[entry.SourceId].Name
                    : sourceData.Cards[entry.SourceId].Name;

                return new ReportTopExpenseDto
                {
                    Description = entry.Description,
                    Date = entry.Date,
                    Amount = entry.Amount,
                    CategoryName = categoryName,
                    CategoryColor = categoryColor,
                    SourceName = sourceName,
                    SourceType = entry.Source == ReportEntrySource.Account ? "account" : "credit_card",
                    InstallmentLabel = entry.InstallmentLabel
                };
            })
            .ToList();
    }

    // Parcelas futuras independem do regime e do período: mostram o compromisso já assumido no cartão.
    private static List<ReportUpcomingInstallmentDto> BuildUpcomingInstallments(ReportSourceData sourceData, DateTime today)
    {
        return sourceData.AccrualCardItems
            .Where(item => item.TotalInstallments > 1
                           && sourceData.Cards.TryGetValue(item.CreditCardId, out var card)
                           && card.Currency == sourceData.Currency)
            .Select(item => new { Item = item, Date = ReportEntryBuilder.GetInstallmentDate(item) })
            .Where(installment => installment.Date > today)
            .GroupBy(installment => FormatMonth(installment.Date))
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Take(UpcomingInstallmentMonthsLimit)
            .Select(group => new ReportUpcomingInstallmentDto
            {
                Month = group.Key,
                Total = group.Sum(installment => installment.Item.InstallmentAmount),
                InstallmentCount = group.Count()
            })
            .ToList();
    }

    private static List<ReportCurrencyTotalsDto> BuildOtherCurrencies(IEnumerable<ReportEntry> entries)
    {
        return entries
            .GroupBy(entry => entry.Currency)
            .Select(group => new ReportCurrencyTotalsDto
            {
                Currency = group.Key,
                Income = group.Where(entry => entry.IsIncome).Sum(entry => entry.Amount),
                Expense = group.Where(entry => !entry.IsIncome).Sum(entry => entry.Amount)
            })
            .OrderBy(currency => currency.Currency, StringComparer.Ordinal)
            .ToList();
    }

    private static ReportDataQualityDto BuildDataQuality(
        IReadOnlyCollection<ReportEntry> entries,
        IEnumerable<ClassifiedTransaction> classifiedTransactions,
        ReportRange period,
        int otherCurrencyEntriesCount)
    {
        var uncategorizedEntries = entries.Where(entry => string.IsNullOrEmpty(entry.CategoryId)).ToList();
        var periodKinds = classifiedTransactions
            .Where(classified => period.Contains(classified.Transaction.Date))
            .Select(classified => classified.Kind)
            .ToList();

        return new ReportDataQualityDto
        {
            UncategorizedCount = uncategorizedEntries.Count,
            UncategorizedTotal = uncategorizedEntries.Sum(entry => entry.Amount),
            PendingCardTransactionsCount = entries.Count(entry => entry.IsPending),
            ExcludedTransfersCount = periodKinds.Count(kind => kind == AccountTransactionKind.Transfer),
            ExcludedInvoicePaymentsCount = periodKinds.Count(kind => kind == AccountTransactionKind.InvoicePayment),
            OtherCurrencyEntriesCount = otherCurrencyEntriesCount
        };
    }

    private static Dictionary<string, decimal> SumBudgetLimitsByCategory(IEnumerable<Budget> budgets, ReportRange period)
    {
        var periodMonths = EnumerateMonths(period).ToHashSet();

        return budgets
            .Where(budget => periodMonths.Contains(budget.Month))
            .SelectMany(budget => budget.Items)
            .GroupBy(item => item.CategoryId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.LimitAmount));
    }

    private static Dictionary<DateTime, decimal> SumByDay(IEnumerable<ReportEntry> entries)
        => entries
            .GroupBy(entry => entry.Date)
            .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Amount));

    private static string ResolveCategoryKey(string? categoryId, IReadOnlyDictionary<string, Category> categories)
        => !string.IsNullOrEmpty(categoryId) && categories.ContainsKey(categoryId) ? categoryId : UncategorizedId;

    private static (string Name, string Color) ResolveCategoryDisplay(string categoryKey, IReadOnlyDictionary<string, Category> categories)
        => categories.TryGetValue(categoryKey, out var category)
            ? (category.Name, category.Color)
            : (UncategorizedName, UncategorizedColor);

    private static IEnumerable<string> EnumerateMonths(ReportRange period)
    {
        var month = new DateTime(period.Start.Year, period.Start.Month, 1);
        while (month <= period.End)
        {
            yield return FormatMonth(month);
            month = month.AddMonths(1);
        }
    }

    private static int CountMonths(DateTime start, DateTime end)
        => (end.Year - start.Year) * 12 + end.Month - start.Month + 1;

    private static bool IsLastDayOfMonth(DateTime date)
        => date.Day == DateTime.DaysInMonth(date.Year, date.Month);

    private static string FormatMonth(DateTime date) => $"{date.Year:D4}-{date.Month:D2}";

    private readonly record struct ReportRange(DateTime Start, DateTime End)
    {
        public bool Contains(DateTime date) => date.Date >= Start && date.Date <= End;
    }

    private sealed record ClassifiedTransaction(Transaction Transaction, AccountTransactionKind Kind);

    private sealed record ReportSourceData(
        string Currency,
        IReadOnlyDictionary<string, Account> Accounts,
        IReadOnlyDictionary<string, CreditCard> Cards,
        IReadOnlyDictionary<string, Category> Categories,
        IReadOnlyList<Budget> Budgets,
        IReadOnlyList<CreditCardInvoice> Invoices,
        IReadOnlyList<Transaction> AccountTransactions,
        IReadOnlyList<CreditCardTransaction> AccrualCardItems);
}
