using System.Security.Claims;
using System.Text;
using Markdig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Administration.Models;
using MoneyManager.Api.Administration.Services;

namespace MoneyManager.Api.Administration.Controllers;

// ---------------------------------------------------------------------------
//  PUBLIC — DocumentsController  GET /api/documents/{slug}
// ---------------------------------------------------------------------------
[ApiController]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly LegalDocumentService _documents;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(LegalDocumentService documents, ILogger<DocumentsController> logger)
    {
        _documents = documents;
        _logger = logger;
    }

    [HttpGet("{slug}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> Get(string slug)
    {
        if (!LegalDocumentService.IsValidSlug(slug))
            return NotFound(new { message = $"Documento '{slug}' nao encontrado" });

        Response.Headers["Cache-Control"] = "public, max-age=300";

        var doc = await _documents.GetAsync(slug);

        return Ok(new
        {
            doc.Slug,
            doc.Title,
            doc.Content,
            doc.Version,
            doc.LastUpdatedAt
        });
    }
}

// ---------------------------------------------------------------------------
//  ADMIN — AdminDocumentsController  /api/admin/documents
// ---------------------------------------------------------------------------
[ApiController]
[Route("api/admin/documents")]
[Authorize]
public sealed class AdminDocumentsController : ControllerBase
{
    private readonly LegalDocumentService _documents;
    private readonly ILogger<AdminDocumentsController> _logger;

    public AdminDocumentsController(LegalDocumentService documents, ILogger<AdminDocumentsController> logger)
    {
        _documents = documents;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var docs = await _documents.ListAllAsync();
        return Ok(docs.Select(d => new
        {
            d.Slug,
            d.Title,
            d.Version,
            d.LastUpdatedAt,
            d.UpdatedBy
        }));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetForEdit(string slug)
    {
        if (!LegalDocumentService.IsValidSlug(slug))
            return NotFound(new { message = $"Documento '{slug}' nao encontrado" });

        var doc = await _documents.GetAsync(slug);
        return Ok(doc);
    }

    [HttpPut("{slug}")]
    public async Task<IActionResult> Update(string slug, [FromBody] UpdateLegalDocumentRequest request)
    {
        if (!LegalDocumentService.IsValidSlug(slug))
            return NotFound(new { message = $"Documento '{slug}' nao encontrado" });

        var adminEmail = User.FindFirstValue(ClaimTypes.Name)
                      ?? User.FindFirstValue(ClaimTypes.Email)
                      ?? User.Identity?.Name
                      ?? "unknown";

        var doc = new LegalDocument
        {
            Slug = slug,
            Title = request.Title,
            Content = request.Content,
            Version = request.Version,
            LastUpdatedAt = DateTime.UtcNow,
            UpdatedBy = adminEmail
        };

        await _documents.SaveAsync(doc);

        _logger.LogInformation(
            "Legal document '{Slug}' updated to v{Version} by {Admin}",
            slug, doc.Version, adminEmail);

        return Ok(doc);
    }

    [HttpPost("{slug}/preview")]
    public IActionResult Preview(string slug, [FromBody] PreviewLegalDocumentRequest request)
    {
        if (!LegalDocumentService.IsValidSlug(slug))
            return NotFound(new { message = $"Documento '{slug}' nao encontrado" });

        var html = Markdown.ToHtml(request.Content ?? string.Empty);
        return Content(html, "text/html", Encoding.UTF8);
    }
}
