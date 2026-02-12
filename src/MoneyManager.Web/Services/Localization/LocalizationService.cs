using System.Net.Http.Json;
using System.Text.Json;
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
        Console.WriteLine($"[LocalizationService] Inicializando... BaseAddress: {_hostEnvironment.BaseAddress}");
        
        // Tentar carregar idioma salvo no localStorage
        var savedLanguage = await _localStorage.GetItemAsync<string>(LANGUAGE_KEY);
        
        if (!string.IsNullOrEmpty(savedLanguage))
        {
            CurrentCulture = savedLanguage;
            Console.WriteLine($"[LocalizationService] Idioma salvo encontrado: {savedLanguage}");
        }
        else
        {
            // Detectar idioma do navegador como fallback
            CurrentCulture = DetectBrowserLanguage();
            Console.WriteLine($"[LocalizationService] Usando idioma padrão: {CurrentCulture}");
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
        var path = $"i18n/{culture}.json";
        
        Console.WriteLine($"[LocalizationService] BaseAddress: {_hostEnvironment.BaseAddress}");
        Console.WriteLine($"[LocalizationService] Carregando: {path}");

        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(_hostEnvironment.BaseAddress) };
            
            // Fazer request com encoding UTF-8 explícito
            using var response = await httpClient.GetAsync(path);
            response.EnsureSuccessStatusCode();
            
            // Ler como bytes e decodificar com UTF-8
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var jsonString = System.Text.Encoding.UTF8.GetString(bytes);
            
            // Parsear como JsonDocument primeiro
            using var doc = JsonDocument.Parse(jsonString);
            _resources = ParseJsonElement(doc.RootElement);
            
            Console.WriteLine($"[LocalizationService] ✅ Carregado {_resources.Count} seções");
            Console.WriteLine($"[LocalizationService] Seções: {string.Join(", ", _resources.Keys)}");
            
            // Testar acesso a Login.Title
            if (TryGetValue("Login.Title", out var testValue))
            {
                Console.WriteLine($"[LocalizationService] ✅ Teste Login.Title = {testValue}");
            }
            else
            {
                Console.WriteLine($"[LocalizationService] ❌ Login.Title não encontrado!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalizationService] ❌ Erro: {ex.Message}");
            _resources = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private Dictionary<string, object> ParseJsonElement(JsonElement element)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.Object => ParseJsonElement(property.Value),
                JsonValueKind.Array => property.Value.EnumerateArray()
                    .Select(e => e.ValueKind == JsonValueKind.Object ? ParseJsonElement(e) : (object)e.ToString())
                    .ToArray(),
                JsonValueKind.String => property.Value.GetString() ?? "",
                JsonValueKind.Number => property.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                _ => property.Value.ToString()
            };
        }

        return dict;
    }

    private bool TryGetValue(string key, out object? value)
    {
        var parts = key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        object current = _resources;

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            
            if (current is Dictionary<string, object> d)
            {
                if (!d.TryGetValue(part, out var next) || next is null)
                {
                    Console.WriteLine($"[LocalizationService] ❌ Chave '{part}' não encontrada em '{string.Join(".", parts.Take(i))}'");
                    value = null;
                    return false;
                }

                current = next;
                continue;
            }

            Console.WriteLine($"[LocalizationService] ❌ '{string.Join(".", parts.Take(i))}' não é um dicionário");
            value = null;
            return false;
        }

        value = current;
        return true;
    }
}
