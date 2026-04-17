namespace MoneyManager.Application.DTOs.Request;

public class CreateCreditCardTransactionRequestDto
{
    public string CreditCardId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalInstallments { get; set; } = 1;
    public bool FirstInstallmentOnCurrentInvoice { get; set; } = true;
    public string? ClientRequestId { get; set; }
}
