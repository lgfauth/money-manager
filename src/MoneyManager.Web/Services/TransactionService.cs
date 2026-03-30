using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using System.Net.Http.Json;
using System.Web;

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

    public async Task<PagedResultDto<Transaction>> GetAllPagedAsync(
        int page = 1,
        int pageSize = 50,
        DateTime? startDate = null,
        DateTime? endDate = null,
        TransactionType? type = null,
        string sortBy = "date_desc")
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["page"] = page.ToString();
        query["pageSize"] = pageSize.ToString();
        query["sortBy"] = sortBy;

        if (startDate.HasValue)
            query["startDate"] = startDate.Value.ToString("yyyy-MM-dd");
        if (endDate.HasValue)
            query["endDate"] = endDate.Value.ToString("yyyy-MM-dd");
        if (type.HasValue)
            query["type"] = ((int)type.Value).ToString();

        return await _httpClient.GetFromJsonAsync<PagedResultDto<Transaction>>($"api/transactions?{query}")
            ?? new PagedResultDto<Transaction>();
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
