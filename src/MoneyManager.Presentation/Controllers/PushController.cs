using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/push")]
[Authorize]
public class PushController : ControllerBase
{
    private readonly IPushService _pushService;
    private readonly VapidSettings _vapid;
    private readonly ILogger<PushController> _logger;

    public PushController(
        IPushService pushService,
        Microsoft.Extensions.Options.IOptions<VapidSettings> vapidOptions,
        ILogger<PushController> logger)
    {
        _pushService = pushService;
        _vapid = vapidOptions.Value;
        _logger = logger;
    }

    /// <summary>Returns the VAPID public key so the browser can subscribe.</summary>
    [HttpGet("public-key")]
    [AllowAnonymous]
    public IActionResult GetPublicKey()
    {
        return Ok(new { publicKey = _vapid.PublicKey });
    }

    /// <summary>Saves or updates a push subscription for the authenticated user.</summary>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint) ||
            string.IsNullOrWhiteSpace(request.P256dh) ||
            string.IsNullOrWhiteSpace(request.Auth))
        {
            return BadRequest(new { message = "Endpoint, p256dh and auth are required." });
        }

        var userId = HttpContext.GetUserId();
        _logger.LogInformation("Push subscribe request from user {UserId}", userId);

        var result = await _pushService.SubscribeAsync(userId, request);
        return Ok(result);
    }

    /// <summary>Removes a push subscription by endpoint.</summary>
    [HttpDelete("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint))
            return BadRequest(new { message = "Endpoint is required." });

        var userId = HttpContext.GetUserId();
        _logger.LogInformation("Push unsubscribe request from user {UserId}", userId);

        await _pushService.UnsubscribeAsync(userId, request.Endpoint);
        return NoContent();
    }

    /// <summary>Sends a test push notification to all subscriptions of the authenticated user.</summary>
    [HttpPost("send-test")]
    public async Task<IActionResult> SendTest()
    {
        var userId = HttpContext.GetUserId();
        _logger.LogInformation("Push send-test request from user {UserId}", userId);

        await _pushService.SendTestAsync(userId);
        return Ok(new { message = "Test notification sent." });
    }
}
