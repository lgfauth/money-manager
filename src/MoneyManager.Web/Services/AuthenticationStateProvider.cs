using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Globalization;

namespace MoneyManager.Web.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private const string TokenKey = "authToken";
    private const string ExpiryKey = "authTokenExpiry";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(10);
    private bool _isInitialized = false;

    public CustomAuthenticationStateProvider(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    private AuthenticationState _anonymousState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_isInitialized)
        {
            return _anonymousState;
        }

        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", TokenKey);
            var expiryStr = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", ExpiryKey);

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expiryStr))
            {
                return _anonymousState;
            }

            if (!DateTime.TryParseExact(expiryStr, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiry))
            {
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", ExpiryKey);
                return _anonymousState;
            }

            if (DateTime.UtcNow > expiry)
            {
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", ExpiryKey);
                return _anonymousState;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user-id"), new Claim(ClaimTypes.Name, "Usuário") };
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch
        {
            return _anonymousState;
        }
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        _isInitialized = true;
        var expiry = DateTime.UtcNow.Add(TokenLifetime).ToString("o", CultureInfo.InvariantCulture);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", TokenKey, token);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", ExpiryKey, expiry);

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user-id"), new Claim(ClaimTypes.Name, "Usuário") };
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", ExpiryKey);
        _httpClient.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymousState));
    }

    public async Task InitializeAsync()
    {
        _isInitialized = true;
        // Notify that auth state might have changed after initialization
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
