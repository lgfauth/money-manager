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

    // GET /api/bank-connections — lista conexões do usuário.
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        var result = await _bankConnectionService.GetUserConnectionsAsync(userId, ct);
        return Ok(result);
    }

    // GET /api/bank-connections/invite — URL para o usuário conectar bancos no Banco MCP.
    [HttpGet("invite")]
    public async Task<IActionResult> GetInviteUrl(CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        try
        {
            var result = await _bankConnectionService.GetUserInviteUrlAsync(userId, ct);
            return Ok(result);
        }
        catch (PremiumRequiredException)
        {
            return Forbid();
        }
    }

    // GET /api/bank-connections/available — lista conexões disponíveis no workspace para o usuário registrar.
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableConnections(CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        try
        {
            var result = await _bankConnectionService.GetAvailableConnectionsAsync(userId, ct);
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

    // POST /api/bank-connections — registra uma conexão (item_id) para o usuário.
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterBankConnectionRequestDto request, CancellationToken ct)
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

    // GET /api/bank-connections/{id}/accounts — contas disponíveis para mapeamento.
    [HttpGet("{id}/accounts")]
    public async Task<IActionResult> GetAccounts(string id, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        try
        {
            var result = await _bankConnectionService.GetConnectionAccountsAsync(userId, id, ct);
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

    // POST /api/bank-connections/{id}/onboarding — salva mapeamento + estratégia + primeiro sync.
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

    // POST /api/bank-connections/{id}/sync — sync manual.
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
