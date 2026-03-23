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

    /// <summary>Unsubscribes the current device from push notifications.</summary>
    Task UnsubscribeAsync();

    /// <summary>Returns the current notification permission state.</summary>
    Task<string> GetPermissionStateAsync();
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

    public async Task UnsubscribeAsync()
    {
        var token = await _authProvider.GetTokenAsync();
        await _js.InvokeVoidAsync("pushManager.unsubscribeFromPush", token ?? string.Empty);
    }

    public async Task<string> GetPermissionStateAsync()
    {
        return await _js.InvokeAsync<string>("pushManager.getPermissionState");
    }

    private sealed record VapidPublicKeyDto(string PublicKey);
}

