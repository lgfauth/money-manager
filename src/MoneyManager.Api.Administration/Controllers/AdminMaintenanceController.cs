using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Administration.Models;
using MoneyManager.Api.Administration.Services;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Api.Administration.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AdminPolicies.Operator)]
public sealed class AdminMaintenanceController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICreditCardInvoiceService _invoiceService;
    private readonly AdminAuditService _auditService;
    private readonly ILogger<AdminMaintenanceController> _logger;

    public AdminMaintenanceController(
        IUnitOfWork unitOfWork,
        ICreditCardInvoiceService invoiceService,
        AdminAuditService auditService,
        ILogger<AdminMaintenanceController> logger)
    {
        _unitOfWork = unitOfWork;
        _invoiceService = invoiceService;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost("reconcile-credit-cards")]
    public async Task<IActionResult> ReconcileCreditCards([FromBody] AdminTargetUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetUserId))
        {
            return BadRequest(new { message = "targetUserId is required" });
        }

        var operatorUsername = User.Identity?.Name ?? "unknown";

        try
        {
            var summary = await _invoiceService.ReconcileCreditCardDataAsync(request.TargetUserId);
            await _auditService.RecordAsync(
                "reconcile-credit-cards",
                operatorUsername,
                request.TargetUserId,
                request,
                true,
                summary);

            return Ok(new
            {
                success = true,
                message = "Reconciliacao de cartoes concluida com sucesso",
                result = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconciling credit cards for user {UserId}", request.TargetUserId);
            await _auditService.RecordAsync(
                "reconcile-credit-cards",
                operatorUsername,
                request.TargetUserId,
                request,
                false,
                null,
                ex.Message);

            return StatusCode(500, new { message = "Erro ao reconciliar cartoes de credito", errors = new[] { ex.Message } });
        }
    }

    [HttpPost("recalculate-invoices")]
    public async Task<IActionResult> RecalculateInvoices([FromBody] AdminTargetUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetUserId))
        {
            return BadRequest(new { message = "targetUserId is required" });
        }

        var operatorUsername = User.Identity?.Name ?? "unknown";

        try
        {
            var allAccounts = await _unitOfWork.Accounts.GetAllAsync();
            var creditCards = allAccounts
                .Where(a => a.UserId == request.TargetUserId && a.Type == AccountType.CreditCard && !a.IsDeleted)
                .ToList();

            var recalculatedCount = 0;

            foreach (var card in creditCards)
            {
                var invoices = await _invoiceService.GetInvoicesByAccountAsync(request.TargetUserId, card.Id);

                foreach (var invoice in invoices)
                {
                    if (invoice.Status != InvoiceStatus.Paid)
                    {
                        await _invoiceService.RecalculateInvoiceTotalAsync(request.TargetUserId, invoice.Id);
                        recalculatedCount++;
                    }
                }
            }

            var result = new { recalculatedCount, totalCards = creditCards.Count };

            await _auditService.RecordAsync(
                "recalculate-invoices",
                operatorUsername,
                request.TargetUserId,
                request,
                true,
                result);

            return Ok(new
            {
                success = true,
                message = $"{recalculatedCount} faturas recalculadas com sucesso",
                result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating invoices for user {UserId}", request.TargetUserId);
            await _auditService.RecordAsync(
                "recalculate-invoices",
                operatorUsername,
                request.TargetUserId,
                request,
                false,
                null,
                ex.Message);

            return StatusCode(500, new { message = "Erro ao recalcular faturas", errors = new[] { ex.Message } });
        }
    }

    [HttpPost("create-missing-open-invoices")]
    public async Task<IActionResult> CreateMissingOpenInvoices([FromBody] AdminTargetUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetUserId))
        {
            return BadRequest(new { message = "targetUserId is required" });
        }

        var operatorUsername = User.Identity?.Name ?? "unknown";

        try
        {
            var allAccounts = await _unitOfWork.Accounts.GetAllAsync();
            var creditCards = allAccounts
                .Where(a => a.UserId == request.TargetUserId && a.Type == AccountType.CreditCard && !a.IsDeleted)
                .ToList();

            var createdCount = 0;

            foreach (var card in creditCards)
            {
                try
                {
                    var openInvoice = await _invoiceService.GetOrCreateOpenInvoiceAsync(request.TargetUserId, card.Id);

                    if (openInvoice.CreatedAt >= DateTime.UtcNow.AddMinutes(-1))
                    {
                        createdCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating open invoice for card {CardId}", card.Id);
                }
            }

            var result = new { createdCount, totalCards = creditCards.Count };

            await _auditService.RecordAsync(
                "create-missing-open-invoices",
                operatorUsername,
                request.TargetUserId,
                request,
                true,
                result);

            return Ok(new
            {
                success = true,
                message = $"{createdCount} faturas abertas criadas",
                result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating missing open invoices for user {UserId}", request.TargetUserId);
            await _auditService.RecordAsync(
                "create-missing-open-invoices",
                operatorUsername,
                request.TargetUserId,
                request,
                false,
                null,
                ex.Message);

            return StatusCode(500, new { message = "Erro ao criar faturas abertas", errors = new[] { ex.Message } });
        }
    }

    [HttpPost("migrate-credit-card-invoices")]
    [Authorize(Policy = AdminPolicies.Admin)]
    public async Task<IActionResult> MigrateCreditCardInvoices([FromBody] AdminTargetUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetUserId))
        {
            return BadRequest(new { message = "targetUserId is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 10)
        {
            return BadRequest(new { message = "reason is required for critical actions and must have at least 10 characters" });
        }

        var operatorUsername = User.Identity?.Name ?? "unknown";

        try
        {
            var migrationErrors = new List<string>();
            var cardsProcessed = 0;
            var invoicesCreated = 0;
            var transactionsLinked = 0;

            var allAccounts = await _unitOfWork.Accounts.GetAllAsync();
            var creditCards = allAccounts
                .Where(a => a.UserId == request.TargetUserId && a.Type == AccountType.CreditCard && !a.IsDeleted)
                .ToList();

            foreach (var card in creditCards)
            {
                try
                {
                    var existingHistoryInvoice = await _unitOfWork.CreditCardInvoices.GetByReferenceMonthAsync(card.Id, "HISTORY");
                    if (existingHistoryInvoice != null)
                    {
                        migrationErrors.Add($"Cartao '{card.Name}': Fatura historica ja existe");
                        continue;
                    }

                    var allTransactions = await _unitOfWork.Transactions.GetAllAsync();
                    var unlinkedTransactions = allTransactions
                        .Where(t => t.AccountId == card.Id
                            && t.Type == TransactionType.Expense
                            && string.IsNullOrEmpty(t.InvoiceId)
                            && !t.IsDeleted)
                        .ToList();

                    if (!unlinkedTransactions.Any())
                    {
                        continue;
                    }

                    await _invoiceService.CreateHistoryInvoiceAsync(request.TargetUserId, card.Id);

                    cardsProcessed++;
                    invoicesCreated++;
                    transactionsLinked += unlinkedTransactions.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing card {CardId}", card.Id);
                    migrationErrors.Add($"Cartao '{card.Name}': {ex.Message}");
                }
            }

            var result = new
            {
                cardsProcessed,
                invoicesCreated,
                transactionsLinked,
                errors = migrationErrors
            };

            await _auditService.RecordAsync(
                "migrate-credit-card-invoices",
                operatorUsername,
                request.TargetUserId,
                request,
                true,
                result);

            return Ok(new
            {
                success = true,
                message = "Migracao concluida",
                result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating invoices for user {UserId}", request.TargetUserId);
            await _auditService.RecordAsync(
                "migrate-credit-card-invoices",
                operatorUsername,
                request.TargetUserId,
                request,
                false,
                null,
                ex.Message);

            return StatusCode(500, new { message = "Erro durante a migracao", errors = new[] { ex.Message } });
        }
    }

    [HttpGet("audit/actions")]
    [Authorize(Policy = AdminPolicies.Viewer)]
    public async Task<ActionResult<IReadOnlyList<AdminAuditActionItemDto>>> GetAuditActions(
        [FromQuery] int limit = 50,
        [FromQuery] string? targetUserId = null,
        [FromQuery] string? action = null)
    {
        var items = await _auditService.GetRecentAsync(limit, targetUserId, action);
        return Ok(items);
    }

    [HttpGet("audit/report/monthly")]
    [Authorize(Policy = AdminPolicies.Viewer)]
    public async Task<ActionResult<AdminMonthlyAuditReportDto>> GetMonthlyAuditReport(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        try
        {
            var report = await _auditService.GetMonthlyReportAsync(year, month);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
