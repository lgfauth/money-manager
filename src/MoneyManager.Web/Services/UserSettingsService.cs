using MoneyManager.Domain.Entities;
using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly HttpClient _httpClient;

    public UserSettingsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserSettings> GetSettingsAsync()
    {
        var settings = await _httpClient.GetFromJsonAsync<UserSettings>("api/settings");
        return settings ?? new UserSettings();
    }

    public async Task<UserSettings> UpdateSettingsAsync(UserSettings settings)
    {
        var response = await _httpClient.PutAsJsonAsync("api/settings", settings);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserSettings>() ?? settings;
    }
}
