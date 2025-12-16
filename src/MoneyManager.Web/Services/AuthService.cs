using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace MoneyManager.Web.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly CustomAuthenticationStateProvider _authStateProvider;

    public AuthService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _authStateProvider = (CustomAuthenticationStateProvider)authStateProvider;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Falha ao fazer login");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        
        if (!string.IsNullOrEmpty(result.Token))
        {
            await _authStateProvider.MarkUserAsAuthenticated(result.Token);
        }

        return result;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Falha ao registrar");
        }

        return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
    }

    public async Task LogoutAsync()
    {
        await _authStateProvider.MarkUserAsLoggedOut();
    }
}
