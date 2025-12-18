using MoneyManager.Application.DTOs.Response;
using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

public interface IOnboardingService
{
    Task<OnboardingStatusDto?> GetStatusAsync();
    Task<List<CategorySuggestionDto>> GetCategorySuggestionsAsync();
    Task CompleteOnboardingAsync();
}

public class OnboardingService : IOnboardingService
{
    private readonly HttpClient _httpClient;

    public OnboardingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OnboardingStatusDto?> GetStatusAsync()
    {
        return await _httpClient.GetFromJsonAsync<OnboardingStatusDto>("api/onboarding/status");
    }

    public async Task<List<CategorySuggestionDto>> GetCategorySuggestionsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<CategorySuggestionDto>>("api/onboarding/category-suggestions") 
            ?? new List<CategorySuggestionDto>();
    }

    public async Task CompleteOnboardingAsync()
    {
        var response = await _httpClient.PostAsync("api/onboarding/complete", null);
        response.EnsureSuccessStatusCode();
    }
}
