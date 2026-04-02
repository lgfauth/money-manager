using System.Text.Json;
using System.Text.Json.Serialization;
using MoneyManager.Api.Administration.Models;

namespace MoneyManager.Api.Administration.Services;

public sealed class LegalDocumentService
{
    private static readonly HashSet<string> AllowedSlugs = ["termos", "privacidade"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _basePath;
    private readonly ILogger<LegalDocumentService> _logger;

    public LegalDocumentService(IConfiguration configuration, ILogger<LegalDocumentService> logger)
    {
        _basePath = configuration["DocumentsPath"]
            ?? Environment.GetEnvironmentVariable("DOCUMENTS_PATH")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "data", "documents");

        _logger = logger;
    }

    public static bool IsValidSlug(string slug) => AllowedSlugs.Contains(slug);

    public async Task<LegalDocument> GetAsync(string slug)
    {
        var path = FilePath(slug);

        if (!File.Exists(path))
        {
            return CreatePlaceholder(slug);
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<LegalDocument>(json, JsonOptions)
                   ?? CreatePlaceholder(slug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read legal document '{Slug}' from {Path}", slug, path);
            return CreatePlaceholder(slug);
        }
    }

    public async Task SaveAsync(LegalDocument document)
    {
        Directory.CreateDirectory(_basePath);

        var path = FilePath(document.Slug);
        var json = JsonSerializer.Serialize(document, JsonOptions);

        await File.WriteAllTextAsync(path, json);

        _logger.LogInformation(
            "Legal document '{Slug}' v{Version} saved at {Path} by {UpdatedBy}",
            document.Slug, document.Version, path, document.UpdatedBy);
    }

    public async Task<IReadOnlyList<LegalDocument>> ListAllAsync()
    {
        var results = new List<LegalDocument>();

        foreach (var slug in AllowedSlugs)
        {
            results.Add(await GetAsync(slug));
        }

        return results;
    }

    // -------------------------------------------------------------------------

    private string FilePath(string slug) =>
        Path.Combine(_basePath, $"{slug}.json");

    private static LegalDocument CreatePlaceholder(string slug)
    {
        var title = slug == "termos" ? "Termos de Uso" : "Politica de Privacidade";

        return new LegalDocument
        {
            Slug = slug,
            Title = title,
            Content = $"# {title}\n\n[Conteudo a ser preenchido pelo administrador]",
            Version = "1.0",
            LastUpdatedAt = DateTime.UtcNow,
            UpdatedBy = "system"
        };
    }
}
