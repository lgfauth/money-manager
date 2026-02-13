using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

/// <summary>
/// Implementação do serviço HTTP para transações de investimento.
/// </summary>
public class InvestmentTransactionService : IInvestmentTransactionService
{
    private readonly HttpClient _httpClient;

    public InvestmentTransactionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<InvestmentTransactionResponseDto>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var url = "api/investment-transactions";
            
            if (startDate.HasValue || endDate.HasValue)
            {
                var queryParams = new List<string>();
                
                if (startDate.HasValue)
                {
                    queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
                }
                
                if (endDate.HasValue)
                {
                    queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }
            }
            
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<InvestmentTransactionResponseDto>>(url);
            return result ?? new List<InvestmentTransactionResponseDto>();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao buscar transações de investimento: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<InvestmentTransactionResponseDto>> GetByAssetIdAsync(string assetId)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<InvestmentTransactionResponseDto>>($"api/investment-transactions/asset/{assetId}");
            return result ?? new List<InvestmentTransactionResponseDto>();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao buscar transações do ativo {assetId}: {ex.Message}");
            throw;
        }
    }

    public async Task<InvestmentTransactionResponseDto> RecordYieldAsync(RecordYieldRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/investment-transactions/yield", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<InvestmentTransactionResponseDto>();
            return result ?? throw new InvalidOperationException("Resposta vazia da API");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao registrar rendimento: {ex.Message}");
            throw;
        }
    }
}
