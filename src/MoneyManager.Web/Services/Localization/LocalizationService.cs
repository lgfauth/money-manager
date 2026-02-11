using System.Net.Http.Json;
using Blazored.LocalStorage;

namespace MoneyManager.Web.Services.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private const string LANGUAGE_KEY = "preferred_language";

    private Dictionary<string, object> _resources = new(StringComparer.OrdinalIgnoreCase);

    public string CurrentCulture { get; private set; } = "pt-BR";

    public event Action? OnLanguageChanged;

    public LocalizationService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task InitializeAsync()
    {
        // Tentar carregar idioma salvo no localStorage
        var savedLanguage = await _localStorage.GetItemAsync<string>(LANGUAGE_KEY);
        
        if (!string.IsNullOrEmpty(savedLanguage))
        {
            CurrentCulture = savedLanguage;
        }
        else
        {
            // Detectar idioma do navegador como fallback
            CurrentCulture = DetectBrowserLanguage();
        }

        await LoadAsync(CurrentCulture);
    }

    public async Task SetCultureAsync(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return;
        }

        CurrentCulture = culture;
        
        // Salvar no localStorage
        await _localStorage.SetItemAsync(LANGUAGE_KEY, culture);
        
        // Recarregar recursos
        await LoadAsync(culture);
        
        // Notificar mudança
        OnLanguageChanged?.Invoke();
    }

    private string DetectBrowserLanguage()
    {
        // Por padrão, retorna pt-BR
        // TODO: Implementar detecção via JavaScript Interop se necessário
        return "pt-BR";
    }

    public string Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        if (TryGetValue(key, out var value) && value is string s)
        {
            return s;
        }

        return key;
    }

    public string Get(string key, params object[] args)
    {
        var value = Get(key);
        if (args is { Length: > 0 })
        {
            try
            {
                return string.Format(value, args);
            }
            catch
            {
                // ignore format errors
            }
        }

        return value;
    }

    private async Task LoadAsync(string culture)
    {
        // BaseAddress is the API URL; use absolute-path to read static files from same origin when hosted.
        // This works in development and in hosted scenarios where the WASM is served from the web host.
        var path = $"i18n/{culture}.json";

        Dictionary<string, object>? dict = null;
        try
        {
            dict = await _httpClient.GetFromJsonAsync<Dictionary<string, object>>(path);
        }
        catch
        {
            // Fallback: keep empty dictionary
        }

        _resources = dict ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    private bool TryGetValue(string key, out object? value)
    {
        var parts = key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        object current = _resources;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> d)
            {
                if (!d.TryGetValue(part, out var next) || next is null)
                {
                    value = null;
                    return false;
                }

                current = next;
                continue;
            }

            value = null;
            return false;
        }

        value = current;
        return true;
    }
}
