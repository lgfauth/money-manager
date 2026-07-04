using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Tests.Application.Services;

public class BankConnectionServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IUserRepository _userRepo;
    private readonly IBankConnectionRepository _bankConnectionRepo;
    private readonly IBankMcpClient _bankMcpClient;
    private readonly ISubscriptionService _subscriptionServiceMock;
    private readonly IEncryptionService _encryptionServiceMock;
    private readonly BankConnectionService _service;

    private const string UserId = "user1";

    public BankConnectionServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _userRepo = Substitute.For<IUserRepository>();
        _bankConnectionRepo = Substitute.For<IBankConnectionRepository>();
        _bankMcpClient = Substitute.For<IBankMcpClient>();
        _subscriptionServiceMock = Substitute.For<ISubscriptionService>();
        _encryptionServiceMock = Substitute.For<IEncryptionService>();

        _unitOfWorkMock.Users.Returns(_userRepo);
        _unitOfWorkMock.BankConnections.Returns(_bankConnectionRepo);
        _encryptionServiceMock.Encrypt(Arg.Any<string>()).Returns(x => "enc:" + x.Arg<string>());
        _encryptionServiceMock.Decrypt(Arg.Any<string>()).Returns(x => x.Arg<string>()["enc:".Length..]);

        _service = new BankConnectionService(
            _unitOfWorkMock,
            _bankMcpClient,
            _subscriptionServiceMock,
            Substitute.For<IAccountService>(),
            Substitute.For<ICreditCardService>(),
            Substitute.For<ICreditCardInvoiceService>(),
            Substitute.For<ITransactionService>(),
            Substitute.For<ICreditCardTransactionService>(),
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
}
