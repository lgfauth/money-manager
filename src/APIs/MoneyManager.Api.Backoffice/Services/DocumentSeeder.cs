using MoneyManager.Api.Administration.Services;

namespace MoneyManager.Api.Administration.Services;

public sealed class DocumentSeeder
{
    private static readonly string[] Slugs = ["termos", "privacidade"];

    private readonly LegalDocumentService _documents;
    private readonly ILogger<DocumentSeeder> _logger;

    public DocumentSeeder(LegalDocumentService documents, ILogger<DocumentSeeder> logger)
    {
        _documents = documents;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        foreach (var slug in Slugs)
        {
            var doc = await _documents.GetAsync(slug);

            // If the file does not exist on disk it returns a placeholder with UpdatedBy = "system"
            // (the same value we set in CreatePlaceholder). We only write when there's nothing yet.
            if (doc.UpdatedBy == "system")
            {
                await _documents.SaveAsync(doc);
                _logger.LogInformation("Seeded placeholder for legal document '{Slug}'", slug);
            }
        }
    }
}
