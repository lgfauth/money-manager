using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/receipts")]
[Authorize]
public class ReceiptsController : ControllerBase
{
    private static readonly HashSet<string> SupportedMimeTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IReceiptAnalysisService _receiptAnalysisService;

    public ReceiptsController(IReceiptAnalysisService receiptAnalysisService)
    {
        _receiptAnalysisService = receiptAnalysisService;
    }

    [HttpPost("analyze")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Analyze([FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return this.ApiBadRequest("Arquivo não enviado ou está vazio.");

        if (file.Length > MaxFileSizeBytes)
            return this.ApiBadRequest("Arquivo excede o tamanho máximo permitido de 10 MB.");

        var mimeType = file.ContentType?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(mimeType) || !SupportedMimeTypes.Contains(mimeType))
            return this.ApiBadRequest("Tipo de arquivo não suportado. Envie uma imagem JPEG, PNG ou WebP.");

        string fileBase64;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            fileBase64 = Convert.ToBase64String(ms.ToArray());
        }

        try
        {
            var result = await _receiptAnalysisService.AnalyzeAsync(fileBase64, mimeType);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("indisponível"))
        {
            return this.ApiError(StatusCodes.Status502BadGateway, ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Falha"))
        {
            return this.ApiError(StatusCodes.Status502BadGateway, ex.Message);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }
}
