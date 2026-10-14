using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Response;

public class DashboardSummaryResponseDto
{
    public string Currency { get; set; } = "BRL";
    public decimal AccountsBalance { get; set; }
    public decimal CreditCardDebt { get; set; }
    public decimal NetWorth { get; set; }
    public List<DashboardUpcomingInvoiceDto> UpcomingInvoices { get; set; } = [];
    public List<DashboardUpcomingRecurringDto> UpcomingRecurring { get; set; } = [];
}

public class DashboardUpcomingInvoiceDto
{
    public string InvoiceId { get; set; } = string.Empty;
    public string CreditCardId { get; set; } = string.Empty;
    public string CreditCardName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string ReferenceMonth { get; set; } = string.Empty;
    public DateTime ClosingDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public InvoiceStatus Status { get; set; }
    public bool IsOverdue { get; set; }
    public string Currency { get; set; } = "BRL";
}

public class DashboardUpcomingRecurringDto
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
}
