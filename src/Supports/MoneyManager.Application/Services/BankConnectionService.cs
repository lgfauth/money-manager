using Microsoft.Extensions.Logging;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

public interface IBankConnectionService
{
    // Salva (criptografada) e valida a API key do Banco MCP do usuário.
    Task<SaveApiKeyResultDto> SaveBankMcpApiKeyAsync(string userId, string apiKey, CancellationToken ct);

    // Lista as conexões disponíveis na conta Banco MCP do usuário (para ele escolher quais sincronizar).
    Task<IReadOnlyList<BankMcpConnectionDto>> GetAvailableConnectionsAsync(string userId, CancellationToken ct);

    // Registra uma conexão escolhida pelo próprio usuário.
    Task<BankConnectionResponseDto> RegisterConnectionAsync(string userId, string externalConnectionId, CancellationToken ct);

    // Busca accounts disponíveis de uma conexão Connected (para tela de seleção do onboarding).
    Task<BankMcpAvailableAccountsResponseDto> GetAvailableAccountsAsync(string userId, string connectionId, CancellationToken ct);

    // Salva seleção de contas + estratégia de dados + dispara primeiro sync.
    Task<BankConnectionResponseDto> CompleteOnboardingAsync(string userId, string connectionId, CompleteOnboardingRequestDto request, CancellationToken ct);

    // Lista conexões ativas do usuário.
    Task<IReadOnlyList<BankConnectionResponseDto>> GetUserConnectionsAsync(string userId, CancellationToken ct);

    // Desconecta um banco (soft delete na BankConnection + limpa ExternalAccountId das Accounts mapeadas).
    Task DisconnectAsync(string userId, string connectionId, CancellationToken ct);

    // Sync manual disparado pelo usuário (botão "atualizar agora").
    Task SyncNowAsync(string userId, string connectionId, CancellationToken ct);

    // Sync periódico — chamado pelo worker. Processa todas as conexões ativas.
    Task SyncAllActiveConnectionsAsync(CancellationToken ct);
}

public class BankConnectionService : IBankConnectionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBankMcpClient _bankMcpClient;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IEncryptionService _encryptionService;
    private readonly IProcessLogger _processLogger;
    private readonly ILogger<BankConnectionService> _logger;

    public BankConnectionService(
        IUnitOfWork unitOfWork,
        IBankMcpClient bankMcpClient,
        ISubscriptionService subscriptionService,
        IEncryptionService encryptionService,
        IProcessLogger processLogger,
        ILogger<BankConnectionService> logger)
    {
        _unitOfWork = unitOfWork;
        _bankMcpClient = bankMcpClient;
        _subscriptionService = subscriptionService;
        _encryptionService = encryptionService;
        _processLogger = processLogger;
        _logger = logger;
    }

    public async Task<SaveApiKeyResultDto> SaveBankMcpApiKeyAsync(string userId, string apiKey, CancellationToken ct)
    {
        await _subscriptionService.EnsurePremiumAccessAsync(userId);

        IReadOnlyList<BankMcpConnection> connections;
        try
        {
            connections = await _bankMcpClient.ListConnectionsAsync(apiKey, ct);
        }
        catch
        {
            throw new InvalidOperationException("API key do Banco MCP inválida ou sem permissão");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Usuário não encontrado");

        user.BankMcpApiKey = _encryptionService.Encrypt(apiKey);
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("API key do Banco MCP salva para usuário {UserId}", userId);

        return new SaveApiKeyResultDto
        {
            IsValid = true,
            AvailableConnections = connections.Count
        };
    }

    public async Task<IReadOnlyList<BankMcpConnectionDto>> GetAvailableConnectionsAsync(string userId, CancellationToken ct)
    {
        var apiKey = await GetDecryptedApiKeyAsync(userId);
        var connections = await _bankMcpClient.ListConnectionsAsync(apiKey, ct);
        var existing = await _unitOfWork.BankConnections.GetByUserIdAsync(userId);
        var existingIds = existing.Select(c => c.ExternalConnectionId).ToHashSet();

        return connections.Select(c => new BankMcpConnectionDto
        {
            ExternalConnectionId = c.ExternalConnectionId,
            InstitutionName = c.InstitutionName,
            InstitutionLogo = c.InstitutionLogo,
            Status = c.Status,
            AlreadyRegistered = existingIds.Contains(c.ExternalConnectionId)
        }).ToList();
    }

    public async Task<BankConnectionResponseDto> RegisterConnectionAsync(string userId, string externalConnectionId, CancellationToken ct)
    {
        await _subscriptionService.EnsurePremiumAccessAsync(userId);

        var apiKey = await GetDecryptedApiKeyAsync(userId);

        var existing = await _unitOfWork.BankConnections.GetByExternalConnectionIdAsync(userId, externalConnectionId);
        if (existing is not null)
            throw new InvalidOperationException("Esta conexão bancária já está registrada");

        var status = await _bankMcpClient.GetConnectionStatusAsync(apiKey, externalConnectionId, ct);
        if (status.Status is "LOGIN_ERROR" or "WAITING_USER_INPUT")
            throw new InvalidOperationException($"Conexão com status inválido no Banco MCP: {status.Status}. Reconecte o banco lá antes de continuar.");

        var accounts = await _bankMcpClient.ListAccountsAsync(apiKey, externalConnectionId, ct);
        var institutionName = accounts.FirstOrDefault()?.InstitutionName ?? "Banco";
        var institutionLogo = accounts.FirstOrDefault()?.InstitutionLogo;

        var connection = new BankConnection
        {
            UserId = userId,
            ExternalConnectionId = externalConnectionId,
            InstitutionName = institutionName,
            InstitutionLogo = institutionLogo,
            Status = BankConnectionStatus.Connected,
            ConnectedAt = DateTime.UtcNow
        };

        await _unitOfWork.BankConnections.AddAsync(connection);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Conexão {ExternalConnectionId} registrada pelo próprio usuário {UserId}", externalConnectionId, userId);

        return MapToDto(connection);
    }

    public async Task<BankMcpAvailableAccountsResponseDto> GetAvailableAccountsAsync(string userId, string connectionId, CancellationToken ct)
    {
        var connection = await _unitOfWork.BankConnections.GetByUserIdAndIdAsync(userId, connectionId)
            ?? throw new KeyNotFoundException("Conexão não encontrada");

        if (connection.Status != BankConnectionStatus.Connected)
            throw new InvalidOperationException("Conexão ainda não está ativa");

        var apiKey = await GetDecryptedApiKeyAsync(userId);
        var accounts = await _bankMcpClient.ListAccountsAsync(apiKey, connection.ExternalConnectionId, ct);

        return new BankMcpAvailableAccountsResponseDto
        {
            ConnectionId = connectionId,
            Accounts = accounts.Select(a => new BankMcpAccountDto
            {
                ExternalAccountId = a.ExternalAccountId,
                Name = a.Name,
                Type = a.Type,
                Balance = a.Balance
            }).ToList()
        };
    }

    public async Task<BankConnectionResponseDto> CompleteOnboardingAsync(
        string userId, string connectionId, CompleteOnboardingRequestDto request, CancellationToken ct)
    {
        _processLogger.AddStep("Iniciando onboarding de conexão bancária", new Dictionary<string, object?>
        {
            ["userId"] = userId,
            ["connectionId"] = connectionId,
            ["strategy"] = request.Strategy.ToString()
        });

        var connection = await _unitOfWork.BankConnections.GetByUserIdAndIdAsync(userId, connectionId)
            ?? throw new KeyNotFoundException("Conexão não encontrada");

        if (connection.Status != BankConnectionStatus.Connected)
            throw new InvalidOperationException("Conexão ainda não está ativa");

        connection.SelectedAccounts = request.AccountMappings.Select(m => new SelectedBankAccount
        {
            ExternalAccountId = m.ExternalAccountId,
            Name = m.ExternalAccountName,
            Type = m.ExternalAccountType,
            MoneyManagerAccountId = m.MoneyManagerAccountId
        }).ToList();

        connection.OnboardingStrategy = request.Strategy;

        foreach (var mapping in request.AccountMappings)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(mapping.MoneyManagerAccountId);
            if (account is null || account.UserId != userId) continue;

            account.ExternalAccountId = mapping.ExternalAccountId;
            await _unitOfWork.Accounts.UpdateAsync(account);
        }

        if (request.Strategy == OnboardingStrategy.CleanSlate)
        {
            await ApplyCleanSlateAsync(userId, ct);
            connection.CutoffDate = DateTime.UtcNow.AddMonths(-12);
        }
        else
        {
            var cutoff = request.CustomCutoffDate ?? await CalculateCutoffDateAsync(userId, ct);
            connection.CutoffDate = cutoff;
        }

        await _unitOfWork.BankConnections.UpdateAsync(connection);
        await _unitOfWork.SaveChangesAsync();

        var apiKey = await GetDecryptedApiKeyAsync(userId);
        await SyncConnectionAsync(connection, apiKey, ct);

        _processLogger.AddStep("Onboarding concluído", new Dictionary<string, object?>
        {
            ["connectionId"] = connectionId,
            ["accountsMapped"] = request.AccountMappings.Count,
            ["cutoffDate"] = connection.CutoffDate
        });

        return MapToDto(connection);
    }

    public async Task<IReadOnlyList<BankConnectionResponseDto>> GetUserConnectionsAsync(string userId, CancellationToken ct)
    {
        var connections = await _unitOfWork.BankConnections.GetByUserIdAsync(userId);
        return connections.Select(MapToDto).ToList();
    }

    public async Task DisconnectAsync(string userId, string connectionId, CancellationToken ct)
    {
        var connection = await _unitOfWork.BankConnections.GetByUserIdAndIdAsync(userId, connectionId)
            ?? throw new KeyNotFoundException("Conexão não encontrada");

        foreach (var selected in connection.SelectedAccounts.Where(s => s.MoneyManagerAccountId is not null))
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(selected.MoneyManagerAccountId!);
            if (account is null || account.UserId != userId) continue;

            account.ExternalAccountId = null;
            await _unitOfWork.Accounts.UpdateAsync(account);
        }

        connection.Disconnect();
        await _unitOfWork.BankConnections.UpdateAsync(connection);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Conexão bancária {ConnectionId} desconectada para usuário {UserId}", connectionId, userId);
    }

    public async Task SyncNowAsync(string userId, string connectionId, CancellationToken ct)
    {
        var connection = await _unitOfWork.BankConnections.GetByUserIdAndIdAsync(userId, connectionId)
            ?? throw new KeyNotFoundException("Conexão não encontrada");

        if (connection.Status != BankConnectionStatus.Connected)
            throw new InvalidOperationException("Conexão não está ativa");

        var apiKey = await GetDecryptedApiKeyAsync(userId);
        await SyncConnectionAsync(connection, apiKey, ct);
    }

    public async Task SyncAllActiveConnectionsAsync(CancellationToken ct)
    {
        var connections = await _unitOfWork.BankConnections.GetAllConnectedAsync();

        _processLogger.AddStep("Iniciando sync periódico", new Dictionary<string, object?>
        {
            ["totalConexoes"] = connections.Count()
        });

        foreach (var connection in connections)
        {
            try
            {
                string apiKey;
                try
                {
                    apiKey = await GetDecryptedApiKeyAsync(connection.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Usuário {UserId} sem API key configurada, pulando sync da conexão {ConnectionId}",
                        connection.UserId, connection.Id);
                    continue;
                }

                await SyncConnectionAsync(connection, apiKey, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao sincronizar conexão {ConnectionId} do usuário {UserId}",
                    connection.Id, connection.UserId);
            }
        }

        _processLogger.AddStep("Sync periódico finalizado");
    }

    // ── Métodos privados ─────────────────────────────────────────────────────

    private async Task<string> GetDecryptedApiKeyAsync(string userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Usuário não encontrado");

        if (string.IsNullOrEmpty(user.BankMcpApiKey))
            throw new InvalidOperationException("API key do Banco MCP não configurada. Configure nas configurações da conta.");

        return _encryptionService.Decrypt(user.BankMcpApiKey);
    }

    private async Task SyncConnectionAsync(BankConnection connection, string apiKey, CancellationToken ct)
    {
        foreach (var selected in connection.SelectedAccounts.Where(s => s.MoneyManagerAccountId is not null))
        {
            try
            {
                var since = selected.LastSyncAt ?? connection.CutoffDate ?? DateTime.UtcNow.AddMonths(-12);

                var transactions = await _bankMcpClient.ListTransactionsAsync(
                    apiKey,
                    connection.ExternalConnectionId,
                    selected.ExternalAccountId,
                    since,
                    ct);

                foreach (var tx in transactions.Where(t => t.Status == "POSTED"))
                {
                    await UpsertTransactionAsync(connection.UserId, selected.MoneyManagerAccountId!, tx, ct);
                }

                selected.LastSyncAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Conta {ExternalAccountId} sincronizada: {Count} transações para usuário {UserId}",
                    selected.ExternalAccountId, transactions.Count, connection.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao sincronizar conta {ExternalAccountId}", selected.ExternalAccountId);
            }
        }

        connection.LastSyncAt = DateTime.UtcNow;
        connection.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.BankConnections.UpdateAsync(connection);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task UpsertTransactionAsync(string userId, string accountId, BankMcpTransaction tx, CancellationToken ct)
    {
        var existing = await _unitOfWork.Transactions.GetByExternalIdAsync(userId, tx.ExternalId);
        if (existing is not null) return;

        var transaction = new Transaction
        {
            UserId = userId,
            AccountId = accountId,
            Description = tx.Description,
            Amount = Math.Abs(tx.Amount),
            Type = tx.Amount < 0 ? TransactionType.Expense : TransactionType.Income,
            Date = tx.Date,
            Source = "bank_sync",
            ExternalId = tx.ExternalId,
            IsDeleted = false
        };

        await _unitOfWork.Transactions.AddAsync(transaction);
    }

    private async Task ApplyCleanSlateAsync(string userId, CancellationToken ct)
    {
        var manualTransactions = await _unitOfWork.Transactions.GetManualByUserIdAsync(userId);
        foreach (var tx in manualTransactions)
        {
            tx.IsDeleted = true;
            await _unitOfWork.Transactions.UpdateAsync(tx);
        }

        _logger.LogInformation("CleanSlate: {Count} transações manuais arquivadas para usuário {UserId}",
            manualTransactions.Count(), userId);
    }

    private async Task<DateTime> CalculateCutoffDateAsync(string userId, CancellationToken ct)
    {
        var lastManual = await _unitOfWork.Transactions.GetLastManualDateAsync(userId);
        var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);

        if (lastManual is null)
            return twelveMonthsAgo;

        var cutoff = lastManual.Value.AddDays(-15);
        return cutoff < twelveMonthsAgo ? twelveMonthsAgo : cutoff;
    }

    private static BankConnectionResponseDto MapToDto(BankConnection c) => new()
    {
        Id = c.Id,
        InstitutionName = c.InstitutionName,
        InstitutionLogo = c.InstitutionLogo,
        Status = c.Status.ToString(),
        ConnectedAt = c.ConnectedAt,
        LastSyncAt = c.LastSyncAt,
        SelectedAccounts = c.SelectedAccounts.Select(s => new SelectedBankAccountDto
        {
            ExternalAccountId = s.ExternalAccountId,
            Name = s.Name,
            Type = s.Type,
            MoneyManagerAccountId = s.MoneyManagerAccountId,
            LastSyncAt = s.LastSyncAt
        }).ToList()
    };
}
