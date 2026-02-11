namespace MoneyManager.Application.DTOs.Request;

public class CreateAccountRequestDto
{
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public decimal InitialBalance { get; set; }

    /// <summary>
    /// Only applicable to credit card accounts. Day of invoice closing (1..31).
    /// </summary>
    public int? InvoiceClosingDay { get; set; }

    /// <summary>
    /// Limite de crédito (apenas para cartões de crédito)
    /// </summary>
    public decimal? CreditLimit { get; set; }

    /// <summary>
    /// Dias entre o fechamento e o vencimento da fatura (padrão: 7 dias)
    /// </summary>
    public int InvoiceDueDayOffset { get; set; } = 7;
}
