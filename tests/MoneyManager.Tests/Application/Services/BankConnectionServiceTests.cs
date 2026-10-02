using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Tests.Application.Services;

public class BankConnectionServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IUserRepository _userRepo;
    private readonly IBankConnectionRepository _bankConnectionRepo;
    private readonly ICreditCardRepository _cardRepo;
    private readonly IBankMcpClient _bankMcpClient;
    private readonly ISubscriptionService _subscriptionServiceMock;
    private readonly IEncryptionService _encryptionServiceMock;
    private readonly ICreditCardService _creditCardServiceMock;
    private readonly ICreditCardInvoiceService _creditCardInvoiceServiceMock;
    private readonly ICreditCardTransactionService _creditCardTransactionServiceMock;
    private readonly ICreditCardTransactionRepository _cardTxRepo;
    private readonly BankConnectionService _service;

    private const string UserId = "user1";

    public BankConnectionServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _userRepo = Substitute.For<IUserRepository>();
        _bankConnectionRepo = Substitute.For<IBankConnectionRepository>();
        _cardRepo = Substitute.For<ICreditCardRepository>();
        _bankMcpClient = Substitute.For<IBankMcpClient>();
        _subscriptionServiceMock = Substitute.For<ISubscriptionService>();
        _encryptionServiceMock = Substitute.For<IEncryptionService>();
        _creditCardServiceMock = Substitute.For<ICreditCardService>();
        _creditCardInvoiceServiceMock = Substitute.For<ICreditCardInvoiceService>();
        _creditCardTransactionServiceMock = Substitute.For<ICreditCardTransactionService>();
        _cardTxRepo = Substitute.For<ICreditCardTransactionRepository>();

        _unitOfWorkMock.Users.Returns(_userRepo);
        _unitOfWorkMock.BankConnections.Returns(_bankConnectionRepo);
        _unitOfWorkMock.CreditCards.Returns(_cardRepo);
        _unitOfWorkMock.CreditCardTransactions.Returns(_cardTxRepo);
        _encryptionServiceMock.Encrypt(Arg.Any<string>()).Returns(x => "enc:" + x.Arg<string>());
        _encryptionServiceMock.Decrypt(Arg.Any<string>()).Returns(x => x.Arg<string>()["enc:".Length..]);

        _service = new BankConnectionService(
            _unitOfWorkMock,
            _bankMcpClient,
            _subscriptionServiceMock,
            Substitute.For<IAccountService>(),
            _creditCardServiceMock,
            _creditCardInvoiceServiceMock,
            Substitute.For<ITransactionService>(),
            _creditCardTransactionServiceMock,
            Substitute.For<IRecurringTransactionService>(),
            Substitute.For<IOpenBankingCategoryMigrationService>(),
            _encryptionServiceMock,
            Substitute.For<IProcessLogger>(),
            Substitute.For<ILogger<BankConnectionService>>());
    }

    [Fact]
    public async Task SaveBankMcpApiKeyAsync_WithValidKey_ShouldEncryptAndPersist()
    {
        var user = new User { Id = UserId, BankMcpKeyExpiredAt = DateTime.UtcNow };
        _userRepo.GetByIdAsync(UserId).Returns(user);
        _bankMcpClient.ListConnectionsAsync("minha-key", Arg.Any<CancellationToken>())
            .Returns(new BankMcpListConnectionsResult(
                new List<BankMcpConnection> { new("item1", "612", "Nubank", "UPDATED") },
                1,
                "https://mcp/add"));

        var result = await _service.SaveBankMcpApiKeyAsync(UserId, "minha-key", CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Equal(1, result.AvailableConnections);
        Assert.Equal("enc:minha-key", user.BankMcpApiKey);
        Assert.Null(user.BankMcpKeyExpiredAt); // chave nova limpa a marcação de expiração
        await _userRepo.Received(1).UpdateAsync(user);
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task SaveBankMcpApiKeyAsync_WithInvalidKey_ShouldThrowInvalidOperation()
    {
        _bankMcpClient.ListConnectionsAsync("key-invalida", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BankMcpListConnectionsResult>(new HttpRequestException("401")));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SaveBankMcpApiKeyAsync(UserId, "key-invalida", CancellationToken.None));

        await _userRepo.DidNotReceive().UpdateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task SaveBankMcpApiKeyAsync_WithoutPremium_ShouldPropagatePremiumRequired()
    {
        _subscriptionServiceMock.EnsurePremiumAccessAsync(UserId)
            .Returns(Task.FromException(new PremiumRequiredException()));

        await Assert.ThrowsAsync<PremiumRequiredException>(
            () => _service.SaveBankMcpApiKeyAsync(UserId, "key", CancellationToken.None));

        await _bankMcpClient.DidNotReceive().ListConnectionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveBankMcpApiKeyAsync_WithUnknownUser_ShouldThrowKeyNotFound()
    {
        _userRepo.GetByIdAsync(UserId).Returns((User?)null);
        _bankMcpClient.ListConnectionsAsync("key", Arg.Any<CancellationToken>())
            .Returns(new BankMcpListConnectionsResult(new List<BankMcpConnection>(), 0, ""));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SaveBankMcpApiKeyAsync(UserId, "key", CancellationToken.None));
    }

    [Fact]
    public async Task GetAvailableConnectionsAsync_WithoutApiKey_ShouldReturnHasApiKeyFalse()
    {
        _userRepo.GetByIdAsync(UserId).Returns(new User { Id = UserId, BankMcpApiKey = null });

        var result = await _service.GetAvailableConnectionsAsync(UserId, CancellationToken.None);

        Assert.False(result.HasApiKey);
        Assert.False(result.ApiKeyExpired);
        Assert.Empty(result.Connections);
        await _bankMcpClient.DidNotReceive().ListConnectionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAvailableConnectionsAsync_WithExpiredApiKey_ShouldReturnApiKeyExpired()
    {
        _userRepo.GetByIdAsync(UserId).Returns(new User
        {
            Id = UserId,
            BankMcpApiKey = "enc:key",
            BankMcpKeyExpiredAt = DateTime.UtcNow.AddDays(-1)
        });

        var result = await _service.GetAvailableConnectionsAsync(UserId, CancellationToken.None);

        Assert.True(result.HasApiKey);
        Assert.True(result.ApiKeyExpired);
        Assert.Empty(result.Connections);
        await _bankMcpClient.DidNotReceive().ListConnectionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAvailableConnectionsAsync_WithoutPremium_ShouldPropagatePremiumRequired()
    {
        _subscriptionServiceMock.EnsurePremiumAccessAsync(UserId)
            .Returns(Task.FromException(new PremiumRequiredException()));

        await Assert.ThrowsAsync<PremiumRequiredException>(
            () => _service.GetAvailableConnectionsAsync(UserId, CancellationToken.None));
    }

    [Fact]
    public async Task SyncNowAsync_WhenAccountAlreadySynced_ShouldQueryFromTenDaysBeforeLastSync()
    {
        // Arrange — conexão já sincronizada anteriormente (LastSyncAt definido na conta).
        var lastSyncAt = new DateTime(2026, 7, 8, 18, 0, 0, DateTimeKind.Utc);
        const string externalAccountId = "acc-ext-1";
        const string connectionId = "conn1";

        var connection = new BankConnection
        {
            Id = connectionId,
            UserId = UserId,
            ExternalConnectionId = "item-1",
            Status = BankConnectionStatus.Connected,
            SelectedAccounts =
            [
                new SelectedBankAccount
                {
                    ExternalAccountId = externalAccountId,
                    Type = "BANK",
                    MoneyManagerAccountId = "mm-acc-1",
                    MoneyManagerEntityType = "Account",
                    LastSyncAt = lastSyncAt
                }
            ]
        };

        _bankConnectionRepo.GetByUserIdAndIdAsync(UserId, connectionId).Returns(connection);
        _userRepo.GetByIdAsync(UserId).Returns(new User { Id = UserId, BankMcpApiKey = "enc:key" });
        _unitOfWorkMock.Categories.Returns(Substitute.For<IRepository<Category>>());
        _unitOfWorkMock.Accounts.Returns(Substitute.For<IRepository<Account>>());

        _bankMcpClient.ListAccountsAsync("key", connection.ExternalConnectionId, Arg.Any<CancellationToken>())
            .Returns([]);
        _bankMcpClient.ListTransactionsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new BankMcpTransactionPage(0, 1, 1, []));

        // Act
        await _service.SyncNowAsync(UserId, connectionId, CancellationToken.None);

        // Assert — a busca deve retroceder 10 dias a partir do LastSyncAt.
        await _bankMcpClient.Received(1).ListTransactionsAsync(
            "key",
            externalAccountId,
            lastSyncAt.AddDays(-10),
            Arg.Any<DateTime>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncNowAsync_WhenCreditCardAlreadySynced_ShouldQueryFromSixtyTwoDaysBeforeLastSync()
    {
        // Regressão: transações de cartão chegam ao Open Finance com atraso, mas com a data
        // original da compra. Com a janela curta das contas bancárias elas nunca eram
        // buscadas e as faturas ficavam sem transações vinculadas.
        var lastSyncAt = new DateTime(2026, 10, 2, 6, 0, 0, DateTimeKind.Utc);
        const string connectionId = "conn1";
        const string externalAccountId = "acc-ext-1";
        const string cardId = "card1";

        var connection = new BankConnection
        {
            Id = connectionId,
            UserId = UserId,
            ExternalConnectionId = "item-1",
            Status = BankConnectionStatus.Connected,
            CutoffDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            SelectedAccounts =
            [
                new SelectedBankAccount
                {
                    ExternalAccountId = externalAccountId,
                    Type = "CREDIT",
                    MoneyManagerAccountId = cardId,
                    MoneyManagerEntityType = "CreditCard",
                    LastSyncAt = lastSyncAt
                }
            ]
        };

        _bankConnectionRepo.GetByUserIdAndIdAsync(UserId, connectionId).Returns(connection);
        _userRepo.GetByIdAsync(UserId).Returns(new User { Id = UserId, BankMcpApiKey = "enc:key" });
        _unitOfWorkMock.Categories.Returns(Substitute.For<IRepository<Category>>());
        _cardRepo.GetByIdAsync(cardId).Returns(new CreditCard { Id = cardId, UserId = UserId, ClosingDay = 7, BillingDueDay = 14 });

        _bankMcpClient.ListAccountsAsync("key", connection.ExternalConnectionId, Arg.Any<CancellationToken>())
            .Returns([]);
        _bankMcpClient.ListTransactionsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new BankMcpTransactionPage(0, 1, 1, []));

        await _service.SyncNowAsync(UserId, connectionId, CancellationToken.None);

        await _bankMcpClient.Received(1).ListTransactionsAsync(
            "key",
            externalAccountId,
            lastSyncAt.AddDays(-62),
            Arg.Any<DateTime>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncNowAsync_ForCreditCardAccount_ShouldCorrectClosingDayBeforeImportingTransactions()
    {
        // Regressão: o ClosingDay do cartão precisa ser corrigido a partir dos dados do banco
        // ANTES de importar as transações do período, senão elas são associadas a uma fatura
        // calculada com um ClosingDay desatualizado — divergindo da fatura que o sync mantém
        // como "Aberta" e fazendo-a aparecer sem nenhuma transação vinculada para o usuário.
        const string connectionId = "conn1";
        const string externalAccountId = "acc-ext-1";
        const string cardId = "card1";

        var connection = new BankConnection
        {
            Id = connectionId,
            UserId = UserId,
            ExternalConnectionId = "item-1",
            Status = BankConnectionStatus.Connected,
            SelectedAccounts =
            [
                new SelectedBankAccount
                {
                    ExternalAccountId = externalAccountId,
                    Type = "CREDIT",
                    MoneyManagerAccountId = cardId,
                    MoneyManagerEntityType = "CreditCard"
                }
            ]
        };

        _bankConnectionRepo.GetByUserIdAndIdAsync(UserId, connectionId).Returns(connection);
        _userRepo.GetByIdAsync(UserId).Returns(new User { Id = UserId, BankMcpApiKey = "enc:key" });
        _unitOfWorkMock.Categories.Returns(Substitute.For<IRepository<Category>>());

        // ClosingDay ainda desatualizado no momento do sync — o que o bug expunha.
        var card = new CreditCard { Id = cardId, UserId = UserId, ClosingDay = 1, BillingDueDay = 10 };
        _cardRepo.GetByIdAsync(cardId).Returns(card);

        _bankMcpClient.ListAccountsAsync("key", connection.ExternalConnectionId, Arg.Any<CancellationToken>())
            .Returns(new List<BankMcpAccount>
            {
                new(externalAccountId, "CREDIT", "CREDIT_CARD", "Banco", "1234", 0, "item-1", "612",
                    5000m, 3000m, null, "Visa", null, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(3))
            });

        _bankMcpClient.GetOpenBillAsync("key", externalAccountId, Arg.Any<CancellationToken>())
            .Returns(new BankMcpOpenBillResult(true, 987.65m, DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(10), 5, 0));

        _bankMcpClient.ListTransactionsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new BankMcpTransactionPage(0, 1, 1, []));

        await _service.SyncNowAsync(UserId, connectionId, CancellationToken.None);

        // A correção do cartão (ClosingDay/BillingDueDay a partir do banco) precisa acontecer
        // antes da busca das transações do período — não depois.
        Received.InOrder(() =>
        {
            _creditCardServiceMock.UpdateFromBankSyncAsync(
                UserId, cardId, Arg.Any<UpdateCreditCardFromSyncDto>(), Arg.Any<CancellationToken>());
            _bankMcpClient.ListTransactionsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task SyncNowAsync_ForCreditCardAccount_ShouldSyncClosedInvoicesPaymentStatus()
    {
        // Regressão: faturas antigas pagas via banco ficavam presas como Closed/Overdue porque
        // o sync só olhava a fatura aberta atual. A partir daqui, toda sincronização de cartão
        // também busca as faturas fechadas no Banco MCP e propaga o payment_status confirmado.
        const string connectionId = "conn1";
        const string externalAccountId = "acc-ext-1";
        const string cardId = "card1";

        var connection = new BankConnection
        {
            Id = connectionId,
            UserId = UserId,
            ExternalConnectionId = "item-1",
            Status = BankConnectionStatus.Connected,
            SelectedAccounts =
            [
                new SelectedBankAccount
                {
                    ExternalAccountId = externalAccountId,
                    Type = "CREDIT",
                    MoneyManagerAccountId = cardId,
                    MoneyManagerEntityType = "CreditCard"
                }
            ]
        };

        _bankConnectionRepo.GetByUserIdAndIdAsync(UserId, connectionId).Returns(connection);
        _userRepo.GetByIdAsync(UserId).Returns(new User { Id = UserId, BankMcpApiKey = "enc:key" });
        _unitOfWorkMock.Categories.Returns(Substitute.For<IRepository<Category>>());

        var card = new CreditCard { Id = cardId, UserId = UserId, ClosingDay = 15, BillingDueDay = 22 };
        _cardRepo.GetByIdAsync(cardId).Returns(card);

        _bankMcpClient.ListAccountsAsync("key", connection.ExternalConnectionId, Arg.Any<CancellationToken>())
            .Returns(new List<BankMcpAccount>
            {
                new(externalAccountId, "CREDIT", "CREDIT_CARD", "Banco", "1234", 0, "item-1", "612",
                    5000m, 3000m, null, "Visa", null, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(3))
            });

        _bankMcpClient.GetOpenBillAsync("key", externalAccountId, Arg.Any<CancellationToken>())
            .Returns((BankMcpOpenBillResult?)null);

        var closedBills = new BankMcpCreditCardBillPage(new List<BankMcpCreditCardBill>
        {
            new("bill1", DateTime.UtcNow.AddDays(-20), 120m, DateTime.UtcNow.AddDays(-27), "PAID", [])
        });
        _bankMcpClient.ListCreditCardBillsAsync("key", externalAccountId, Arg.Any<CancellationToken>())
            .Returns(closedBills);

        _bankMcpClient.ListTransactionsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new BankMcpTransactionPage(0, 1, 1, []));

        await _service.SyncNowAsync(UserId, connectionId, CancellationToken.None);

        await _bankMcpClient.Received(1).ListCreditCardBillsAsync("key", externalAccountId, Arg.Any<CancellationToken>());
        await _creditCardInvoiceServiceMock.Received(1).SyncPaymentStatusFromBankAsync(
            UserId, cardId, closedBills.Results, Arg.Any<CancellationToken>());
    }

    // Conexão de cartão já sincronizada, com o Banco MCP devolvendo as transações informadas.
    private const string CardConnectionId = "conn-card";
    private const string CardId = "card1";
    private static readonly DateTime CardLastSyncAt = new(2026, 10, 2, 6, 0, 0, DateTimeKind.Utc);

    private void ArrangeCreditCardSync(params BankMcpTransaction[] transactions)
    {
        var connection = new BankConnection
        {
            Id = CardConnectionId,
            UserId = UserId,
            ExternalConnectionId = "item-1",
            Status = BankConnectionStatus.Connected,
            SelectedAccounts =
            [
                new SelectedBankAccount
                {
                    ExternalAccountId = "acc-ext-1",
                    Type = "CREDIT",
                    MoneyManagerAccountId = CardId,
                    MoneyManagerEntityType = "CreditCard",
                    LastSyncAt = CardLastSyncAt
                }
            ]
        };

        _bankConnectionRepo.GetByUserIdAndIdAsync(UserId, CardConnectionId).Returns(connection);
        _userRepo.GetByIdAsync(UserId).Returns(new User { Id = UserId, BankMcpApiKey = "enc:key" });
        _unitOfWorkMock.Categories.Returns(Substitute.For<IRepository<Category>>());
        _unitOfWorkMock.CreditCardInvoices.Returns(Substitute.For<ICreditCardInvoiceRepository>());
        _cardRepo.GetByIdAsync(CardId).Returns(new CreditCard { Id = CardId, UserId = UserId, ClosingDay = 7, BillingDueDay = 14 });
        _bankMcpClient.ListAccountsAsync("key", connection.ExternalConnectionId, Arg.Any<CancellationToken>())
            .Returns([]);
        _bankMcpClient.ListTransactionsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new BankMcpTransactionPage(transactions.Length, 1, 1, transactions));
    }

    private static BankMcpTransaction CardTx(string id, string status, DateTime date, decimal amount = 100m, string description = "Compra")
        => new(id, "acc-ext-1", date, description, amount, "DEBIT", status, null, null, null);

    [Fact]
    public async Task SyncNowAsync_ForCreditCard_ShouldImportPendingTransactionsOfOpenInvoice()
    {
        // Regressão: compras da fatura aberta vêm como PENDING do Open Finance e eram descartadas,
        // deixando a fatura corrente sempre sem transações.
        ArrangeCreditCardSync(CardTx("tx-pending", "PENDING", new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc)));
        _cardTxRepo.GetByCardAsync(UserId, CardId).Returns([]);

        await _service.SyncNowAsync(UserId, CardConnectionId, CancellationToken.None);

        await _creditCardTransactionServiceMock.Received(1).CreateAsync(
            UserId,
            Arg.Is<CreateCreditCardTransactionRequestDto>(r => r.ExternalId == "tx-pending" && r.IsPending));
    }

    [Fact]
    public async Task SyncNowAsync_WhenPendingCardTransactionIsPosted_ShouldConfirmItWithBankValues()
    {
        var existing = new CreditCardTransaction
        {
            UserId = UserId,
            CreditCardId = CardId,
            InvoiceId = "inv-1",
            ExternalId = "tx-1",
            Source = "bank_sync",
            IsPending = true,
            Description = "AUTORIZACAO",
            PurchaseDate = new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc),
            TotalAmount = 90m,
            InstallmentAmount = 90m
        };
        ArrangeCreditCardSync(CardTx("tx-1", "POSTED", existing.PurchaseDate, 100m, "LOJA X"));
        _cardTxRepo.GetByExternalIdAsync(UserId, "tx-1").Returns(existing);
        _cardTxRepo.GetByCardAsync(UserId, CardId).Returns([existing]);

        await _service.SyncNowAsync(UserId, CardConnectionId, CancellationToken.None);

        Assert.False(existing.IsPending);
        Assert.False(existing.IsDeleted);
        Assert.Equal("LOJA X", existing.Description);
        Assert.Equal(100m, existing.InstallmentAmount);
        await _creditCardInvoiceServiceMock.Received().RecalculateTotalAsync(UserId, "inv-1");
        await _creditCardTransactionServiceMock.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<CreateCreditCardTransactionRequestDto>());
    }

    [Fact]
    public async Task SyncNowAsync_WhenPendingCardTransactionDisappearsFromBank_ShouldRemoveIt()
    {
        var stale = new CreditCardTransaction
        {
            UserId = UserId,
            CreditCardId = CardId,
            InvoiceId = "inv-1",
            ExternalId = "tx-cancelada",
            Source = "bank_sync",
            IsPending = true,
            PurchaseDate = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc)
        };
        var confirmed = new CreditCardTransaction
        {
            UserId = UserId,
            CreditCardId = CardId,
            InvoiceId = "inv-1",
            ExternalId = "tx-antiga",
            Source = "bank_sync",
            IsPending = false,
            PurchaseDate = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc)
        };
        ArrangeCreditCardSync();
        _cardTxRepo.GetByCardAsync(UserId, CardId).Returns([stale, confirmed]);

        await _service.SyncNowAsync(UserId, CardConnectionId, CancellationToken.None);

        Assert.True(stale.IsDeleted);
        Assert.False(confirmed.IsDeleted);
        await _creditCardInvoiceServiceMock.Received().RecalculateTotalAsync(UserId, "inv-1");
    }
}
