using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Presentation.Models;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private static readonly HashSet<string> AllowedSlugs = ["termos", "privacidade"];

    private readonly IMongoCollection<LegalDocument> _collection;

    public DocumentsController(MongoContext mongoContext)
    {
        _collection = mongoContext.GetCollection<LegalDocument>("legal_documents");
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug)
    {
        if (!AllowedSlugs.Contains(slug))
            return NotFound(new { message = $"Documento '{slug}' nao encontrado" });

        var doc = await _collection.Find(d => d.Slug == slug).FirstOrDefaultAsync()
                  ?? CreatePlaceholder(slug);

        Response.Headers["Cache-Control"] = "public, max-age=300";

        return Ok(new
        {
            doc.Slug,
            doc.Title,
            doc.Content,
            doc.Version,
            doc.LastUpdatedAt
        });
    }

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
