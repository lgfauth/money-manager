namespace MoneyManager.Application.DTOs.Response;

public class AccountResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public decimal Balance { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal? CreditLimit { get; set; }
    public int? InvoiceClosingDay { get; set; }
    public int InvoiceDueDayOffset { get; set; }
    public DateTime? LastInvoiceClosedAt { get; set; }
    public string? CurrentOpenInvoiceId { get; set; }
    public DateTime CreatedAt { get; set; }
}
