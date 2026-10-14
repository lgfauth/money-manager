using System.Text.Json.Serialization;

namespace MoneyManager.Application.DTOs.Request;

public class ReportOverviewRequestDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ReportBasis Basis { get; set; } = ReportBasis.Accrual;
}

/// <summary>
/// Regime de apuração do relatório.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportBasis
{
    /// <summary>
    /// Competência — compras no cartão contam na data de cada parcela.
    /// </summary>
    Accrual,

    /// <summary>
    /// Caixa — compras no cartão contam na data de pagamento da fatura.
    /// </summary>
    Cash
}
