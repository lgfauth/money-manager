using Microsoft.JSInterop;

namespace MoneyManager.Web.Services;

public interface IApiConfigService
{
    Task<string> GetApiUrlAsync();
}

public class ApiConfigService : IApiConfigService
{
    private readonly IJSRuntime _jsRuntime;
    private string? _cachedApiUrl;

    public ApiConfigService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetApiUrlAsync()
    {
        if (_cachedApiUrl != null)
            return _cachedApiUrl;

        try
        {
            var configApiUrl = await _jsRuntime.InvokeAsync<string>("eval", "window.blazorConfig?.apiUrl || ''");
            
            if (!string.IsNullOrEmpty(configApiUrl) && configApiUrl != "__API_URL__")
            {
                _cachedApiUrl = configApiUrl;
                Console.WriteLine($"[MoneyManager] API URL from config: {_cachedApiUrl}");
                return _cachedApiUrl;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MoneyManager] Error reading API URL from JS: {ex.Message}");
        }

        // Fallback to default
        _cachedApiUrl = "https://localhost:5001";
        Console.WriteLine($"[MoneyManager] Using default API URL: {_cachedApiUrl}");
        return _cachedApiUrl;
    }
}
