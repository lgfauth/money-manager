using MoneyManager.Domain.Entities;
using MoneyManager.Application.DTOs.Request;
using System.Net.Http.Json;
using System.Text.Json;

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
        var response = await _httpClient.GetAsync("api/recurringtransactions");
        if (!response.IsSuccessStatusCode)
        {
            var apiMessage = await TryReadApiErrorMessageAsync(response);
            throw new HttpRequestException(apiMessage);
        }

        return await response.Content.ReadFromJsonAsync<IEnumerable<RecurringTransaction>>()
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

    private static async Task<string> TryReadApiErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var raw = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return $"Erro ao carregar transações recorrentes ({(int)response.StatusCode}).";
            }

            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("message", out var messageProp))
            {
                var message = messageProp.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }

            return $"Erro ao carregar transações recorrentes ({(int)response.StatusCode}).";
        }
        catch
        {
            return $"Erro ao carregar transações recorrentes ({(int)response.StatusCode}).";
        }
    }
}
