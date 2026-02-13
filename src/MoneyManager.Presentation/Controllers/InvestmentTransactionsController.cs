using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

/// <summary>
/// Controller para gerenciamento de transações de investimento.
/// </summary>
[ApiController]
[Route("api/investment-transactions")]
[Authorize]
public class InvestmentTransactionsController : ControllerBase
{
    private readonly IInvestmentTransactionService _investmentTransactionService;
    private readonly ILogger<InvestmentTransactionsController> _logger;

    public InvestmentTransactionsController(
        IInvestmentTransactionService investmentTransactionService,
        ILogger<InvestmentTransactionsController> logger)
    {
        _investmentTransactionService = investmentTransactionService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém histórico de transações de investimento do usuário com filtros opcionais.
    /// </summary>
    /// <param name="startDate">Data inicial (opcional)</param>
    /// <param name="endDate">Data final (opcional)</param>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = HttpContext.GetUserId();
            _logger.LogDebug("Fetching investment transactions for user {UserId}", userId);

            var result = await _investmentTransactionService.GetByUserIdAsync(userId, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching investment transactions");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtém transações de um ativo específico.
    /// </summary>
    [HttpGet("asset/{assetId}")]
    public async Task<IActionResult> GetByAssetId(string assetId)
    {
        try
        {
            _logger.LogDebug("Fetching transactions for asset {AssetId}", assetId);

            var result = await _investmentTransactionService.GetByAssetIdAsync(assetId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transactions for asset {AssetId}", assetId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Registra um rendimento (dividendo, juros, aluguel) de um ativo.
    /// </summary>
    [HttpPost("yield")]
    public async Task<IActionResult> RecordYield([FromBody] RecordYieldRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = HttpContext.GetUserId();
            _logger.LogInformation("Recording yield for asset {AssetId}, amount: {Amount}, type: {YieldType}", 
                request.AssetId, request.Amount, request.YieldType);

            var result = await _investmentTransactionService.RecordYieldAsync(userId, request);
            return CreatedAtAction(nameof(GetByAssetId), new { assetId = request.AssetId }, result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Ativo de investimento não encontrado" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid yield operation");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording yield");
            return BadRequest(new { message = ex.Message });
        }
    }
}
