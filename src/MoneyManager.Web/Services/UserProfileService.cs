using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

public class UserProfileService : IUserProfileService
{
    private readonly HttpClient _httpClient;

    public UserProfileService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserProfileResponseDto> GetProfileAsync()
    {
        var profile = await _httpClient.GetFromJsonAsync<UserProfileResponseDto>("api/profile");
        return profile ?? new UserProfileResponseDto();
    }

    public async Task<UserProfileResponseDto> UpdateProfileAsync(UpdateProfileRequestDto request)
    {
        var response = await _httpClient.PutAsJsonAsync("api/profile", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfileResponseDto>() 
            ?? throw new Exception("Failed to update profile");
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/profile/change-password", request);
        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<UserProfileResponseDto> UpdateEmailAsync(UpdateEmailRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/profile/update-email", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfileResponseDto>() 
            ?? throw new Exception("Failed to update email");
    }
}
