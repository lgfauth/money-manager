using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public interface IDashboardService
{
    Task<DashboardSummaryResponseDto> GetSummaryAsync(string userId);
}

/// <summary>
/// Posição atual do usuário (patrimônio líquido) e compromissos dos próximos dias,
/// complementando o relatório do mês exibido no dashboard.
/// </summary>
public class DashboardService : IDashboardService
{
    private const string DefaultCurrency = "BRL";
    private const string UncategorizedName = "Sem categoria";
    private const string UncategorizedColor = "#64748b";
    private const int UpcomingRecurringDays = 15;
    private const int UpcomingRecurringLimit = 10;

    // Faturas ainda não quitadas — inclui as futuras (Pending), que carregam parcelas já assumidas.
    private static readonly InvoiceStatus[] UnpaidInvoiceStatuses =
        [InvoiceStatus.Pending, InvoiceStatus.Open, InvoiceStatus.Closed, InvoiceStatus.Overdue];

    // Faturas que já têm valor definido ou em formação e um vencimento próximo.
    private static readonly InvoiceStatus[] UpcomingInvoiceStatuses =
        [InvoiceStatus.Open, InvoiceStatus.Closed, InvoiceStatus.Overdue];

    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardSummaryResponseDto> GetSummaryAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;
        var settings = (await _unitOfWork.UserSettings.GetByUserIdAsync(userId)).FirstOrDefault();
        var currency = string.IsNullOrWhiteSpace(settings?.Currency) ? DefaultCurrency : settings.Currency;

        var accounts = (await _unitOfWork.Accounts.GetByUserIdAsync(userId))
            .Where(account => !account.IsDeleted)
            .ToDictionary(account => account.Id);
        var cards = (await _unitOfWork.CreditCards.GetByUserAsync(userId))
            .Where(card => !card.IsDeleted)
            .ToDictionary(card => card.Id);
        var invoices = (await _unitOfWork.CreditCardInvoices.GetByUserAsync(userId))
            .Where(invoice => cards.ContainsKey(invoice.CreditCardId))
            .ToList();

        var accountsBalance = accounts.Values
            .Where(account => account.Currency == currency)
            .Sum(account => account.Balance);
        var creditCardDebt = invoices
            .Where(invoice => UnpaidInvoiceStatuses.Contains(invoice.Status)
                              && cards[invoice.CreditCardId].Currency == currency)
            .Sum(invoice => invoice.TotalAmount);

        return new DashboardSummaryResponseDto
        {
            Currency = currency,
            AccountsBalance = accountsBalance,
            CreditCardDebt = creditCardDebt,
            NetWorth = accountsBalance - creditCardDebt,
            UpcomingInvoices = BuildUpcomingInvoices(invoices, cards, today),
            UpcomingRecurring = await BuildUpcomingRecurringAsync(userId, accounts, today)
        };
    }

    private static List<DashboardUpcomingInvoiceDto> BuildUpcomingInvoices(
        IEnumerable<CreditCardInvoice> invoices,
        IReadOnlyDictionary<string, CreditCard> cards,
        DateTime today)
    {
        return invoices
            .Where(invoice => UpcomingInvoiceStatuses.Contains(invoice.Status) && invoice.TotalAmount > 0)
            .OrderBy(invoice => invoice.DueDate)
            .Select(invoice =>
            {
                var card = cards[invoice.CreditCardId];
                return new DashboardUpcomingInvoiceDto
                {
                    InvoiceId = invoice.Id,
                    CreditCardId = card.Id,
                    CreditCardName = card.Name,
                    Color = card.Color,
                    ReferenceMonth = invoice.ReferenceMonth,
                    ClosingDate = invoice.ClosingDate,
                    DueDate = invoice.DueDate,
                    Amount = invoice.TotalAmount,
                    Status = invoice.Status,
                    // O worker marca Overdue uma vez por dia; uma fatura fechada já vencida também é atraso.
                    IsOverdue = invoice.Status == InvoiceStatus.Overdue
                                || (invoice.Status == InvoiceStatus.Closed && invoice.DueDate.Date < today),
                    Currency = card.Currency
                };
            })
            .ToList();
    }

    private async Task<List<DashboardUpcomingRecurringDto>> BuildUpcomingRecurringAsync(
        string userId,
        IReadOnlyDictionary<string, Account> accounts,
        DateTime today)
    {
        var windowEnd = today.AddDays(UpcomingRecurringDays);
        var recurringTransactions = (await _unitOfWork.RecurringTransactions.GetByUserIdAsync(userId))
            .Where(recurring => !recurring.IsDeleted
                                && recurring.IsActive
                                && accounts.ContainsKey(recurring.AccountId)
                                && recurring.NextOccurrenceDate.Date >= today
                                && recurring.NextOccurrenceDate.Date <= windowEnd
                                && (recurring.EndDate is null || recurring.NextOccurrenceDate.Date <= recurring.EndDate.Value.Date)
                                && recurring.RemainingOccurrences is null or > 0)
            .OrderBy(recurring => recurring.NextOccurrenceDate)
            .Take(UpcomingRecurringLimit)
            .ToList();

        if (recurringTransactions.Count == 0)
            return [];

        var categories = (await _unitOfWork.Categories.GetByUserIdAsync(userId))
            .ToDictionary(category => category.Id);

        return recurringTransactions
            .Select(recurring =>
            {
                var category = recurring.CategoryId is not null
                    ? categories.GetValueOrDefault(recurring.CategoryId)
                    : null;

                return new DashboardUpcomingRecurringDto
                {
                    Id = recurring.Id,
                    Description = recurring.Description,
                    Amount = recurring.Amount,
                    Type = recurring.Type,
                    Date = recurring.NextOccurrenceDate,
                    AccountName = accounts[recurring.AccountId].Name,
                    CategoryName = category?.Name ?? UncategorizedName,
                    CategoryColor = category?.Color ?? UncategorizedColor
                };
            })
            .ToList();
    }
}
