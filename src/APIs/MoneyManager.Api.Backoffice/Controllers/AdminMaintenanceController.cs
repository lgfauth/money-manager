using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Administration.Models;
using MoneyManager.Api.Administration.Services;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.WorkerControl;

namespace MoneyManager.Api.Administration.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public sealed class AdminMaintenanceController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOpenBankingCategoryMigrationService _categoryMigrationService;
    private readonly WorkerCommandQueueService _commandQueueService;
    private readonly AdminAuditService _auditService;
    private readonly ILogger<AdminMaintenanceController> _logger;

    public AdminMaintenanceController(
        IUnitOfWork unitOfWork,
        IOpenBankingCategoryMigrationService categoryMigrationService,
        WorkerCommandQueueService commandQueueService,
        AdminAuditService auditService,
        ILogger<AdminMaintenanceController> logger)
    {
        _unitOfWork = unitOfWork;
        _categoryMigrationService = categoryMigrationService;
        _commandQueueService = commandQueueService;
        _auditService = auditService;
        _logger = logger;
    }

    // Migração retroativa: migra para o padrão Open Banking as categorias de todos os
    // usuários com pelo menos uma BankConnection com SelectedAccounts não-vazio e que
    // ainda não foram migrados. Idempotente — pode ser executado mais de uma vez.
    [HttpPost("maintenance/openbanking-categories/migrate")]
    [Authorize(Policy = AdminPolicies.Operator)]
    public async Task<IActionResult> MigrateOpenBankingCategories([FromBody] RunNowJobRequest? request)
    {
        if (string.IsNullOrWhiteSpace(request?.Reason) || request.Reason.Trim().Length < 10)
        {
            return BadRequest(new { message = "reason is required and must have at least 10 characters" });
        }

        var operatorUsername = User.Identity?.Name ?? "unknown";

        try
        {
            var connections = await _unitOfWork.BankConnections.GetAllAsync();
            var candidateUserIds = connections
                .Where(c => !c.IsDeleted && c.SelectedAccounts.Count > 0)
                .Select(c => c.UserId)
                .Distinct()
                .ToList();

            var migratedCount = 0;
            var skippedCount = 0;
            var errors = new List<string>();

            foreach (var userId in candidateUserIds)
            {
                try
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(userId);
                    if (user is null || user.HasMigratedOpenBankingCategories)
                    {
                        skippedCount++;
                        continue;
                    }

                    var migrated = await _categoryMigrationService.MigrateUserToOpenBankingCategoriesAsync(userId);
                    if (migrated) migratedCount++;
                    else skippedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Erro na migração retroativa de categorias Open Banking do usuário {UserId}", userId);
                    errors.Add($"{userId}: {ex.Message}");
                }
            }

            var result = new
            {
                totalCandidates = candidateUserIds.Count,
                migratedCount,
                skippedCount,
                errors
            };

            _logger.LogInformation(
                "Migração retroativa de categorias Open Banking finalizada: {MigratedCount} usuário(s) migrado(s), {SkippedCount} pulado(s), {ErrorCount} erro(s)",
                migratedCount, skippedCount, errors.Count);

            await _auditService.RecordAsync(
                action: "maintenance/openbanking-categories/migrate",
                operatorUsername: operatorUsername,
                targetUserId: "all",
                parameters: request,
                isSuccess: errors.Count == 0,
                result: result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar migração retroativa de categorias Open Banking");

            await _auditService.RecordAsync(
                action: "maintenance/openbanking-categories/migrate",
                operatorUsername: operatorUsername,
                targetUserId: "all",
                parameters: request,
                isSuccess: false,
                result: null,
                errorMessage: ex.Message);

            return StatusCode(500, new { message = "Erro ao executar migração retroativa de categorias", errors = new[] { ex.Message } });
        }
    }

    // Recategoriza transações já sincronizadas sem categoria (com categoryId de origem Pluggy
    // persistido), usando as categorias Open Banking do usuário. Roda apenas para usuários já
    // migrados — os demais são pulados. Com TargetUserId vazio, processa todos os elegíveis.
    [HttpPost("maintenance/openbanking-categories/recategorize")]
    [Authorize(Policy = AdminPolicies.Operator)]
    public async Task<IActionResult> RecategorizeOpenBankingTransactions(
        [FromBody] AdminOpenBankingRecategorizeRequest? request)
    {
        if (string.IsNullOrWhiteSpace(request?.Reason) || request.Reason.Trim().Length < 10)
        {
            return BadRequest(new { message = "reason is required and must have at least 10 characters" });
        }

        var operatorUsername = User.Identity?.Name ?? "unknown";
        var targetUserId = request.TargetUserId?.Trim();
        var auditTarget = string.IsNullOrEmpty(targetUserId) ? "all" : targetUserId;

        try
        {
            List<string> candidateUserIds;
            if (!string.IsNullOrEmpty(targetUserId))
            {
                candidateUserIds = [targetUserId];
            }
            else
            {
                var connections = await _unitOfWork.BankConnections.GetAllAsync();
                candidateUserIds = connections
                    .Where(c => !c.IsDeleted && c.SelectedAccounts.Count > 0)
                    .Select(c => c.UserId)
                    .Distinct()
                    .ToList();
            }

            var usersProcessed = 0;
            var skippedCount = 0;
            var recategorizedTransactions = 0;
            var errors = new List<string>();

            foreach (var userId in candidateUserIds)
            {
                try
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(userId);
                    if (user is null || !user.HasMigratedOpenBankingCategories)
                    {
                        // Sem migração, o usuário não tem as categorias Open Banking — recategorizar
                        // jogaria tudo em "Outros". Pular e informar.
                        skippedCount++;
                        continue;
                    }

                    recategorizedTransactions += await _categoryMigrationService
                        .RecategorizeExistingTransactionsAsync(userId);
                    usersProcessed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Erro na recategorização de transações Open Banking do usuário {UserId}", userId);
                    errors.Add($"{userId}: {ex.Message}");
                }
            }

            var result = new
            {
                totalCandidates = candidateUserIds.Count,
                usersProcessed,
                skippedCount,
                recategorizedTransactions,
                errors
            };

            _logger.LogInformation(
                "Recategorização de transações Open Banking finalizada: {UsersProcessed} usuário(s) processado(s), {RecategorizedTransactions} transação(ões) recategorizada(s), {SkippedCount} usuário(s) pulado(s), {ErrorCount} erro(s)",
                usersProcessed, recategorizedTransactions, skippedCount, errors.Count);

            await _auditService.RecordAsync(
                action: "maintenance/openbanking-categories/recategorize",
                operatorUsername: operatorUsername,
                targetUserId: auditTarget,
                parameters: request,
                isSuccess: errors.Count == 0,
                result: result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar recategorização de transações Open Banking");

            await _auditService.RecordAsync(
                action: "maintenance/openbanking-categories/recategorize",
                operatorUsername: operatorUsername,
                targetUserId: auditTarget,
                parameters: request,
                isSuccess: false,
                result: null,
                errorMessage: ex.Message);

            return StatusCode(500, new { message = "Erro ao executar recategorização de transações", errors = new[] { ex.Message } });
        }
    }

    // Re-sync completo: reseta a janela de sync (LastSyncAt) das contas vinculadas para que o
    // próximo sync re-busque as transações desde o CutoffDate da conexão — permitindo o backfill
    // de categoria (OpenBankingCategoryId) em transações importadas antes da feature. Ao final,
    // enfileira um run-now do BankSyncWorker para execução imediata.
    [HttpPost("maintenance/bank-connections/full-resync")]
    [Authorize(Policy = AdminPolicies.Operator)]
    public async Task<IActionResult> FullResyncBankConnections([FromBody] AdminBankFullResyncRequest? request)
    {
        if (string.IsNullOrWhiteSpace(request?.Reason) || request.Reason.Trim().Length < 10)
        {
            return BadRequest(new { message = "reason is required and must have at least 10 characters" });
        }

        var operatorUsername = User.Identity?.Name ?? "unknown";
        var targetUserId = request.TargetUserId?.Trim();
        var auditTarget = string.IsNullOrEmpty(targetUserId) ? "all" : targetUserId;

        try
        {
            var connections = (await _unitOfWork.BankConnections.GetAllAsync())
                .Where(c => !c.IsDeleted && c.SelectedAccounts.Count > 0)
                .Where(c => string.IsNullOrEmpty(targetUserId) || c.UserId == targetUserId)
                .ToList();

            var connectionsReset = 0;
            var accountsReset = 0;

            foreach (var connection in connections)
            {
                var resetInConnection = 0;
                foreach (var selected in connection.SelectedAccounts.Where(s => s.LastSyncAt is not null))
                {
                    selected.LastSyncAt = null;
                    resetInConnection++;
                }

                if (resetInConnection == 0)
                    continue;

                connection.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.BankConnections.UpdateAsync(connection);
                connectionsReset++;
                accountsReset += resetInConnection;
            }

            await _unitOfWork.SaveChangesAsync();

            var queued = await _commandQueueService.EnqueueRunNowAsync("BankSyncWorker", operatorUsername, request.Reason);

            var result = new
            {
                totalConnections = connections.Count,
                connectionsReset,
                accountsReset,
                syncCommandQueued = true,
                commandId = queued.CommandId,
                alreadyQueued = queued.AlreadyQueued
            };

            _logger.LogInformation(
                "Re-sync completo Open Banking: janela de sync resetada em {ConnectionsReset} conexão(ões) / {AccountsReset} conta(s); BankSyncWorker enfileirado (commandId {CommandId})",
                connectionsReset, accountsReset, queued.CommandId);

            await _auditService.RecordAsync(
                action: "maintenance/bank-connections/full-resync",
                operatorUsername: operatorUsername,
                targetUserId: auditTarget,
                parameters: request,
                isSuccess: true,
                result: result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar re-sync completo das conexões bancárias");

            await _auditService.RecordAsync(
                action: "maintenance/bank-connections/full-resync",
                operatorUsername: operatorUsername,
                targetUserId: auditTarget,
                parameters: request,
                isSuccess: false,
                result: null,
                errorMessage: ex.Message);

            return StatusCode(500, new { message = "Erro ao executar re-sync completo", errors = new[] { ex.Message } });
        }
    }
}
