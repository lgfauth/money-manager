using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

public class CategoryService : ICategoryService
{
    private readonly HttpClient _httpClient;

    public CategoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Category>>("api/categories") 
            ?? new List<Category>();
    }

    public async Task<IEnumerable<Category>> GetAllAsync(CategoryType type)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Category>>($"api/categories?type={(int)type}")
            ?? new List<Category>();
    }

    public async Task<Category?> GetByIdAsync(string id)
    {
        return await _httpClient.GetFromJsonAsync<Category>($"api/categories/{id}");
    }

    public async Task<Category> CreateAsync(Category category)
    {
        var response = await _httpClient.PostAsJsonAsync("api/categories", category);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Category>();
    }

    public async Task UpdateAsync(string id, Category category)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/categories/{id}", category);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"api/categories/{id}");
        response.EnsureSuccessStatusCode();
    }
}
