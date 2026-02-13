namespace MoneyManager.Domain.Enums;

/// <summary>
/// Tipos de transações financeiras no sistema.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Receita (entrada de dinheiro).
    /// </summary>
    Income = 0,

    /// <summary>
    /// Despesa (saída de dinheiro).
    /// </summary>
    Expense = 1,

    /// <summary>
    /// Transferência entre contas.
    /// </summary>
    Transfer = 2,

    /// <summary>
    /// Rendimento de investimento (dividendos, juros, aluguéis).
    /// </summary>
    InvestmentYield = 3,

    /// <summary>
    /// Perda de investimento (ajuste negativo de valor).
    /// </summary>
    InvestmentLoss = 4,

    /// <summary>
    /// Compra de ativo de investimento.
    /// </summary>
    InvestmentBuy = 5,

    /// <summary>
    /// Venda de ativo de investimento.
    /// </summary>
    InvestmentSell = 6
}
