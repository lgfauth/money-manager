using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.Services;

/// <summary>
/// Implementação do serviço de dados de mercado usando a API Brapi (https://brapi.dev).
/// Suporta ações e FIIs da B3.
/// </summary>
public class BrapiMarketDataService : IMarketDataService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BrapiMarketDataService> _logger;
    private const string BaseUrl = "https://brapi.dev/api";
    private const int CacheExpirationMinutes = 15; // Cache de 15 minutos
    private static readonly SemaphoreSlim _rateLimiter = new(10, 10); // Max 10 requisições simultâneas

    public BrapiMarketDataService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<BrapiMarketDataService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<decimal?> GetCurrentPriceAsync(string ticker, InvestmentAssetType assetType)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return null;
        }

        // Apenas suporta ações e FIIs da B3
        if (assetType != InvestmentAssetType.Stock && 
            assetType != InvestmentAssetType.RealEstate &&
            assetType != InvestmentAssetType.ETF)
        {
            _logger.LogWarning("Tipo de ativo {AssetType} não suportado pela Brapi", assetType);
            return null;
        }

        var cacheKey = $"brapi_price_{ticker.ToUpperInvariant()}";

        // Tentar obter do cache
        if (_cache.TryGetValue<decimal>(cacheKey, out var cachedPrice))
        {
            _logger.LogDebug("Preço de {Ticker} obtido do cache: {Price}", ticker, cachedPrice);
            return cachedPrice;
        }

        await _rateLimiter.WaitAsync();
        try
        {
            _logger.LogInformation("Buscando cotação de {Ticker} na Brapi", ticker);

            var response = await _httpClient.GetAsync($"/quote/{ticker.ToUpperInvariant()}?range=1d&interval=1d&fundamental=false");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Erro ao buscar cotação de {Ticker}: {StatusCode}",
                    ticker, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<BrapiQuoteResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data?.Results == null || !data.Results.Any())
            {
                _logger.LogWarning("Nenhum resultado encontrado para {Ticker}", ticker);
                return null;
            }

            var quote = data.Results[0];
            var price = quote.RegularMarketPrice;

            if (price <= 0)
            {
                _logger.LogWarning("Preço inválido para {Ticker}: {Price}", ticker, price);
                return null;
            }

            // Armazenar no cache
            _cache.Set(cacheKey, price, TimeSpan.FromMinutes(CacheExpirationMinutes));

            _logger.LogInformation(
                "Cotação de {Ticker} obtida com sucesso: R$ {Price:F2}",
                ticker, price);

            return price;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro HTTP ao buscar cotação de {Ticker}", ticker);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar resposta da Brapi para {Ticker}", ticker);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao buscar cotação de {Ticker}", ticker);
            return null;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<List<HistoricalPriceDto>> GetHistoricalPricesAsync(
        string ticker, 
        DateTime startDate, 
        DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return new List<HistoricalPriceDto>();
        }

        await _rateLimiter.WaitAsync();
        try
        {
            _logger.LogInformation(
                "Buscando histórico de {Ticker} de {Start} a {End}",
                ticker, startDate, endDate);

            // Calcular range (1d, 5d, 1mo, 3mo, 6mo, 1y, 2y, 5y, 10y, ytd, max)
            var range = CalculateRange(startDate, endDate);

            var response = await _httpClient.GetAsync(
                $"/quote/{ticker.ToUpperInvariant()}?range={range}&interval=1d&fundamental=false");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Erro ao buscar histórico de {Ticker}: {StatusCode}",
                    ticker, response.StatusCode);
                return new List<HistoricalPriceDto>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<BrapiQuoteResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data?.Results == null || !data.Results.Any())
            {
                return new List<HistoricalPriceDto>();
            }

            var quote = data.Results[0];
            if (quote.HistoricalDataPrice == null || !quote.HistoricalDataPrice.Any())
            {
                return new List<HistoricalPriceDto>();
            }

            var prices = quote.HistoricalDataPrice
                .Where(h => h.DateAsDateTime >= startDate && h.DateAsDateTime <= endDate)
                .Select(h => new HistoricalPriceDto
                {
                    Date = h.DateAsDateTime,
                    Open = h.Open,
                    High = h.High,
                    Low = h.Low,
                    Close = h.Close,
                    Volume = h.Volume
                })
                .OrderBy(h => h.Date)
                .ToList();

            _logger.LogInformation(
                "Histórico de {Ticker} obtido: {Count} registros",
                ticker, prices.Count);

            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar histórico de {Ticker}", ticker);
            return new List<HistoricalPriceDto>();
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<AssetInfoDto?> GetAssetInfoAsync(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return null;
        }

        var cacheKey = $"brapi_info_{ticker.ToUpperInvariant()}";

        // Tentar obter do cache (cache mais longo para informações)
        if (_cache.TryGetValue<AssetInfoDto>(cacheKey, out var cachedInfo))
        {
            return cachedInfo;
        }

        await _rateLimiter.WaitAsync();
        try
        {
            _logger.LogInformation("Buscando informações de {Ticker} na Brapi", ticker);

            var response = await _httpClient.GetAsync(
                $"/quote/{ticker.ToUpperInvariant()}?fundamental=true");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<BrapiQuoteResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data?.Results == null || !data.Results.Any())
            {
                return null;
            }

            var quote = data.Results[0];

            var info = new AssetInfoDto
            {
                Ticker = quote.Symbol,
                Name = quote.LongName ?? quote.ShortName ?? ticker,
                Sector = quote.SummaryProfile?.Sector,
                MarketCap = quote.MarketCap,
                DividendYield = quote.DividendYield,
                LastUpdate = DateTime.UtcNow
            };

            // Cache por 24 horas (informações mudam pouco)
            _cache.Set(cacheKey, info, TimeSpan.FromHours(24));

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar informações de {Ticker}", ticker);
            return null;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/quote/PETR4?range=1d&interval=1d");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string CalculateRange(DateTime startDate, DateTime endDate)
    {
        var days = (endDate - startDate).Days;

        return days switch
        {
            <= 5 => "5d",
            <= 30 => "1mo",
            <= 90 => "3mo",
            <= 180 => "6mo",
            <= 365 => "1y",
            <= 730 => "2y",
            <= 1825 => "5y",
            _ => "max"
        };
    }

    #region DTOs Internos (Brapi API)

    private class BrapiQuoteResponse
    {
        public List<BrapiQuote> Results { get; set; } = new();
    }

    private class BrapiQuote
    {
        public string Symbol { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public string? LongName { get; set; }
        public decimal RegularMarketPrice { get; set; }
        public long? MarketCap { get; set; }
        public decimal? DividendYield { get; set; }
        public BrapiSummaryProfile? SummaryProfile { get; set; }
        public List<BrapiHistoricalPrice>? HistoricalDataPrice { get; set; }
    }

    private class BrapiSummaryProfile
    {
        public string? Sector { get; set; }
        public string? Industry { get; set; }
    }

    private class BrapiHistoricalPrice
    {
        public long Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }

        public DateTime DateAsDateTime => DateTimeOffset.FromUnixTimeSeconds(Date).DateTime;
    }

    #endregion
}
