namespace MoneyManager.Domain.Enums;

/// <summary>
/// Tipos de ativos de investimento disponíveis no sistema.
/// </summary>
public enum InvestmentAssetType
{
    /// <summary>
    /// Ações negociadas em bolsa de valores (ex: PETR4, VALE3).
    /// </summary>
    Stock = 0,

    /// <summary>
    /// Investimentos de renda fixa (CDB, Tesouro Direto, LCI, LCA, etc.).
    /// </summary>
    FixedIncome = 1,

    /// <summary>
    /// Fundos de Investimento Imobiliário (FIIs).
    /// </summary>
    RealEstate = 2,

    /// <summary>
    /// Criptomoedas (Bitcoin, Ethereum, etc.).
    /// </summary>
    Crypto = 3,

    /// <summary>
    /// Fundos de Investimento (Multimercado, Renda Fixa, Ações, etc.).
    /// </summary>
    Fund = 4,

    /// <summary>
    /// Exchange-Traded Funds (ETFs).
    /// </summary>
    ETF = 5,

    /// <summary>
    /// Outros tipos de investimento não categorizados.
    /// </summary>
    Other = 99
}
