using MoneyManager.Domain.Entities;
using MoneyManager.Application.DTOs.Request;
using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

public class TransactionService : ITransactionService
{
    private readonly HttpClient _httpClient;

    public TransactionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Transaction>>("api/transactions") 
            ?? new List<Transaction>();
    }

    public async Task<Transaction?> GetByIdAsync(string id)
    {
        return await _httpClient.GetFromJsonAsync<Transaction>($"api/transactions/{id}");
    }

    public async Task<Transaction> CreateAsync(CreateTransactionRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/transactions", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Transaction>();
    }

    public async Task UpdateAsync(string id, CreateTransactionRequestDto request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/transactions/{id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"api/transactions/{id}");
        response.EnsureSuccessStatusCode();
    }
}
