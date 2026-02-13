using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Response;

/// <summary>
/// DTO de resposta com resumo consolidado dos investimentos.
/// </summary>
public class InvestmentSummaryResponseDto
{
    /// <summary>
    /// Total investido em todos os ativos.
    /// </summary>
    public decimal TotalInvested { get; set; }

    /// <summary>
    /// Valor atual de todos os ativos.
    /// </summary>
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Lucro ou prejuízo total absoluto.
    /// </summary>
    public decimal TotalProfitLoss { get; set; }

    /// <summary>
    /// Percentual de lucro ou prejuízo total.
    /// </summary>
    public decimal TotalProfitLossPercentage { get; set; }

    /// <summary>
    /// Quantidade total de ativos.
    /// </summary>
    public int TotalAssets { get; set; }

    /// <summary>
    /// Ativos agrupados por tipo.
    /// </summary>
    public List<AssetsByTypeDto> AssetsByType { get; set; } = new();

    /// <summary>
    /// Top 5 melhores ativos (maiores rentabilidades).
    /// </summary>
    public List<AssetPerformanceDto> TopPerformers { get; set; } = new();

    /// <summary>
    /// Top 5 piores ativos (menores rentabilidades).
    /// </summary>
    public List<AssetPerformanceDto> WorstPerformers { get; set; } = new();

    /// <summary>
    /// Rendimentos totais recebidos no período.
    /// </summary>
    public decimal TotalYields { get; set; }
}

/// <summary>
/// DTO para agrupamento de ativos por tipo.
/// </summary>
public class AssetsByTypeDto
{
    /// <summary>
    /// Tipo do ativo.
    /// </summary>
    public InvestmentAssetType AssetType { get; set; }

    /// <summary>
    /// Quantidade de ativos deste tipo.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Valor total investido neste tipo.
    /// </summary>
    public decimal TotalInvested { get; set; }

    /// <summary>
    /// Valor atual total deste tipo.
    /// </summary>
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Lucro ou prejuízo deste tipo.
    /// </summary>
    public decimal ProfitLoss { get; set; }

    /// <summary>
    /// Percentual de lucro ou prejuízo deste tipo.
    /// </summary>
    public decimal ProfitLossPercentage { get; set; }

    /// <summary>
    /// Percentual do portfólio que este tipo representa.
    /// </summary>
    public decimal PortfolioPercentage { get; set; }
}

/// <summary>
/// DTO para performance de um ativo.
/// </summary>
public class AssetPerformanceDto
{
    /// <summary>
    /// ID do ativo.
    /// </summary>
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Nome do ativo.
    /// </summary>
    public string AssetName { get; set; } = string.Empty;

    /// <summary>
    /// Ticker do ativo.
    /// </summary>
    public string? AssetTicker { get; set; }

    /// <summary>
    /// Tipo do ativo.
    /// </summary>
    public InvestmentAssetType AssetType { get; set; }

    /// <summary>
    /// Valor investido.
    /// </summary>
    public decimal TotalInvested { get; set; }

    /// <summary>
    /// Valor atual.
    /// </summary>
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Lucro ou prejuízo.
    /// </summary>
    public decimal ProfitLoss { get; set; }

    /// <summary>
    /// Percentual de rentabilidade.
    /// </summary>
    public decimal ProfitLossPercentage { get; set; }
}
