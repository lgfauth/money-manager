namespace MoneyManager.Application.DTOs.Response;

public class CreditCardTransactionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string CreditCardId { get; set; } = string.Empty;
    public string InvoiceId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = "#64748b";
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public int InstallmentNumber { get; set; }
    public int TotalInstallments { get; set; }
    public string? ParentTransactionId { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
