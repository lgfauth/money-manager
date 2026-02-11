using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/credit-card-invoices")]
[Authorize]
public class CreditCardInvoicesController : ControllerBase
{
    private readonly ICreditCardInvoiceService _invoiceService;
    private readonly ILogger<CreditCardInvoicesController> _logger;

    public CreditCardInvoicesController(
        ICreditCardInvoiceService invoiceService,
        ILogger<CreditCardInvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    // ==================== GESTÃO DE FATURAS ====================

    /// <summary>
    /// Busca ou cria fatura aberta de um cartão
    /// </summary>
    [HttpGet("accounts/{accountId}/open")]
    public async Task<IActionResult> GetOrCreateOpenInvoice(string accountId)
    {
        var userId = GetUserId();
        _logger.LogInformation("Getting or creating open invoice for account {AccountId}, user {UserId}", accountId, userId);

        try
        {
            var invoice = await _invoiceService.GetOrCreateOpenInvoiceAsync(userId, accountId);
            return Ok(invoice);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Account {AccountId} not found for user {UserId}: {Message}", accountId, userId, ex.Message);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating open invoice for account {AccountId}", accountId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Busca fatura por ID
    /// </summary>
    [HttpGet("{invoiceId}")]
    public async Task<IActionResult> GetById(string invoiceId)
    {
        var userId = GetUserId();
        _logger.LogDebug("Getting invoice {InvoiceId} for user {UserId}", invoiceId, userId);

        try
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(userId, invoiceId);
            return Ok(invoice);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for user {UserId}", invoiceId, userId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice {InvoiceId}", invoiceId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Busca todas as faturas de um cartão
    /// </summary>
    [HttpGet("accounts/{accountId}")]
    public async Task<IActionResult> GetByAccount(string accountId)
    {
        var userId = GetUserId();
        _logger.LogDebug("Getting invoices for account {AccountId}, user {UserId}", accountId, userId);

        try
        {
            var invoices = await _invoiceService.GetInvoicesByAccountAsync(userId, accountId);
            return Ok(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices for account {AccountId}", accountId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Busca faturas pendentes do usuário
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var userId = GetUserId();
        _logger.LogDebug("Getting pending invoices for user {UserId}", userId);

        try
        {
            var invoices = await _invoiceService.GetPendingInvoicesAsync(userId);
            return Ok(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending invoices");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Busca faturas vencidas do usuário
    /// </summary>
    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue()
    {
        var userId = GetUserId();
        _logger.LogDebug("Getting overdue invoices for user {UserId}", userId);

        try
        {
            var invoices = await _invoiceService.GetOverdueInvoicesAsync(userId);
            return Ok(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue invoices");
            return BadRequest(ex.Message);
        }
    }

    // ==================== FECHAMENTO ====================

    /// <summary>
    /// Fecha uma fatura manualmente
    /// </summary>
    [HttpPost("{invoiceId}/close")]
    public async Task<IActionResult> CloseInvoice(string invoiceId)
    {
        var userId = GetUserId();
        _logger.LogInformation("Closing invoice {InvoiceId} for user {UserId}", invoiceId, userId);

        try
        {
            var invoice = await _invoiceService.CloseInvoiceAsync(userId, invoiceId);
            return Ok(invoice);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for user {UserId}", invoiceId, userId);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot close invoice {InvoiceId}: {Message}", invoiceId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing invoice {InvoiceId}", invoiceId);
            return StatusCode(500, ex.Message);
        }
    }

    // ==================== PAGAMENTO ====================

    /// <summary>
    /// Paga uma fatura totalmente
    /// </summary>
    [HttpPost("pay")]
    public async Task<IActionResult> PayInvoice([FromBody] PayInvoiceRequestDto request)
    {
        var userId = GetUserId();
        _logger.LogInformation("Processing full payment for invoice {InvoiceId}, user {UserId}", request.InvoiceId, userId);

        try
        {
            await _invoiceService.PayInvoiceAsync(userId, request);
            return Ok(new { Message = "Invoice paid successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for user {UserId}", request.InvoiceId, userId);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot pay invoice {InvoiceId}: {Message}", request.InvoiceId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error paying invoice {InvoiceId}", request.InvoiceId);
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Paga uma fatura parcialmente
    /// </summary>
    [HttpPost("pay-partial")]
    public async Task<IActionResult> PayPartialInvoice([FromBody] PayInvoiceRequestDto request)
    {
        var userId = GetUserId();
        _logger.LogInformation("Processing partial payment for invoice {InvoiceId}, user {UserId}, amount {Amount}", 
            request.InvoiceId, userId, request.Amount);

        try
        {
            await _invoiceService.PayPartialInvoiceAsync(userId, request);
            return Ok(new { Message = "Partial payment processed successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for user {UserId}", request.InvoiceId, userId);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot pay invoice {InvoiceId}: {Message}", request.InvoiceId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing partial payment for invoice {InvoiceId}", request.InvoiceId);
            return StatusCode(500, ex.Message);
        }
    }

    // ==================== RELATÓRIOS ====================

    /// <summary>
    /// Busca resumo de uma fatura com transações
    /// </summary>
    [HttpGet("{invoiceId}/summary")]
    public async Task<IActionResult> GetSummary(string invoiceId)
    {
        var userId = GetUserId();
        _logger.LogDebug("Getting summary for invoice {InvoiceId}, user {UserId}", invoiceId, userId);

        try
        {
            var summary = await _invoiceService.GetInvoiceSummaryAsync(userId, invoiceId);
            return Ok(summary);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for user {UserId}", invoiceId, userId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting summary for invoice {InvoiceId}", invoiceId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Busca transações de uma fatura
    /// </summary>
    [HttpGet("{invoiceId}/transactions")]
    public async Task<IActionResult> GetTransactions(string invoiceId)
    {
        var userId = GetUserId();
        _logger.LogDebug("Getting transactions for invoice {InvoiceId}, user {UserId}", invoiceId, userId);

        try
        {
            var transactions = await _invoiceService.GetInvoiceTransactionsAsync(userId, invoiceId);
            return Ok(transactions);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for user {UserId}", invoiceId, userId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions for invoice {InvoiceId}", invoiceId);
            return BadRequest(ex.Message);
        }
    }

    // ==================== UTILITÁRIOS ====================

    /// <summary>
    /// Determina a fatura para uma transação baseado na data
    /// </summary>
    [HttpGet("accounts/{accountId}/determine")]
    public async Task<IActionResult> DetermineInvoiceForTransaction(string accountId, [FromQuery] DateTime transactionDate)
    {
        var userId = GetUserId();
        _logger.LogDebug("Determining invoice for account {AccountId}, date {Date}", accountId, transactionDate);

        try
        {
            var invoice = await _invoiceService.DetermineInvoiceForTransactionAsync(userId, accountId, transactionDate);
            return Ok(invoice);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Account {AccountId} not found for user {UserId}", accountId, userId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining invoice for transaction");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Recalcula o total de uma fatura
    /// </summary>
    [HttpPost("{invoiceId}/recalculate")]
    public async Task<IActionResult> RecalculateTotal(string invoiceId)
    {
        var userId = GetUserId();
        _logger.LogInformation("Recalculating total for invoice {InvoiceId}, user {UserId}", invoiceId, userId);

        try
        {
            await _invoiceService.RecalculateInvoiceTotalAsync(userId, invoiceId);
            return Ok(new { Message = "Invoice total recalculated successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for user {UserId}", invoiceId, userId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating total for invoice {InvoiceId}", invoiceId);
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Cria fatura histórica para migração de dados antigos
    /// </summary>
    [HttpPost("accounts/{accountId}/history")]
    public async Task<IActionResult> CreateHistoryInvoice(string accountId)
    {
        var userId = GetUserId();
        _logger.LogInformation("Creating history invoice for account {AccountId}, user {UserId}", accountId, userId);

        try
        {
            var invoice = await _invoiceService.CreateHistoryInvoiceAsync(userId, accountId);
            return Ok(invoice);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Account {AccountId} not found for user {UserId}", accountId, userId);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot create history invoice for account {AccountId}: {Message}", accountId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating history invoice for account {AccountId}", accountId);
            return StatusCode(500, ex.Message);
        }
    }
}
