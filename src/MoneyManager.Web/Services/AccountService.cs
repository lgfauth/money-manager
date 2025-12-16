using MoneyManager.Domain.Entities;
using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

public class AccountService : IAccountService
{
    private readonly HttpClient _httpClient;

    public AccountService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Account>> GetAllAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Account>>("api/accounts") 
            ?? new List<Account>();
    }

    public async Task<Account?> GetByIdAsync(string id)
    {
        return await _httpClient.GetFromJsonAsync<Account>($"api/accounts/{id}");
    }

    public async Task<Account> CreateAsync(Account account)
    {
        var response = await _httpClient.PostAsJsonAsync("api/accounts", account);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Account>();
    }

    public async Task UpdateAsync(string id, Account account)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/accounts/{id}", account);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"api/accounts/{id}");
        response.EnsureSuccessStatusCode();
    }
}
