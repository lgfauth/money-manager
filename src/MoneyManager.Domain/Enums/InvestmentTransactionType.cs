namespace MoneyManager.Domain.Enums;

/// <summary>
/// Tipos de transações relacionadas a investimentos.
/// </summary>
public enum InvestmentTransactionType
{
    /// <summary>
    /// Compra de ativo (aumento de posição).
    /// </summary>
    Buy = 0,

    /// <summary>
    /// Venda de ativo (redução de posição).
    /// </summary>
    Sell = 1,

    /// <summary>
    /// Recebimento de dividendos (ações).
    /// </summary>
    Dividend = 2,

    /// <summary>
    /// Recebimento de juros (renda fixa).
    /// </summary>
    Interest = 3,

    /// <summary>
    /// Rendimento geral (aluguéis de FIIs, rendimentos de fundos, etc.).
    /// </summary>
    YieldPayment = 4,

    /// <summary>
    /// Ajuste de valor de mercado (marcação a mercado).
    /// </summary>
    MarketAdjustment = 5,

    /// <summary>
    /// Taxa ou corretagem paga.
    /// </summary>
    Fee = 6
}
