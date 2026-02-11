using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Presentation.Controllers;

/// <summary>
/// Controller para operações administrativas e migração de dados
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICreditCardInvoiceService _invoiceService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUnitOfWork unitOfWork,
        ICreditCardInvoiceService invoiceService,
        ILogger<AdminController> logger)
    {
        _unitOfWork = unitOfWork;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Migra transações antigas de cartões de crédito para faturas históricas
    /// </summary>
    [HttpPost("migrate-credit-card-invoices")]
    public async Task<IActionResult> MigrateCreditCardInvoices()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("Starting credit card invoice migration for user {UserId}", userId);

        try
        {
            var migrationResult = new
            {
                CardsProcessed = 0,
                InvoicesCreated = 0,
                TransactionsLinked = 0,
                Errors = new List<string>()
            };

            // Buscar todos os cartões de crédito do usuário
            var allAccounts = await _unitOfWork.Accounts.GetAllAsync();
            var creditCards = allAccounts
                .Where(a => a.UserId == userId && a.Type == AccountType.CreditCard && !a.IsDeleted)
                .ToList();

            _logger.LogInformation("Found {Count} credit cards to process", creditCards.Count);

            foreach (var card in creditCards)
            {
                try
                {
                    _logger.LogInformation("Processing card {CardId} - {CardName}", card.Id, card.Name);

                    // Verificar se já existe fatura histórica
                    var existingHistoryInvoice = await _unitOfWork.CreditCardInvoices
                        .GetByReferenceMonthAsync(card.Id, "HISTORY");

                    if (existingHistoryInvoice != null)
                    {
                        _logger.LogInformation("History invoice already exists for card {CardId}, skipping", card.Id);
                        ((List<string>)migrationResult.Errors).Add($"Cartão '{card.Name}': Fatura histórica já existe");
                        continue;
                    }

                    // Buscar transações antigas sem fatura vinculada
                    var allTransactions = await _unitOfWork.Transactions.GetAllAsync();
                    var unlinkedTransactions = allTransactions
                        .Where(t => t.AccountId == card.Id 
                                 && t.Type == TransactionType.Expense 
                                 && string.IsNullOrEmpty(t.InvoiceId)
                                 && !t.IsDeleted)
                        .ToList();

                    if (!unlinkedTransactions.Any())
                    {
                        _logger.LogInformation("No unlinked transactions for card {CardId}", card.Id);
                        continue;
                    }

                    _logger.LogInformation("Found {Count} unlinked transactions for card {CardId}", 
                        unlinkedTransactions.Count, card.Id);

                    // Criar fatura histórica
                    var historyInvoice = await _invoiceService.CreateHistoryInvoiceAsync(userId, card.Id);

                    migrationResult = new
                    {
                        CardsProcessed = migrationResult.CardsProcessed + 1,
                        InvoicesCreated = migrationResult.InvoicesCreated + 1,
                        TransactionsLinked = migrationResult.TransactionsLinked + unlinkedTransactions.Count,
                        Errors = migrationResult.Errors
                    };

                    _logger.LogInformation("Created history invoice {InvoiceId} for card {CardId} with {Count} transactions",
                        historyInvoice.Id, card.Id, unlinkedTransactions.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing card {CardId}", card.Id);
                    ((List<string>)migrationResult.Errors).Add($"Cartão '{card.Name}': {ex.Message}");
                }
            }

            _logger.LogInformation("Migration completed: {Result}", migrationResult);

            return Ok(new
            {
                Success = true,
                Message = "Migração concluída com sucesso",
                Result = migrationResult
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during migration");
            return StatusCode(500, new
            {
                Success = false,
                Message = "Erro durante a migração",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Recalcula totais de todas as faturas de um usuário
    /// </summary>
    [HttpPost("recalculate-invoices")]
    public async Task<IActionResult> RecalculateInvoices()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("Recalculating invoices for user {UserId}", userId);

        try
        {
            var allAccounts = await _unitOfWork.Accounts.GetAllAsync();
            var creditCards = allAccounts
                .Where(a => a.UserId == userId && a.Type == AccountType.CreditCard && !a.IsDeleted)
                .ToList();

            var recalculatedCount = 0;

            foreach (var card in creditCards)
            {
                var invoices = await _invoiceService.GetInvoicesByAccountAsync(userId, card.Id);

                foreach (var invoice in invoices)
                {
                    if (invoice.Status != InvoiceStatus.Paid)
                    {
                        await _invoiceService.RecalculateInvoiceTotalAsync(userId, invoice.Id);
                        recalculatedCount++;
                    }
                }
            }

            _logger.LogInformation("Recalculated {Count} invoices", recalculatedCount);

            return Ok(new
            {
                Success = true,
                Message = $"{recalculatedCount} faturas recalculadas com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating invoices");
            return StatusCode(500, new
            {
                Success = false,
                Message = "Erro ao recalcular faturas",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Cria fatura aberta para cartões que não têm
    /// </summary>
    [HttpPost("create-missing-open-invoices")]
    public async Task<IActionResult> CreateMissingOpenInvoices()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("Creating missing open invoices for user {UserId}", userId);

        try
        {
            var allAccounts = await _unitOfWork.Accounts.GetAllAsync();
            var creditCards = allAccounts
                .Where(a => a.UserId == userId && a.Type == AccountType.CreditCard && !a.IsDeleted)
                .ToList();

            var createdCount = 0;

            foreach (var card in creditCards)
            {
                try
                {
                    var openInvoice = await _invoiceService.GetOrCreateOpenInvoiceAsync(userId, card.Id);
                    
                    if (openInvoice.CreatedAt >= DateTime.UtcNow.AddMinutes(-1))
                    {
                        createdCount++;
                        _logger.LogInformation("Created open invoice for card {CardId}", card.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating open invoice for card {CardId}", card.Id);
                }
            }

            return Ok(new
            {
                Success = true,
                Message = $"{createdCount} faturas abertas criadas",
                TotalCards = creditCards.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating open invoices");
            return StatusCode(500, new
            {
                Success = false,
                Message = "Erro ao criar faturas abertas",
                Error = ex.Message
            });
        }
    }
}
