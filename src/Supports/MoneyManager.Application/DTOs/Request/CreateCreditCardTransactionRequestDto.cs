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
    public bool IsRefund { get; set; } = false;
    public string? ClientRequestId { get; set; }
    public string Source { get; set; } = "manual";
    public string? ExternalId { get; set; }
    public bool IsPending { get; set; } // bank_sync: compra da fatura aberta ainda não confirmada
    public string? OpenBankingCategoryId { get; set; } // categoryId de origem do Pluggy (bank_sync)
}
