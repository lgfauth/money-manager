using Microsoft.AspNetCore.Mvc;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DocumentsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug)
    {
        var allowedSlugs = new HashSet<string> { "termos", "privacidade" };
        if (!allowedSlugs.Contains(slug))
            return NotFound(new { message = $"Documento '{slug}' nao encontrado" });

        var backofficeBaseUrl = _configuration["BackofficeBaseUrl"]
            ?? Environment.GetEnvironmentVariable("BACKOFFICE_BASE_URL");

        if (string.IsNullOrWhiteSpace(backofficeBaseUrl))
        {
            _logger.LogWarning("BackofficeBaseUrl is not configured; cannot proxy legal document '{Slug}'", slug);
            return StatusCode(503, new { message = "Documento temporariamente indisponivel" });
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("backoffice");
            var response = await httpClient.GetAsync($"api/documents/{slug}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Backoffice returned {Status} for document '{Slug}'",
                    response.StatusCode, slug);
                return StatusCode(503, new { message = "Documento temporariamente indisponivel" });
            }

            Response.Headers["Cache-Control"] = "public, max-age=300";

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying legal document '{Slug}' from backoffice", slug);
            return StatusCode(503, new { message = "Documento temporariamente indisponivel" });
        }
    }
}
