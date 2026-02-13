using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;
using System.ComponentModel.DataAnnotations;

namespace MoneyManager.Presentation.Controllers;

/// <summary>
/// Controller para gerenciamento de ativos de investimento.
/// </summary>
[ApiController]
[Route("api/investment-assets")]
[Authorize]
public class InvestmentAssetsController : ControllerBase
{
    private readonly IInvestmentAssetService _investmentAssetService;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<InvestmentAssetsController> _logger;

    public InvestmentAssetsController(
        IInvestmentAssetService investmentAssetService,
        IMarketDataService marketDataService,
        ILogger<InvestmentAssetsController> logger)
    {
        _investmentAssetService = investmentAssetService;
        _marketDataService = marketDataService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém todos os ativos de investimento do usuário.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var userId = HttpContext.GetUserId();
            _logger.LogDebug("Fetching all investment assets for user {UserId}", userId);

            var result = await _investmentAssetService.GetAllAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching investment assets");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtém um ativo de investimento específico por ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var userId = HttpContext.GetUserId();
            var result = await _investmentAssetService.GetByIdAsync(userId, id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Ativo de investimento não encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching investment asset {AssetId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cria um novo ativo de investimento.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvestmentAssetRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = HttpContext.GetUserId();
            _logger.LogInformation("Creating investment asset for user {UserId}: {AssetName}", userId, request.Name);

            var result = await _investmentAssetService.CreateAsync(userId, request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating investment asset");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating investment asset");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza informações de um ativo de investimento.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateInvestmentAssetRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = HttpContext.GetUserId();
            _logger.LogInformation("Updating investment asset {AssetId} for user {UserId}", id, userId);

            var result = await _investmentAssetService.UpdateAsync(userId, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Ativo de investimento não encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating investment asset {AssetId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deleta um ativo de investimento (soft delete).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var userId = HttpContext.GetUserId();
            _logger.LogInformation("Deleting investment asset {AssetId} for user {UserId}", id, userId);

            await _investmentAssetService.DeleteAsync(userId, id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Ativo de investimento não encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting investment asset {AssetId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Registra uma compra de ativo (aumenta quantidade e recalcula preço médio).
    /// </summary>
    [HttpPost("{id}/buy")]
    public async Task<IActionResult> Buy(string id, [FromBody] BuyAssetRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = HttpContext.GetUserId();
            _logger.LogInformation("Processing buy operation for asset {AssetId}, quantity: {Quantity}, price: {Price}", 
                id, request.Quantity, request.Price);

            var result = await _investmentAssetService.BuyAsync(userId, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Ativo de investimento não encontrado" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid buy operation for asset {AssetId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing buy operation for asset {AssetId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Registra uma venda de ativo (reduz quantidade e calcula lucro/prejuízo).
    /// </summary>
    [HttpPost("{id}/sell")]
    public async Task<IActionResult> Sell(string id, [FromBody] SellAssetRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = HttpContext.GetUserId();
            _logger.LogInformation("Processing sell operation for asset {AssetId}, quantity: {Quantity}, price: {Price}", 
                id, request.Quantity, request.Price);

            var result = await _investmentAssetService.SellAsync(userId, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Ativo de investimento não encontrado" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid sell operation for asset {AssetId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sell operation for asset {AssetId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Ajusta o preço de mercado de um ativo.
    /// </summary>
    [HttpPost("{id}/adjust-price")]
    public async Task<IActionResult> AdjustPrice(string id, [FromBody] AdjustPriceRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = HttpContext.GetUserId();
            _logger.LogInformation("Adjusting price for asset {AssetId} to {NewPrice}", id, request.NewPrice);

            var result = await _investmentAssetService.AdjustPriceAsync(userId, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Ativo de investimento não encontrado" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid price adjustment for asset {AssetId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting price for asset {AssetId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtém resumo consolidado de todos os investimentos do usuário.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        try
        {
            var userId = HttpContext.GetUserId();
            _logger.LogDebug("Fetching investment summary for user {UserId}", userId);

            var result = await _investmentAssetService.GetSummaryAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching investment summary");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza preços de mercado de todos os ativos com ticker (manual).
    /// </summary>
    [HttpPost("update-prices")]
    public async Task<IActionResult> UpdatePrices()
    {
        try
        {
            var userId = HttpContext.GetUserId();
            _logger.LogInformation("Manual price update requested by user {UserId}", userId);

            // Verificar se API está disponível
            var isAvailable = await _marketDataService.IsAvailableAsync();
            if (!isAvailable)
            {
                return ServiceUnavailable(new { message = "Serviço de cotações temporariamente indisponível" });
            }

            // Buscar ativos do usuário
            var assets = await _investmentAssetService.GetAllAsync(userId);
            var assetsWithTicker = assets
                .Where(a => !string.IsNullOrWhiteSpace(a.Ticker))
                .ToList();

            if (!assetsWithTicker.Any())
            {
                return Ok(new { 
                    message = "Nenhum ativo com ticker encontrado",
                    updated = 0,
                    skipped = 0
                });
            }

            int updated = 0;
            int skipped = 0;

            foreach (var asset in assetsWithTicker)
            {
                try
                {
                    var price = await _marketDataService.GetCurrentPriceAsync(
                        asset.Ticker!, 
                        asset.AssetType);

                    if (!price.HasValue || price.Value <= 0)
                    {
                        skipped++;
                        continue;
                    }

                    // Atualizar preço via serviço
                    await _investmentAssetService.AdjustPriceAsync(userId, asset.Id, new AdjustPriceRequestDto
                    {
                        NewPrice = price.Value,
                        Date = DateTime.UtcNow
                    });

                    updated++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error updating price for asset {AssetId} ({Ticker})", 
                        asset.Id, asset.Ticker);
                    skipped++;
                }
            }

            return Ok(new
            {
                message = $"Preços atualizados com sucesso",
                updated,
                skipped,
                total = assetsWithTicker.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in manual price update");
            return BadRequest(new { message = ex.Message });
        }
    }

    private ObjectResult ServiceUnavailable(object value)
    {
        return StatusCode(StatusCodes.Status503ServiceUnavailable, value);
    }
}

