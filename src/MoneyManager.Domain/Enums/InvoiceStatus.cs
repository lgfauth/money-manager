namespace MoneyManager.Domain.Enums;

/// <summary>
/// Status de uma fatura de cartão de crédito
/// </summary>
public enum InvoiceStatus
{
    /// <summary>
    /// Fatura aberta, ainda aceitando novas transações
    /// </summary>
    Open = 0,

    /// <summary>
    /// Fatura fechada, não aceita mais transações mas ainda não foi paga
    /// </summary>
    Closed = 1,

    /// <summary>
    /// Fatura paga completamente
    /// </summary>
    Paid = 2,

    /// <summary>
    /// Fatura parcialmente paga
    /// </summary>
    PartiallyPaid = 3,

    /// <summary>
    /// Fatura vencida (passou da data de vencimento sem pagamento total)
    /// </summary>
    Overdue = 4
}
