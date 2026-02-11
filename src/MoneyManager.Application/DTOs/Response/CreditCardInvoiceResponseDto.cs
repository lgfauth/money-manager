using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Response;

/// <summary>
/// DTO de resposta para fatura de cartão de crédito
/// </summary>
public class CreditCardInvoiceResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public InvoiceStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public DateTime? ClosedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string ReferenceMonth { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysUntilDue { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO de resumo de fatura (para dashboard)
/// </summary>
public class InvoiceSummaryDto
{
    public CreditCardInvoiceResponseDto Invoice { get; set; } = new();
    public List<TransactionResponseDto> Transactions { get; set; } = new();
    public decimal AverageTransactionAmount { get; set; }
    public int TotalTransactions { get; set; }
    public Dictionary<string, decimal> AmountByCategory { get; set; } = new();
}
