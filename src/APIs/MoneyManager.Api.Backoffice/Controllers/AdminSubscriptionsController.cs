using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Administration.Models;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;

namespace MoneyManager.Api.Administration.Controllers;

[ApiController]
[Route("api/admin/subscriptions")]
[Authorize(Policy = AdminPolicies.Operator)]
public sealed class AdminSubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IValidator<ActivatePremiumRequestDto> _validator;
    private readonly ILogger<AdminSubscriptionsController> _logger;

    public AdminSubscriptionsController(
        ISubscriptionService subscriptionService,
        IValidator<ActivatePremiumRequestDto> validator,
        ILogger<AdminSubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _validator = validator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _subscriptionService.GetAllForAdminAsync(page, pageSize);
        return Ok(result);
    }

    [HttpPost("{userId}/activate")]
    [Authorize(Policy = AdminPolicies.Admin)]
    public async Task<IActionResult> Activate(string userId, [FromBody] ActivatePremiumRequestDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new
            {
                message = "Dados inválidos",
                errors = validation.Errors.Select(e => e.ErrorMessage)
            });

        var adminUsername = User.Identity?.Name ?? "unknown";

        try
        {
            var result = await _subscriptionService.ActivatePremiumManuallyAsync(userId, request.DurationDays, adminUsername);
            _logger.LogInformation("Premium ativado para {UserId} via admin {AdminUsername}", userId, adminUsername);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Usuário não encontrado" });
        }
    }

    [HttpPost("{userId}/revoke")]
    [Authorize(Policy = AdminPolicies.Admin)]
    public async Task<IActionResult> Revoke(string userId)
    {
        try
        {
            var result = await _subscriptionService.RevokePremiumManuallyAsync(userId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Assinatura não encontrada para o usuário" });
        }
    }
}
