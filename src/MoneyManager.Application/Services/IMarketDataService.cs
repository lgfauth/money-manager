using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.Services;

/// <summary>
/// Serviço para obter dados de mercado de APIs externas.
/// Suporta cotações de ações, FIIs, criptomoedas, etc.
/// </summary>
public interface IMarketDataService
{
    /// <summary>
    /// Obtém o preço atual de um ativo pelo ticker.
    /// </summary>
    /// <param name="ticker">Ticker do ativo (ex: PETR4, KNRI11, BTC)</param>
    /// <param name="assetType">Tipo do ativo</param>
    /// <returns>Preço atual ou null se não encontrado</returns>
    Task<decimal?> GetCurrentPriceAsync(string ticker, InvestmentAssetType assetType);

    /// <summary>
    /// Obtém preços históricos de um ativo.
    /// </summary>
    /// <param name="ticker">Ticker do ativo</param>
    /// <param name="startDate">Data inicial</param>
    /// <param name="endDate">Data final</param>
    /// <returns>Lista de preços históricos</returns>
    Task<List<HistoricalPriceDto>> GetHistoricalPricesAsync(
        string ticker, 
        DateTime startDate, 
        DateTime endDate);

    /// <summary>
    /// Obtém informações detalhadas sobre um ativo.
    /// </summary>
    /// <param name="ticker">Ticker do ativo</param>
    /// <returns>Informações do ativo</returns>
    Task<AssetInfoDto?> GetAssetInfoAsync(string ticker);

    /// <summary>
    /// Verifica se o serviço está disponível.
    /// </summary>
    /// <returns>True se disponível</returns>
    Task<bool> IsAvailableAsync();
}

/// <summary>
/// DTO com preço histórico.
/// </summary>
public class HistoricalPriceDto
{
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

/// <summary>
/// DTO com informações do ativo.
/// </summary>
public class AssetInfoDto
{
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public decimal? MarketCap { get; set; }
    public decimal? DividendYield { get; set; }
    public DateTime LastUpdate { get; set; }
}
