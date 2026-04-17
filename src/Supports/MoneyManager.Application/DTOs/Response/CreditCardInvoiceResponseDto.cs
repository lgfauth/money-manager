namespace MoneyManager.Application.DTOs.Response;

public class CreditCardInvoiceResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string CreditCardId { get; set; } = string.Empty;
    public string CreditCardName { get; set; } = string.Empty;
    public string ReferenceMonth { get; set; } = string.Empty;
    public DateTime ClosingDate { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidWithAccountId { get; set; }
    public decimal? PaidAmount { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
