using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Response;

public class AccountResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public decimal InitialBalance { get; set; }
    public string Currency { get; set; } = "BRL";
    public string Color { get; set; } = "#00C896";
    public decimal? CreditLimit { get; set; }
    public decimal CommittedCredit { get; set; }
    public decimal AvailableCredit { get; set; }
    public int? InvoiceClosingDay { get; set; }
    public int InvoiceDueDayOffset { get; set; }
    public DateTime? LastInvoiceClosedAt { get; set; }
    public string? CurrentOpenInvoiceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
