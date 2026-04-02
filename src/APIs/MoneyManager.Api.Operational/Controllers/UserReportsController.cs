using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserReportsController : ControllerBase
{
    private readonly IUserReportService _userReportService;
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Elogio",
        "Reclamação",
        "Sugestão de melhoria",
        "Encontrei um problema"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "video/mp4", "video/webm", "video/quicktime"
    };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public UserReportsController(IUserReportService userReportService)
    {
        _userReportService = userReportService;
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSize + 1024 * 100)] // file + form fields
    public async Task<IActionResult> Create([FromForm] string category, [FromForm] string description, IFormFile? attachment)
    {
        if (string.IsNullOrWhiteSpace(category) || !AllowedCategories.Contains(category))
            return this.ApiBadRequest("Categoria inválida.");

        if (string.IsNullOrWhiteSpace(description) || description.Length < 10)
            return this.ApiBadRequest("A descrição deve ter pelo menos 10 caracteres.");

        if (description.Length > 5000)
            return this.ApiBadRequest("A descrição deve ter no máximo 5000 caracteres.");

        string? attachmentUrl = null;
        string? attachmentFileName = null;

        if (attachment != null)
        {
            if (attachment.Length > MaxFileSize)
                return this.ApiBadRequest("Arquivo muito grande. Máximo 10 MB.");

            if (!AllowedMimeTypes.Contains(attachment.ContentType))
                return this.ApiBadRequest("Tipo de arquivo não suportado. Envie imagem ou vídeo.");

            using var ms = new MemoryStream();
            await attachment.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());
            attachmentUrl = $"data:{attachment.ContentType};base64,{base64}";
            attachmentFileName = attachment.FileName;
        }

        var userId = HttpContext.GetUserId();
        var userName = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Usuário";

        var report = await _userReportService.CreateAsync(userId, userName, category, description, attachmentUrl, attachmentFileName);
        return CreatedAtAction(nameof(GetMine), null, report);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var userId = HttpContext.GetUserId();
        var reports = await _userReportService.GetByUserAsync(userId);
        return Ok(reports);
    }
}
