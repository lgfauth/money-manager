using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/bank-connections")]
[Authorize]
public class BankConnectionsController : ControllerBase
{
    private readonly IBankConnectionService _bankConnectionService;
    private readonly IValidator<CompleteOnboardingRequestDto> _validator;
    private readonly ILogger<BankConnectionsController> _logger;

    public BankConnectionsController(
        IBankConnectionService bankConnectionService,
        IValidator<CompleteOnboardingRequestDto> validator,
        ILogger<BankConnectionsController> logger)
    {
        _bankConnectionService = bankConnectionService;
        _validator = validator;
        _logger = logger;
    }

    // POST /api/bank-connections/api-key — usuário salva sua key do Banco MCP.
    [HttpPost("api-key")]
    public async Task<IActionResult> SaveApiKey([FromBody] SaveBankMcpApiKeyRequestDto request, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        try
        {
            var result = await _bankConnectionService.SaveBankMcpApiKeyAsync(userId, request.ApiKey, ct);
            return Ok(result);
        }
        catch (PremiumRequiredException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    // GET /api/bank-connections/available — lista conexões disponíveis na conta Banco MCP do usuário.
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableConnections(CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        try
        {
            var result = await _bankConnectionService.GetAvailableConnectionsAsync(userId, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    // POST /api/bank-connections/register — usuário registra uma conexão disponível.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterConnectionRequestDto request, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        try
        {
            var result = await _bankConnectionService.RegisterConnectionAsync(userId, request.ExternalConnectionId, ct);
            return Ok(result);
        }
        catch (PremiumRequiredException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    // GET /api/bank-connections — lista conexões do usuário.
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        var result = await _bankConnectionService.GetUserConnectionsAsync(userId, ct);
        return Ok(result);
    }

    // GET /api/bank-connections/{id}/accounts — accounts disponíveis para mapeamento.
    [HttpGet("{id}/accounts")]
    public async Task<IActionResult> GetAvailableAccounts(string id, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        try
        {
            var result = await _bankConnectionService.GetAvailableAccountsAsync(userId, id, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound();
        }
        catch (InvalidOperationException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    // POST /api/bank-connections/{id}/onboarding — salva mapeamento + estratégia + dispara sync.
    [HttpPost("{id}/onboarding")]
    public async Task<IActionResult> CompleteOnboarding(
        string id, [FromBody] CompleteOnboardingRequestDto request, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();

        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _bankConnectionService.CompleteOnboardingAsync(userId, id, request, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound();
        }
        catch (InvalidOperationException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    // POST /api/bank-connections/{id}/sync — sync manual do usuário.
    [HttpPost("{id}/sync")]
    public async Task<IActionResult> SyncNow(string id, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        try
        {
            await _bankConnectionService.SyncNowAsync(userId, id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound();
        }
        catch (InvalidOperationException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    // DELETE /api/bank-connections/{id} — desconecta.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Disconnect(string id, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        try
        {
            await _bankConnectionService.DisconnectAsync(userId, id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound();
        }
    }
}
