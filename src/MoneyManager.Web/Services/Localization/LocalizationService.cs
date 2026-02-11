using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace MoneyManager.Web.Services.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private readonly IWebAssemblyHostEnvironment _hostEnvironment;
    private readonly ILocalStorageService _localStorage;
    private const string LANGUAGE_KEY = "preferred_language";

    private Dictionary<string, object> _resources = new(StringComparer.OrdinalIgnoreCase);

    public string CurrentCulture { get; private set; } = "pt-BR";

    public event Action? OnLanguageChanged;

    public LocalizationService(IWebAssemblyHostEnvironment hostEnvironment, ILocalStorageService localStorage)
    {
        _hostEnvironment = hostEnvironment;
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
        var path = $"{_hostEnvironment.BaseAddress}i18n/{culture}.json";

        Dictionary<string, object>? dict = null;
        try
        {
            using var httpClient = new HttpClient();
            dict = await httpClient.GetFromJsonAsync<Dictionary<string, object>>(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalizationService] Erro ao carregar {path}: {ex.Message}");
            // Fallback: keep empty dictionary
        }

        _resources = dict ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        
        if (_resources.Any())
        {
            Console.WriteLine($"[LocalizationService] Carregado {_resources.Count} seções de {culture}");
        }
        else
        {
            Console.WriteLine($"[LocalizationService] AVISO: Nenhum recurso carregado de {culture}");
        }
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
