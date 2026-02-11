using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Request;

/// <summary>
/// DTO para criar fatura de cartão de crédito manualmente (usado na migração e casos especiais)
/// </summary>
public class CreateCreditCardInvoiceRequestDto
{
    public string AccountId { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
}

/// <summary>
/// DTO para pagamento de fatura
/// </summary>
public class PayInvoiceRequestDto
{
    /// <summary>
    /// ID da fatura a ser paga
    /// </summary>
    public string InvoiceId { get; set; } = string.Empty;

    /// <summary>
    /// ID da conta que vai realizar o pagamento
    /// </summary>
    public string PayFromAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Valor do pagamento
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Data do pagamento
    /// </summary>
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Descrição adicional (opcional)
    /// </summary>
    public string? Description { get; set; }
}
