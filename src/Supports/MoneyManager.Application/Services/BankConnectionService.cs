using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

public interface IBankConnectionService
{
    // Gera URL de convite para o usuário conectar seus bancos no Banco MCP.
    Task<BankMcpUserInviteResponseDto> GetUserInviteUrlAsync(string userId, CancellationToken ct);

    // Lista as conexões disponíveis no workspace do Banco MCP para o usuário registrar.
    Task<BankMcpAvailableConnectionsResponseDto> GetAvailableConnectionsAsync(string userId, CancellationToken ct);

    // Registra uma conexão do Banco MCP para o usuário (por item_id).
    Task<BankConnectionResponseDto> RegisterConnectionAsync(string userId, string externalConnectionId, CancellationToken ct);

    // Busca accounts disponíveis de uma conexão Connected (para tela de seleção do onboarding).
    Task<BankMcpAvailableAccountsResponseDto> GetConnectionAccountsAsync(string userId, string connectionId, CancellationToken ct);

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
    private readonly IBankMcpManagementClient _bankMcpManagementClient;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IProcessLogger _processLogger;
    private readonly ILogger<BankConnectionService> _logger;
    private readonly string _toolkitId;

    public BankConnectionService(
        IUnitOfWork unitOfWork,
        IBankMcpClient bankMcpClient,
        IBankMcpManagementClient bankMcpManagementClient,
        ISubscriptionService subscriptionService,
        IProcessLogger processLogger,
        ILogger<BankConnectionService> logger,
        IOptions<BancoMcpOptions> bancoMcpOptions)
    {
        _unitOfWork = unitOfWork;
        _bankMcpClient = bankMcpClient;
        _bankMcpManagementClient = bankMcpManagementClient;
        _subscriptionService = subscriptionService;
        _processLogger = processLogger;
        _logger = logger;
        _toolkitId = bancoMcpOptions.Value.ToolkitId;
    }

    public async Task<BankMcpUserInviteResponseDto> GetUserInviteUrlAsync(string userId, CancellationToken ct)
    {
        await _subscriptionService.EnsurePremiumAccessAsync(userId);

        // Label identifica o usuário MoneyManager no workspace do Banco MCP.
        var invite = await _bankMcpManagementClient.CreateUserInviteAsync(_toolkitId, $"mm_{userId}", ct);

        _logger.LogInformation("Convite Banco MCP gerado para usuário {UserId}", userId);

        return new BankMcpUserInviteResponseDto { ConnectUrl = invite.ConnectUrl };
    }

    public async Task<BankMcpAvailableConnectionsResponseDto> GetAvailableConnectionsAsync(string userId, CancellationToken ct)
    {
        await _subscriptionService.EnsurePremiumAccessAsync(userId);

        var result = await _bankMcpClient.ListConnectionsAsync(ct);

        // Marca quais já estão registradas para este usuário.
        var existing = await _unitOfWork.BankConnections.GetByUserIdAsync(userId);
        var existingIds = existing.Select(c => c.ExternalConnectionId).ToHashSet();

        return new BankMcpAvailableConnectionsResponseDto
        {
            AddConnectionUrl = result.AddConnectionUrl,
            Connections = result.Connections.Select(c => new BankMcpConnectionDto
            {
                ItemId = c.ItemId,
                ConnectorId = c.ConnectorId,
                ConnectorName = c.ConnectorName,
                Status = c.Status,
                AlreadyRegistered = existingIds.Contains(c.ItemId)
            }).ToList()
        };
    }

    public async Task<BankConnectionResponseDto> RegisterConnectionAsync(string userId, string externalConnectionId, CancellationToken ct)
    {
        await _subscriptionService.EnsurePremiumAccessAsync(userId);

        var existing = await _unitOfWork.BankConnections.GetByExternalConnectionIdAsync(userId, externalConnectionId);
        if (existing is not null)
            throw new InvalidOperationException("Esta conexão bancária já está registrada");

        // Valida status no Banco MCP antes de registrar.
        var status = await _bankMcpClient.GetConnectionStatusAsync(externalConnectionId, ct);
        if (status.Status is "LOGIN_ERROR" or "WAITING_USER_INPUT")
            throw new InvalidOperationException(
                $"Conexão com status inválido no Banco MCP: {status.Status}. Reconecte o banco antes de continuar.");

        // Busca nome do banco via accounts (campo "bank", não "name").
        var accounts = await _bankMcpClient.ListAccountsAsync(externalConnectionId, ct);
        var institutionName = accounts.FirstOrDefault()?.DisplayName ?? "Banco";
        var connectorId = accounts.FirstOrDefault()?.ConnectorId ?? string.Empty;

        var connection = new BankConnection
        {
            UserId = userId,
            ExternalConnectionId = externalConnectionId,
            ConnectorId = connectorId,
            InstitutionName = institutionName,
            Status = BankConnectionStatus.Connected,
            ConnectedAt = DateTime.UtcNow
        };

        await _unitOfWork.BankConnections.AddAsync(connection);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Conexão {ExternalConnectionId} ({InstitutionName}) registrada para usuário {UserId}",
            externalConnectionId, institutionName, userId);

        return MapToDto(connection);
    }

    public async Task<BankMcpAvailableAccountsResponseDto> GetConnectionAccountsAsync(string userId, string connectionId, CancellationToken ct)
    {
        var connection = await _unitOfWork.BankConnections.GetByUserIdAndIdAsync(userId, connectionId)
            ?? throw new KeyNotFoundException("Conexão não encontrada");

        if (connection.Status == BankConnectionStatus.Error)
            throw new InvalidOperationException("Conexão com erro — reconecte o banco");

        var accounts = await _bankMcpClient.ListAccountsAsync(connection.ExternalConnectionId, ct);

        return new BankMcpAvailableAccountsResponseDto
        {
            ConnectionId = connectionId,
            Accounts = accounts.Select(a => new BankMcpAccountDto
            {
                AccountId = a.AccountId,
                Type = a.Type,
                Subtype = a.Subtype,
                DisplayName = a.DisplayName, // campo "bank" da API
                Number = a.Number,
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

        // Salva mapeamentos de contas.
        connection.SelectedAccounts = request.AccountMappings.Select(m => new SelectedBankAccount
        {
            ExternalAccountId = m.ExternalAccountId,
            Type = m.ExternalAccountType,
            Subtype = m.ExternalAccountSubtype,
            Number = m.ExternalAccountNumber,
            BankName = m.BankName,
            MoneyManagerAccountId = m.MoneyManagerAccountId
        }).ToList();

        connection.OnboardingStrategy = request.Strategy;

        // Atualiza ExternalAccountId nas Accounts do MoneyManager mapeadas.
        foreach (var mapping in request.AccountMappings)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(mapping.MoneyManagerAccountId);
            if (account is null || account.UserId != userId) continue;

            account.ExternalAccountId = mapping.ExternalAccountId;
            await _unitOfWork.Accounts.UpdateAsync(account);
        }

        // Aplica estratégia de dados históricos.
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

        // Primeiro sync imediato.
        await SyncConnectionAsync(connection, ct);

        _processLogger.AddStep("Onboarding concluído", new Dictionary<string, object?>
        {
            ["connectionId"] = connectionId,
            ["accountsMapped"] = request.AccountMappings.Count
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

        // Remove ExternalAccountId das Accounts mapeadas.
        foreach (var selected in connection.SelectedAccounts.Where(s => s.MoneyManagerAccountId is not null))
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(selected.MoneyManagerAccountId!);
            if (account is null || account.UserId != userId) continue;

            account.ExternalAccountId = null;
            await _unitOfWork.Accounts.UpdateAsync(account);
        }

        // Revoga no Banco MCP.
        try
        {
            await _bankMcpClient.DisconnectAsync(connection.ExternalConnectionId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Falha ao revogar conexão {ExternalConnectionId} no Banco MCP — prosseguindo com soft delete local",
                connection.ExternalConnectionId);
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

        await SyncConnectionAsync(connection, ct);
    }

    public async Task SyncAllActiveConnectionsAsync(CancellationToken ct)
    {
        var connections = await _unitOfWork.BankConnections.GetAllConnectedAsync();

        _processLogger.AddStep("Iniciando sync bancário periódico",
            new Dictionary<string, object?> { ["totalConexoes"] = connections.Count() });

        foreach (var connection in connections)
        {
            try
            {
                await SyncConnectionAsync(connection, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao sincronizar conexão {ConnectionId} do usuário {UserId}",
                    connection.Id, connection.UserId);
                // Não propaga — uma conexão com erro não interrompe as demais.
            }
        }

        _processLogger.AddStep("Sync bancário periódico finalizado");
    }

    // ── Métodos privados ───────────────────────────────────────────────────

    private async Task SyncConnectionAsync(BankConnection connection, CancellationToken ct)
    {
        var mappedAccounts = connection.SelectedAccounts
            .Where(s => s.MoneyManagerAccountId is not null)
            .ToList();

        foreach (var selected in mappedAccounts)
        {
            try
            {
                var since = selected.LastSyncAt ?? connection.CutoffDate ?? DateTime.UtcNow.AddMonths(-12);
                var page = 1;
                var imported = 0;

                // Pagina até buscar todas as transações do período.
                while (true)
                {
                    var result = await _bankMcpClient.ListTransactionsAsync(
                        selected.ExternalAccountId,
                        since,
                        DateTime.UtcNow,
                        page,
                        pageSize: 500,
                        ct);

                    foreach (var tx in result.Results.Where(t => t.Status == "POSTED"))
                    {
                        await UpsertTransactionAsync(connection.UserId, selected.MoneyManagerAccountId!, tx, ct);
                        imported++;
                    }

                    if (page >= result.TotalPages) break;
                    page++;
                }

                selected.LastSyncAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Conta {AccountId} ({BankName}) sincronizada: {Count} transações para usuário {UserId}",
                    selected.ExternalAccountId, selected.BankName, imported, connection.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao sincronizar conta {AccountId}", selected.ExternalAccountId);
            }
        }

        connection.LastSyncAt = DateTime.UtcNow;
        connection.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.BankConnections.UpdateAsync(connection);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task UpsertTransactionAsync(string userId, string accountId, BankMcpTransaction tx, CancellationToken ct)
    {
        // Deduplicação por ExternalId — nunca cria duplicata.
        var existing = await _unitOfWork.Transactions.GetByExternalIdAsync(userId, tx.Id);
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
            ExternalId = tx.Id,
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

        if (lastManual is null) return twelveMonthsAgo;

        var cutoff = lastManual.Value.AddDays(-15);
        return cutoff < twelveMonthsAgo ? twelveMonthsAgo : cutoff;
    }

    private static BankConnectionResponseDto MapToDto(BankConnection c) => new()
    {
        Id = c.Id,
        InstitutionName = c.InstitutionName,
        Status = c.Status.ToString(),
        ConnectedAt = c.ConnectedAt,
        LastSyncAt = c.LastSyncAt,
        SelectedAccounts = c.SelectedAccounts.Select(s => new SelectedBankAccountDto
        {
            ExternalAccountId = s.ExternalAccountId,
            BankName = s.BankName,
            Type = s.Type,
            Subtype = s.Subtype,
            Number = s.Number,
            MoneyManagerAccountId = s.MoneyManagerAccountId,
            LastSyncAt = s.LastSyncAt
        }).ToList()
    };
}

public sealed class BancoMcpOptions
{
    public const string SectionName = "BancoMcp";
    public string ToolkitId { get; set; } = string.Empty;
}
