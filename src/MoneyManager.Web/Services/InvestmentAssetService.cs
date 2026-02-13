using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

/// <summary>
/// Implementação do serviço HTTP para ativos de investimento.
/// </summary>
public class InvestmentAssetService : IInvestmentAssetService
{
    private readonly HttpClient _httpClient;

    public InvestmentAssetService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<InvestmentAssetResponseDto>> GetAllAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<InvestmentAssetResponseDto>>("api/investment-assets");
            return result ?? new List<InvestmentAssetResponseDto>();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao buscar ativos de investimento: {ex.Message}");
            throw;
        }
    }

    public async Task<InvestmentAssetResponseDto?> GetByIdAsync(string id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<InvestmentAssetResponseDto>($"api/investment-assets/{id}");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao buscar ativo {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<InvestmentAssetResponseDto> CreateAsync(CreateInvestmentAssetRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/investment-assets", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<InvestmentAssetResponseDto>();
            return result ?? throw new InvalidOperationException("Resposta vazia da API");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao criar ativo de investimento: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateAsync(string id, UpdateInvestmentAssetRequestDto request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/investment-assets/{id}", request);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao atualizar ativo {id}: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/investment-assets/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao deletar ativo {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<InvestmentAssetResponseDto> BuyAsync(string id, BuyAssetRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/investment-assets/{id}/buy", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<InvestmentAssetResponseDto>();
            return result ?? throw new InvalidOperationException("Resposta vazia da API");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao registrar compra do ativo {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<InvestmentAssetResponseDto> SellAsync(string id, SellAssetRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/investment-assets/{id}/sell", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<InvestmentAssetResponseDto>();
            return result ?? throw new InvalidOperationException("Resposta vazia da API");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao registrar venda do ativo {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<InvestmentAssetResponseDto> AdjustPriceAsync(string id, AdjustPriceRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/investment-assets/{id}/adjust-price", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<InvestmentAssetResponseDto>();
            return result ?? throw new InvalidOperationException("Resposta vazia da API");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao ajustar preço do ativo {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<InvestmentSummaryResponseDto> GetSummaryAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<InvestmentSummaryResponseDto>("api/investment-assets/summary");
            return result ?? new InvestmentSummaryResponseDto();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao buscar resumo de investimentos: {ex.Message}");
            throw;
        }
    }

    public async Task<PriceUpdateResponseDto?> UpdatePricesAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/investment-assets/update-prices", null);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<PriceUpdateResponseDto>();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao atualizar preços: {ex.Message}");
            throw;
        }
    }
}

