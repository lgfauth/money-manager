using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

public interface IPushNotificationService
{
    /// <summary>
    /// Full flow: registers the SW, requests permission, subscribes and saves to the API.
    /// Returns 'success', 'permission-denied', 'not-supported', or 'error'.
    /// </summary>
    Task<string> InitAsync();

    /// <summary>Unsubscribes the current device. Returns true on success, false on failure.</summary>
    Task<bool> UnsubscribeAsync();

    /// <summary>Returns the current notification permission state.</summary>
    Task<string> GetPermissionStateAsync();

    /// <summary>Returns true if the server has at least one active subscription for the current user.</summary>
    Task<bool> GetServerStatusAsync();
}

public class PushNotificationService : IPushNotificationService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly CustomAuthenticationStateProvider _authProvider;

    public PushNotificationService(
        HttpClient http,
        IJSRuntime js,
        AuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _js = js;
        _authProvider = (CustomAuthenticationStateProvider)authStateProvider;
    }

    public async Task<string> InitAsync()
    {
        string vapidPublicKey;
        try
        {
            var dto = await _http.GetFromJsonAsync<VapidPublicKeyDto>("api/push/public-key");
            vapidPublicKey = dto?.PublicKey ?? string.Empty;
        }
        catch
        {
            return "error";
        }

        if (string.IsNullOrWhiteSpace(vapidPublicKey))
            return "error";

        var token = await _authProvider.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return "error";

        return await _js.InvokeAsync<string>("pushManager.initPush", vapidPublicKey, token);
    }

    public async Task<bool> UnsubscribeAsync()
    {
        var token = await _authProvider.GetTokenAsync();
        return await _js.InvokeAsync<bool>("pushManager.unsubscribeFromPush", token ?? string.Empty);
    }

    public async Task<string> GetPermissionStateAsync()
    {
        return await _js.InvokeAsync<string>("pushManager.getPermissionState");
    }

    public async Task<bool> GetServerStatusAsync()
    {
        try
        {
            var dto = await _http.GetFromJsonAsync<PushStatusDto>("api/push/status");
            return dto?.Active ?? false;
        }
        catch
        {
            return false;
        }
    }

    private sealed record VapidPublicKeyDto(string PublicKey);
    private sealed record PushStatusDto(bool Active);
}

