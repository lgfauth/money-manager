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

    // --- Só para Source = "invoice_import" (importação de fatura); ignorados nas demais origens ---

    // Mês de referência (yyyy-MM) da fatura que recebe a primeira parcela criada, mesmo fechada/paga
    public string? TargetReferenceMonth { get; set; }

    // Parcela inicial (X de N): cria só X..N, com a numeração original
    public int? FirstInstallmentNumber { get; set; }

    public string? InvoiceImportId { get; set; }

    // Gravado só na parcela criada na fatura importada (base da detecção de reimportação)
    public string? ImportFingerprint { get; set; }
}
