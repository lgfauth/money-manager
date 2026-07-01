using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.Services;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        ISubscriptionService subscriptionService,
        ILogger<WebhooksController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Recebe eventos de recorrência do Pix Automático (idRec aprovado/cancelado).
    /// Registrar esta URL em PUT /v2/webhookrec na Efí Bank com scope webhookrec.write.
    /// </summary>
    [HttpPost("efi/rec")]
    public Task<IActionResult> EfiRec() => HandleEfiWebhook("rec");

    /// <summary>
    /// Recebe eventos de cobrança do Pix Automático (cobrança paga/expirada/cancelada).
    /// Registrar esta URL em PUT /v2/webhookcobr na Efí Bank com scope webhookcobr.write.
    /// </summary>
    [HttpPost("efi/cobr")]
    public Task<IActionResult> EfiCobr() => HandleEfiWebhook("cobr");

    // Mantido para compatibilidade com registros anteriores sem path suffix
    [HttpPost("efi")]
    public Task<IActionResult> Efi() => HandleEfiWebhook("generic");

    private async Task<IActionResult> HandleEfiWebhook(string eventPath)
    {
        using var reader = new StreamReader(Request.Body);
        var rawPayload = await reader.ReadToEndAsync();

        try
        {
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            await _subscriptionService.HandlePaymentWebhookAsync(rawPayload, headers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook Efí — path: {EventPath}", eventPath);
        }

        // Sempre 200 — evita retry storm do provedor mesmo em erros internos
        return Ok();
    }
}
