using Microsoft.Extensions.Logging;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

public interface IBankConnectionService
{
    // Salva (criptografada) e valida a API key do Banco MCP do usuário.
    Task<SaveApiKeyResultDto> SaveBankMcpApiKeyAsync(string userId, string apiKey, CancellationToken ct);

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

    // Desvincula uma conta/cartão específico do banco conectado.
    Task<UnlinkAccountResponseDto> UnlinkAccountAsync(string userId, string accountId, CancellationToken ct);

    // Desconecta um banco inteiro (item_id), revogando consentimento e removendo vínculo local.
    Task<DisconnectBankResponseDto> DisconnectBankAsync(string userId, string itemId, CancellationToken ct);

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
    private readonly IAccountService _accountService;
    private readonly ICreditCardService _creditCardService;
    private readonly ICreditCardInvoiceService _creditCardInvoiceService;
    private readonly ITransactionService _transactionService;
    private readonly ICreditCardTransactionService _creditCardTransactionService;
    private readonly IRecurringTransactionService _recurringTransactionService;
    private readonly IOpenBankingCategoryMigrationService _categoryMigrationService;
    private readonly IEncryptionService _encryptionService;
    private readonly IProcessLogger _processLogger;
    private readonly ILogger<BankConnectionService> _logger;

    public BankConnectionService(
        IUnitOfWork unitOfWork,
        IBankMcpClient bankMcpClient,
        ISubscriptionService subscriptionService,
        IAccountService accountService,
        ICreditCardService creditCardService,
        ICreditCardInvoiceService creditCardInvoiceService,
        ITransactionService transactionService,
        ICreditCardTransactionService creditCardTransactionService,
        IRecurringTransactionService recurringTransactionService,
        IOpenBankingCategoryMigrationService categoryMigrationService,
        IEncryptionService encryptionService,
        IProcessLogger processLogger,
        ILogger<BankConnectionService> logger)
    {
        _unitOfWork = unitOfWork;
        _bankMcpClient = bankMcpClient;
        _subscriptionService = subscriptionService;
        _accountService = accountService;
        _creditCardService = creditCardService;
        _creditCardInvoiceService = creditCardInvoiceService;
        _transactionService = transactionService;
        _creditCardTransactionService = creditCardTransactionService;
        _recurringTransactionService = recurringTransactionService;
        _categoryMigrationService = categoryMigrationService;
        _encryptionService = encryptionService;
        _processLogger = processLogger;
        _logger = logger;
    }

    public async Task<SaveApiKeyResultDto> SaveBankMcpApiKeyAsync(string userId, string apiKey, CancellationToken ct)
    {
        await _subscriptionService.EnsurePremiumAccessAsync(userId);

        BankMcpListConnectionsResult connections;
        try
        {
            connections = await _bankMcpClient.ListConnectionsAsync(apiKey, ct);
        }
        catch
        {
            throw new InvalidOperationException(
                "API key do Banco MCP inválida ou sem permissão. Verifique e tente novamente.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Usuário não encontrado");

        user.BankMcpApiKey = _encryptionService.Encrypt(apiKey);
        user.BankMcpKeyExpiredAt = null;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "API key do Banco MCP salva para usuário {UserId} - {Count} conexão(ões) disponível(is)",
            userId, connections.Count);

        return new SaveApiKeyResultDto
        {
            IsValid = true,
            AvailableConnections = connections.Count
        };
    }

    public async Task<BankMcpAvailableConnectionsResponseDto> GetAvailableConnectionsAsync(string userId, CancellationToken ct)
    {
        await _subscriptionService.EnsurePremiumAccessAsync(userId);
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Usuário não encontrado");

        if (user.BankMcpKeyExpiredAt.HasValue)
        {
            return new BankMcpAvailableConnectionsResponseDto
            {
                HasApiKey = true,
                ApiKeyExpired = true,
                Connections = [],
                AddConnectionUrl = string.Empty
            };
        }

        if (string.IsNullOrEmpty(user.BankMcpApiKey))
        {
            return new BankMcpAvailableConnectionsResponseDto
            {
                HasApiKey = false,
                ApiKeyExpired = false,
                Connections = [],
                AddConnectionUrl = string.Empty
            };
        }

        var apiKey = _encryptionService.Decrypt(user.BankMcpApiKey);

        var result = await _bankMcpClient.ListConnectionsAsync(apiKey, ct);

        var existingConnections = await _unitOfWork.BankConnections.GetByUserIdAsync(userId);
        var existingDict = existingConnections.ToDictionary(c => c.ExternalConnectionId);

        return new BankMcpAvailableConnectionsResponseDto
        {
            HasApiKey = true,
            ApiKeyExpired = false,
            AddConnectionUrl = result.AddConnectionUrl,
            Connections = result.Connections.Select(c =>
            {
                existingDict.TryGetValue(c.ItemId, out var existingConn);
                var isPendingSetup = existingConn is not null
                    && !existingConn.SelectedAccounts.Any(s => s.MoneyManagerAccountId is not null);

                return new BankMcpConnectionDto
                {
                    ItemId = c.ItemId,
                    ConnectorId = c.ConnectorId,
                    ConnectorName = c.ConnectorName,
                    Status = c.Status,
                    AlreadyRegistered = existingConn is not null && !isPendingSetup,
                    PendingSetup = isPendingSetup,
                    PendingConnectionId = isPendingSetup ? existingConn!.Id : null
                };
            }).ToList()
        };
    }

    public async Task<BankConnectionResponseDto> RegisterConnectionAsync(string userId, string externalConnectionId, CancellationToken ct)
    {
        await _subscriptionService.EnsurePremiumAccessAsync(userId);
        var apiKey = await GetDecryptedApiKeyAsync(userId, ct);

        var existing = await _unitOfWork.BankConnections.GetByExternalConnectionIdAsync(userId, externalConnectionId);
        if (existing is not null)
            throw new InvalidOperationException("Esta conexão bancária já está registrada");

        // Valida status no Banco MCP antes de registrar.
        var status = await _bankMcpClient.GetConnectionStatusAsync(apiKey, externalConnectionId, ct);
        if (status.Status is "LOGIN_ERROR" or "WAITING_USER_INPUT")
            throw new InvalidOperationException(
                $"Conexão com status inválido no Banco MCP: {status.Status}. Reconecte o banco antes de continuar.");

        // Busca nome do banco via accounts (campo "bank", não "name").
        var accounts = await _bankMcpClient.ListAccountsAsync(apiKey, externalConnectionId, ct);
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
        var apiKey = await GetDecryptedApiKeyAsync(userId, ct);

        var connection = await _unitOfWork.BankConnections.GetByUserIdAndIdAsync(userId, connectionId)
            ?? throw new KeyNotFoundException("Conexão não encontrada");

        if (connection.Status == BankConnectionStatus.Error)
            throw new InvalidOperationException("Conexão com erro — reconecte o banco");

        var accounts = await _bankMcpClient.ListAccountsAsync(apiKey, connection.ExternalConnectionId, ct);

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
        var apiKey = await GetDecryptedApiKeyAsync(userId, ct);

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
            MoneyManagerAccountId = m.MoneyManagerAccountId,
            MoneyManagerEntityType = m.MoneyManagerEntityType
        }).ToList();

        connection.OnboardingStrategy = request.Strategy;

        // Atualiza ExternalAccountId nas Accounts do MoneyManager mapeadas.
        foreach (var mapping in request.AccountMappings)
        {
            if (!string.Equals(mapping.MoneyManagerEntityType, "Account", StringComparison.Ordinal))
                continue;

            var account = await _unitOfWork.Accounts.GetByIdAsync(mapping.MoneyManagerAccountId);
            if (account is null || account.UserId != userId) continue;

            account.ExternalAccountId = mapping.ExternalAccountId;
            await _unitOfWork.Accounts.UpdateAsync(account);

            var disabledCount = await _recurringTransactionService
                .DeactivateActiveByAccountAsync(userId, account.Id);

            _logger.LogInformation(
                "Onboarding bancário: {Count} recorrente(s) desativada(s) para a conta {AccountId}",
                disabledCount,
                account.Id);
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

        // Migra as categorias do usuário para o padrão Open Banking imediatamente após o
        // link bem-sucedido — o flag HasMigratedOpenBankingCategories garante idempotência.
        var categoriesMigrated = await _categoryMigrationService
            .MigrateUserToOpenBankingCategoriesAsync(userId);

        // Primeiro sync imediato.
        await SyncConnectionAsync(connection, apiKey, ct);

        _processLogger.AddStep("Onboarding concluído", new Dictionary<string, object?>
        {
            ["connectionId"] = connectionId,
            ["accountsMapped"] = request.AccountMappings.Count,
            ["categoriesMigrated"] = categoriesMigrated
        });

        var dto = MapToDto(connection);
        dto.CategoriesMigrated = categoriesMigrated;
        return dto;
    }

    public async Task<IReadOnlyList<BankConnectionResponseDto>> GetUserConnectionsAsync(string userId, CancellationToken ct)
    {
        var connections = await _unitOfWork.BankConnections.GetByUserIdAsync(userId);
        return connections.Select(MapToDto).ToList();
    }

    public async Task<UnlinkAccountResponseDto> UnlinkAccountAsync(string userId, string accountId, CancellationToken ct)
    {
        return await UnlinkAccountInternalAsync(userId, accountId, ct, triggerAutoDisconnect: true);
    }

    public async Task<DisconnectBankResponseDto> DisconnectBankAsync(string userId, string itemId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var connection = await _unitOfWork.BankConnections.GetByExternalConnectionIdAsync(userId, itemId);
        if (connection is null)
        {
            // Compatibilidade transitória: permite receber o Id local da conexão.
            connection = await _unitOfWork.BankConnections.GetByUserIdAndIdAsync(userId, itemId);
        }

        if (connection is null)
            throw new KeyNotFoundException("Conexão não encontrada");

        var apiKey = await GetDecryptedApiKeyAsync(userId, ct);

        _logger.LogInformation(
            "Iniciando desconexão total do banco {ItemId} para usuário {UserId}",
            connection.ExternalConnectionId,
            userId);

        try
        {
            await _bankMcpClient.DisconnectAsync(apiKey, connection.ExternalConnectionId, ct);
            _logger.LogInformation(
                "Consentimento revogado no Banco MCP para item {ItemId} do usuário {UserId}",
                connection.ExternalConnectionId,
                userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao revogar consentimento no Banco MCP para item {ItemId} do usuário {UserId}",
                connection.ExternalConnectionId,
                userId);

            throw new InvalidOperationException("Falha ao revogar consentimento no Open Finance. Nenhuma alteração local foi aplicada.");
        }

        var linkedAccountIds = connection.SelectedAccounts
            .Where(s => !string.IsNullOrWhiteSpace(s.MoneyManagerAccountId))
            .Select(s => s.MoneyManagerAccountId!)
            .Distinct()
            .ToList();

        foreach (var linkedAccountId in linkedAccountIds)
        {
            await UnlinkAccountInternalAsync(userId, linkedAccountId, ct, triggerAutoDisconnect: false);
        }

        await _unitOfWork.BankConnections.DeleteAsync(connection.Id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Conexão bancária {ItemId} removida fisicamente para usuário {UserId}",
            connection.ExternalConnectionId,
            userId);

        return new DisconnectBankResponseDto
        {
            ItemId = connection.ExternalConnectionId,
            Success = true,
            UnlinkedAccountsCount = linkedAccountIds.Count
        };
    }

    private async Task<UnlinkAccountResponseDto> UnlinkAccountInternalAsync(
        string userId,
        string accountId,
        CancellationToken ct,
        bool triggerAutoDisconnect)
    {
        ct.ThrowIfCancellationRequested();

        var connections = (await _unitOfWork.BankConnections.GetByUserIdAsync(userId)).ToList();

        var match = connections
            .Select(c => new
            {
                Connection = c,
                Selected = c.SelectedAccounts.FirstOrDefault(s =>
                    string.Equals(s.MoneyManagerAccountId, accountId, StringComparison.Ordinal))
            })
            .FirstOrDefault(x => x.Selected is not null)
            ?? throw new KeyNotFoundException("Conta/cartão não está vinculado a nenhuma conexão bancária");

        var connection = match.Connection;
        var selected = match.Selected!;
        var entityType = selected.MoneyManagerEntityType;
        var hasDeactivatedRecurrences = false;

        if (string.Equals(entityType, "Account", StringComparison.Ordinal))
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
            if (account is null || account.UserId != userId || account.IsDeleted)
                throw new KeyNotFoundException("Conta não encontrada");

            account.ExternalAccountId = null;
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);

            hasDeactivatedRecurrences = await HasDeactivatedRecurrencesAsync(userId, accountId);
        }
        else if (string.Equals(entityType, "CreditCard", StringComparison.Ordinal))
        {
            var creditCard = await _unitOfWork.CreditCards.GetByIdAsync(accountId);
            if (creditCard is null || creditCard.UserId != userId || creditCard.IsDeleted)
                throw new KeyNotFoundException("Cartão não encontrado");

            creditCard.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CreditCards.UpdateAsync(creditCard);
        }
        else
        {
            throw new InvalidOperationException($"Tipo de entidade não suportado para desvínculo: {entityType}");
        }

        connection.SelectedAccounts = connection.SelectedAccounts
            .Where(s => !string.Equals(s.MoneyManagerAccountId, accountId, StringComparison.Ordinal))
            .ToList();
        connection.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.BankConnections.UpdateAsync(connection);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Conta/cartão {AccountId} desvinculado do item {ItemId} para usuário {UserId}",
            accountId,
            connection.ExternalConnectionId,
            userId);

        var hasLinkedAccounts = connection.SelectedAccounts.Any(s =>
            !string.IsNullOrWhiteSpace(s.MoneyManagerAccountId));

        if (triggerAutoDisconnect && !hasLinkedAccounts)
        {
            _logger.LogInformation(
                "Item {ItemId} sem contas/cartões vinculados após unlink; iniciando desconexão total",
                connection.ExternalConnectionId);

            await DisconnectBankAsync(userId, connection.ExternalConnectionId, ct);
        }

        return new UnlinkAccountResponseDto
        {
            AccountId = accountId,
            ItemId = connection.ExternalConnectionId,
            Success = true,
            HasDeactivatedRecurrences = hasDeactivatedRecurrences
        };
    }

    private async Task<bool> HasDeactivatedRecurrencesAsync(string userId, string accountId)
    {
        var recurrences = await _unitOfWork.RecurringTransactions.GetAllAsync();

        return recurrences.Any(r =>
            r.UserId == userId
            && r.AccountId == accountId
            && !r.IsDeleted
            && !r.IsActive);
    }

    public async Task SyncNowAsync(string userId, string connectionId, CancellationToken ct)
    {
        var connection = await _unitOfWork.BankConnections.GetByUserIdAndIdAsync(userId, connectionId)
            ?? throw new KeyNotFoundException("Conexão não encontrada");

        if (connection.Status != BankConnectionStatus.Connected)
            throw new InvalidOperationException("Conexão não está ativa");

        var apiKey = await GetDecryptedApiKeyAsync(userId, ct);
        await SyncConnectionAsync(connection, apiKey, ct);
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
                string apiKey;
                try
                {
                    apiKey = await GetDecryptedApiKeyAsync(connection.UserId, ct);
                }
                catch (InvalidOperationException)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} sem API key configurada - pulando conexão {ConnectionId}",
                        connection.UserId, connection.Id);
                    continue;
                }

                await SyncConnectionAsync(connection, apiKey, ct);
            }
            catch (BankMcpKeyExpiredException)
            {
                _logger.LogWarning(
                    "API key do Banco MCP expirada para usuário {UserId} - marcando e notificando",
                    connection.UserId);

                var user = await _unitOfWork.Users.GetByIdAsync(connection.UserId);
                if (user is not null)
                {
                    user.BankMcpKeyExpiredAt = DateTime.UtcNow;
                    await _unitOfWork.Users.UpdateAsync(user);
                }

                var userConnections = await _unitOfWork.BankConnections.GetByUserIdAsync(connection.UserId);
                foreach (var userConnection in userConnections)
                {
                    userConnection.MarkError();
                    await _unitOfWork.BankConnections.UpdateAsync(userConnection);
                }

                await _unitOfWork.SaveChangesAsync();
                continue;
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

    private async Task SyncConnectionAsync(BankConnection connection, string apiKey, CancellationToken ct)
    {
        var bankAccounts = await _bankMcpClient.ListAccountsAsync(apiKey, connection.ExternalConnectionId, ct);
        var bankAccountsByExternalId = bankAccounts.ToDictionary(a => a.AccountId);

        var mappedAccounts = connection.SelectedAccounts
            .Where(s => s.MoneyManagerAccountId is not null)
            .ToList();

        // Mapa OpenBankingCategoryId -> CategoryId do usuário, carregado uma única vez por sync.
        var userCategories = await _unitOfWork.Categories.GetByUserIdAsync(connection.UserId);
        var categoryMap = userCategories
            .Where(c => !c.IsDeleted && !string.IsNullOrEmpty(c.OpenBankingCategoryId))
            .ToDictionary(c => c.OpenBankingCategoryId!, c => c.Id);

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
                        apiKey,
                        selected.ExternalAccountId,
                        since,
                        DateTime.UtcNow,
                        page,
                        pageSize: 500,
                        ct);

                    foreach (var tx in result.Results.Where(t => t.Status == "POSTED"))
                    {
                        if (string.Equals(selected.MoneyManagerEntityType, "CreditCard", StringComparison.Ordinal))
                            await UpsertCreditCardTransactionAsync(connection.UserId, selected.MoneyManagerAccountId!, tx, categoryMap, ct);
                        else
                            await UpsertTransactionAsync(connection.UserId, selected.MoneyManagerAccountId!, tx, categoryMap, ct);

                        imported++;
                    }

                    if (page >= result.TotalPages) break;
                    page++;
                }

                if (string.Equals(selected.Type, "BANK", StringComparison.Ordinal))
                    await UpdateBankAccountBalanceAsync(connection.UserId, selected, bankAccountsByExternalId);

                if (string.Equals(selected.Type, "CREDIT", StringComparison.Ordinal))
                    await UpdateCreditCardFromSyncAsync(connection.UserId, apiKey, selected, bankAccountsByExternalId, ct);

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

    private async Task UpdateBankAccountBalanceAsync(
        string userId,
        SelectedBankAccount selected,
        IReadOnlyDictionary<string, BankMcpAccount> bankAccountsByExternalId)
    {
        if (!bankAccountsByExternalId.TryGetValue(selected.ExternalAccountId, out var bankAccount))
            return;

        var account = await _unitOfWork.Accounts.GetByIdAsync(selected.MoneyManagerAccountId!);
        if (account is null || account.UserId != userId || account.IsDeleted)
            return;

        var delta = bankAccount.Balance - account.Balance;
        if (delta == 0m)
            return;

        await _accountService.UpdateBalanceAsync(userId, selected.MoneyManagerAccountId!, delta);
    }

    private async Task UpdateCreditCardFromSyncAsync(
        string userId,
        string apiKey,
        SelectedBankAccount selected,
        IReadOnlyDictionary<string, BankMcpAccount> bankAccountsByExternalId,
        CancellationToken ct)
    {
        if (!bankAccountsByExternalId.TryGetValue(selected.ExternalAccountId, out var mcpCard))
            return;

        var openBill = await _bankMcpClient.GetOpenBillAsync(apiKey, selected.ExternalAccountId, ct);

        var dueDay = openBill?.DueDate?.Day ?? mcpCard.BalanceDueDate?.Day;
        var closingDay = openBill?.CloseDate?.Day;
        if (!closingDay.HasValue && dueDay.HasValue)
            closingDay = Math.Max(dueDay.Value - 7, 1);

        await _creditCardService.UpdateFromBankSyncAsync(
            userId,
            selected.MoneyManagerAccountId!,
            new UpdateCreditCardFromSyncDto
            {
                Limit = mcpCard.CreditLimit,
                AvailableLimit = mcpCard.AvailableCreditLimit,
                Brand = mcpCard.Brand,
                DueDay = dueDay,
                ClosingDay = closingDay
            },
            ct);

        if (openBill is null)
        {
            _logger.LogInformation(
                "Banco não expõe fatura aberta para account {ExternalAccountId} no momento",
                selected.ExternalAccountId);
            return;
        }

        if (!openBill.DueDate.HasValue)
        {
            _logger.LogInformation(
                "Fatura aberta sem due_date para account {ExternalAccountId}; atualização da fatura ignorada",
                selected.ExternalAccountId);
            return;
        }

        await _creditCardInvoiceService.UpdateOrCreateOpenInvoiceAsync(
            userId,
            selected.MoneyManagerAccountId!,
            new UpdateOpenInvoiceFromSyncDto
            {
                TotalAmount = openBill.TotalAmount,
                DueDate = openBill.DueDate.Value,
                CloseDate = openBill.CloseDate
            },
            ct);
    }

    private async Task<string> GetDecryptedApiKeyAsync(string userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Usuário não encontrado");

        if (string.IsNullOrEmpty(user.BankMcpApiKey))
            throw new InvalidOperationException(
                "API key do Banco MCP não configurada. Configure nas configurações da conta.");

        return _encryptionService.Decrypt(user.BankMcpApiKey);
    }

    private async Task UpsertTransactionAsync(
        string userId, string accountId, BankMcpTransaction tx,
        IDictionary<string, string> categoryMap, CancellationToken ct)
    {
        var existing = await _unitOfWork.Transactions.GetByExternalIdAsync(userId, tx.Id);
        if (existing is not null)
        {
            // Backfill: transações importadas antes da resolução de categorias podem estar sem
            // o categoryId de origem e/ou sem categoria — preenche sem sobrescrever o que existe.
            if (!string.IsNullOrEmpty(tx.CategoryId)
                && (string.IsNullOrEmpty(existing.OpenBankingCategoryId) || string.IsNullOrEmpty(existing.CategoryId)))
            {
                if (string.IsNullOrEmpty(existing.OpenBankingCategoryId))
                    existing.OpenBankingCategoryId = tx.CategoryId;
                if (string.IsNullOrEmpty(existing.CategoryId))
                    existing.CategoryId = await ResolveCategoryIdAsync(userId, tx.CategoryId, categoryMap);

                existing.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Transactions.UpdateAsync(existing);
                await _unitOfWork.SaveChangesAsync();
            }

            return;
        }

        await _transactionService.CreateAsync(userId, new CreateTransactionRequestDto
        {
            AccountId = accountId,
            Amount = Math.Abs(tx.Amount),
            CategoryId = await ResolveCategoryIdAsync(userId, tx.CategoryId, categoryMap),
            Type = tx.Amount < 0 ? TransactionType.Expense : TransactionType.Income,
            Date = tx.Date,
            Description = tx.Description,
            Tags = [],
            Notes = null,
            ToAccountId = null,
            Status = TransactionStatus.Completed,
            ClientRequestId = $"bank-sync:{tx.Id}",
            Source = "bank_sync",
            ExternalId = tx.Id,
            OpenBankingCategoryId = tx.CategoryId
        });
    }

    private async Task UpsertCreditCardTransactionAsync(
        string userId, string creditCardId, BankMcpTransaction tx,
        IDictionary<string, string> categoryMap, CancellationToken ct)
    {
        var existing = await _unitOfWork.CreditCardTransactions.GetByExternalIdAsync(userId, tx.Id);
        if (existing is not null)
        {
            // Backfill: mesmo tratamento das transações bancárias — só preenche o que está vazio.
            if (!string.IsNullOrEmpty(tx.CategoryId)
                && (string.IsNullOrEmpty(existing.OpenBankingCategoryId) || string.IsNullOrEmpty(existing.CategoryId)))
            {
                if (string.IsNullOrEmpty(existing.OpenBankingCategoryId))
                    existing.OpenBankingCategoryId = tx.CategoryId;
                if (string.IsNullOrEmpty(existing.CategoryId))
                    existing.CategoryId = await ResolveCategoryIdAsync(userId, tx.CategoryId, categoryMap);

                existing.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardTransactions.UpdateAsync(existing);
                await _unitOfWork.SaveChangesAsync();
            }

            return;
        }

        await _creditCardTransactionService.CreateAsync(userId, new CreateCreditCardTransactionRequestDto
        {
            CreditCardId = creditCardId,
            Description = tx.Description,
            CategoryId = await ResolveCategoryIdAsync(userId, tx.CategoryId, categoryMap),
            PurchaseDate = tx.Date,
            TotalAmount = Math.Abs(tx.Amount),
            TotalInstallments = 1,
            FirstInstallmentOnCurrentInvoice = true,
            IsRefund = false,
            ClientRequestId = null,
            Source = "bank_sync",
            ExternalId = tx.Id,
            OpenBankingCategoryId = tx.CategoryId
        });
    }

    // Resolve a categoria do usuário pelo categoryId do Pluggy. Transações sem categoryId
    // no payload seguem o fluxo manual/Haiku existente (CategoryId = null).
    private async Task<string?> ResolveCategoryIdAsync(
        string userId, string? openBankingCategoryId, IDictionary<string, string> categoryMap)
    {
        if (string.IsNullOrEmpty(openBankingCategoryId))
            return null;

        if (categoryMap.TryGetValue(openBankingCategoryId, out var categoryId))
            return categoryId;

        // Não encontrado no mapa — o serviço resolve com fallback "Outros" (criando se necessário).
        categoryId = await _categoryMigrationService.ResolveUserCategoryIdAsync(userId, openBankingCategoryId);
        categoryMap[openBankingCategoryId] = categoryId;
        return categoryId;
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
            MoneyManagerEntityType = s.MoneyManagerEntityType,
            LastSyncAt = s.LastSyncAt
        }).ToList()
    };
}
