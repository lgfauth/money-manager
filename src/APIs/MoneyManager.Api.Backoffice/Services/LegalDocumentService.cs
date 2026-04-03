using MongoDB.Driver;
using MoneyManager.Api.Administration.Models;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Api.Administration.Services;

public sealed class LegalDocumentService
{
    private static readonly HashSet<string> AllowedSlugs = ["termos", "privacidade"];

    private readonly IMongoCollection<LegalDocument> _collection;
    private readonly ILogger<LegalDocumentService> _logger;

    public LegalDocumentService(MongoContext mongoContext, ILogger<LegalDocumentService> logger)
    {
        _collection = mongoContext.GetCollection<LegalDocument>("legal_documents");
        _logger = logger;
    }

    public static bool IsValidSlug(string slug) => AllowedSlugs.Contains(slug);

    public async Task<LegalDocument> GetAsync(string slug)
    {
        try
        {
            var doc = await _collection.Find(d => d.Slug == slug).FirstOrDefaultAsync();
            return doc ?? CreatePlaceholder(slug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read legal document '{Slug}' from MongoDB", slug);
            return CreatePlaceholder(slug);
        }
    }

    public async Task SaveAsync(LegalDocument document)
    {
        var filter = Builders<LegalDocument>.Filter.Eq(d => d.Slug, document.Slug);
        var options = new ReplaceOptions { IsUpsert = true };

        await _collection.ReplaceOneAsync(filter, document, options);

        _logger.LogInformation(
            "Legal document '{Slug}' v{Version} saved to MongoDB by {UpdatedBy}",
            document.Slug, document.Version, document.UpdatedBy);
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
