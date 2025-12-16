using MoneyManager.Domain.Entities;
using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

public class BudgetService : IBudgetService
{
    private readonly HttpClient _httpClient;

    public BudgetService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Budget?> UpdateAsync(string month, MoneyManager.Application.DTOs.Request.CreateBudgetRequestDto request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/budgets/{month}", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Budget>();
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao atualizar orçamento: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<Budget>> GetAllAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<Budget>>("api/budgets") ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar orçamentos: {ex.Message}");
            return [];
        }
    }

    public async Task<Budget?> GetByIdAsync(string id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Budget>($"api/budgets/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar orçamento: {ex.Message}");
            return null;
        }
    }

    public async Task<Budget?> CreateAsync(MoneyManager.Application.DTOs.Request.CreateBudgetRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/budgets", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Budget>();
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao criar orçamento: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateAsync(string id, Budget budget)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/budgets/{id}", budget);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao atualizar orçamento: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/budgets/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao deletar orçamento: {ex.Message}");
            return false;
        }
    }
}
