namespace MoneyManager.Application.DTOs.Response;

public class CreditCardResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AvailableLimit { get; set; }
    public int ClosingDay { get; set; }
    public int BillingDueDay { get; set; }
    public int BestPurchaseDay { get; set; }
    public string Color { get; set; } = "#4E9BFF";
    public string Currency { get; set; } = "BRL";
    public CreditCardInvoiceSummaryDto? CurrentInvoice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreditCardInvoiceSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string ReferenceMonth { get; set; } = string.Empty;
    public DateTime ClosingDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
