using MoneyManager.Domain.Entities;
using MoneyManager.Application.DTOs.Request;
using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

public class RecurringTransactionService : IRecurringTransactionService
{
    private readonly HttpClient _httpClient;

    public RecurringTransactionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<RecurringTransaction>> GetAllAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<RecurringTransaction>>("api/recurringtransactions") 
            ?? new List<RecurringTransaction>();
    }

    public async Task<RecurringTransaction?> GetByIdAsync(string id)
    {
        return await _httpClient.GetFromJsonAsync<RecurringTransaction>($"api/recurringtransactions/{id}");
    }

    public async Task<RecurringTransaction> CreateAsync(CreateRecurringTransactionRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/recurringtransactions", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecurringTransaction>() 
            ?? throw new Exception("Failed to create recurring transaction");
    }

    public async Task UpdateAsync(string id, CreateRecurringTransactionRequestDto request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/recurringtransactions/{id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"api/recurringtransactions/{id}");
        response.EnsureSuccessStatusCode();
    }
}
