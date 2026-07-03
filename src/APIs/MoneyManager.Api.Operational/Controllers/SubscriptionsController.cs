using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IValidator<CreateSubscriptionRequestDto> _validator;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        IValidator<CreateSubscriptionRequestDto> validator,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _validator = validator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequestDto request)
    {
        var userId = HttpContext.GetUserId();

        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _subscriptionService.CreateAsync(userId, request);
            _logger.LogInformation("Assinatura premium iniciada para usuário {UserId}", userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound();
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var userId = HttpContext.GetUserId();

        try
        {
            return Ok(await _subscriptionService.GetByUserIdAsync(userId));
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound();
        }
    }

    [HttpDelete("me")]
    public async Task<IActionResult> Cancel()
    {
        var userId = HttpContext.GetUserId();

        try
        {
            await _subscriptionService.CancelAsync(userId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound();
        }
    }
}
