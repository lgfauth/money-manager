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

public class SubscriptionServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly IUserRepository _userRepo;
    private readonly IPaymentGateway _paymentGateway;
    private readonly SubscriptionService _service;

    public SubscriptionServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _subscriptionRepo = Substitute.For<ISubscriptionRepository>();
        _userRepo = Substitute.For<IUserRepository>();
        _paymentGateway = Substitute.For<IPaymentGateway>();
        _unitOfWorkMock.Subscriptions.Returns(_subscriptionRepo);
        _unitOfWorkMock.Users.Returns(_userRepo);
        _paymentGateway.ProviderName.Returns("efi");

        _service = new SubscriptionService(
            _unitOfWorkMock,
            _paymentGateway,
            Substitute.For<IProcessLogger>(),
            Substitute.For<ILogger<SubscriptionService>>());
    }

    [Fact]
    public async Task InitializeFreeAsync_WithoutExistingSubscription_ShouldCreateExpiredFreePlan()
    {
        _subscriptionRepo.GetByUserIdAsync("user1").Returns((Subscription?)null);
        _subscriptionRepo.AddAsync(Arg.Any<Subscription>()).Returns(x => x.Arg<Subscription>());

        var result = await _service.InitializeFreeAsync("user1");

        Assert.Equal(PlanType.Free.ToString(), result.Plan);
        Assert.Equal(SubscriptionStatus.Expired.ToString(), result.Status);
        Assert.False(result.IsPremiumActive);
        await _subscriptionRepo.Received(1).AddAsync(Arg.Any<Subscription>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task InitializeFreeAsync_WithExistingSubscription_ShouldThrow()
    {
        _subscriptionRepo.GetByUserIdAsync("user1").Returns(new Subscription { UserId = "user1" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.InitializeFreeAsync("user1"));
    }

    [Fact]
    public async Task ActivateTrialAsync_WithoutExistingSubscription_ShouldCreateActiveTrial()
    {
        _subscriptionRepo.GetByUserIdAsync("user1").Returns((Subscription?)null);
        _subscriptionRepo.AddAsync(Arg.Any<Subscription>()).Returns(x => x.Arg<Subscription>());

        var result = await _service.ActivateTrialAsync("user1");

        Assert.Equal(SubscriptionStatus.Trial.ToString(), result.Status);
        Assert.NotNull(result.TrialEndsAt);
        Assert.True(result.TrialEndsAt > DateTime.UtcNow.AddDays(13));
        Assert.True(result.IsPremiumActive);
    }

    [Fact]
    public async Task ActivateTrialAsync_WithExistingSubscription_ShouldThrow()
    {
        _subscriptionRepo.GetByUserIdAsync("user1").Returns(new Subscription { UserId = "user1" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ActivateTrialAsync("user1"));
    }

    [Fact]
    public async Task CreateAsync_WithTrialSubscription_ShouldCallGatewayAndKeepTrialStatus()
    {
        var subscription = new Subscription { UserId = "user1", Status = SubscriptionStatus.Trial };
        _subscriptionRepo.GetByUserIdAsync("user1").Returns(subscription);
        _paymentGateway.CreateSubscriptionAsync(Arg.Any<CreateSubscriptionGatewayRequest>())
            .Returns(new CreateSubscriptionGatewayResult
            {
                ExternalSubscriptionId = "ext-1",
                PaymentUrl = "https://pay.example/1"
            });

        var result = await _service.CreateAsync("user1", new CreateSubscriptionRequestDto
        {
            PayerName = "Luan",
            PayerCpf = "12345678900",
            PayerEmail = "luan@example.com"
        });

        Assert.Equal("ext-1", result.ExternalSubscriptionId);
        Assert.Equal("https://pay.example/1", result.PaymentUrl);
        Assert.Equal(SubscriptionStatus.Trial, subscription.Status); // ativação só via webhook
        Assert.Equal("efi", subscription.PaymentProvider);
        Assert.Equal("ext-1", subscription.ExternalSubscriptionId);
        await _subscriptionRepo.Received(1).UpdateAsync(subscription);
    }

    [Fact]
    public async Task CreateAsync_WithoutSubscription_ShouldThrowKeyNotFound()
    {
        _subscriptionRepo.GetByUserIdAsync("user1").Returns((Subscription?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CreateAsync("user1", new CreateSubscriptionRequestDto()));
    }

    [Fact]
    public async Task CreateAsync_WithActiveSubscription_ShouldThrowInvalidOperation()
    {
        _subscriptionRepo.GetByUserIdAsync("user1")
            .Returns(new Subscription { UserId = "user1", Status = SubscriptionStatus.Active });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync("user1", new CreateSubscriptionRequestDto()));
    }

    [Fact]
    public async Task HandlePaymentWebhookAsync_WithInvalidPayload_ShouldIgnoreWithoutUpdating()
    {
        _paymentGateway.ValidateAndParseWebhookAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, string>>())
            .Returns(new WebhookValidationResult { IsValid = false });

        await _service.HandlePaymentWebhookAsync("{}", new Dictionary<string, string>());

        await _subscriptionRepo.DidNotReceive().UpdateAsync(Arg.Any<Subscription>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task HandlePaymentWebhookAsync_WithUnknownSubscription_ShouldIgnore()
    {
        _paymentGateway.ValidateAndParseWebhookAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, string>>())
            .Returns(new WebhookValidationResult
            {
                IsValid = true,
                EventType = WebhookEventType.PaymentConfirmed,
                ExternalSubscriptionId = "ext-unknown"
            });
        _subscriptionRepo.GetByExternalSubscriptionIdAsync("ext-unknown").Returns((Subscription?)null);

        await _service.HandlePaymentWebhookAsync("{}", new Dictionary<string, string>());

        await _subscriptionRepo.DidNotReceive().UpdateAsync(Arg.Any<Subscription>());
    }

    [Fact]
    public async Task HandlePaymentWebhookAsync_PaymentConfirmedOnTrial_ShouldActivatePremium()
    {
        var subscription = new Subscription { UserId = "user1", Status = SubscriptionStatus.Trial };
        var periodEnd = DateTime.UtcNow.AddMonths(1);
        SetupWebhook(subscription, WebhookEventType.PaymentConfirmed, periodEnd);

        await _service.HandlePaymentWebhookAsync("{}", new Dictionary<string, string>());

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(PlanType.Premium, subscription.Plan);
        Assert.Equal(periodEnd, subscription.CurrentPeriodEnd);
        await _subscriptionRepo.Received(1).UpdateAsync(subscription);
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task HandlePaymentWebhookAsync_PaymentConfirmedOnPastDue_ShouldRenewPeriod()
    {
        var subscription = new Subscription
        {
            UserId = "user1",
            Plan = PlanType.Premium,
            Status = SubscriptionStatus.PastDue,
            GraceEndsAt = DateTime.UtcNow.AddDays(2)
        };
        var newPeriodEnd = DateTime.UtcNow.AddMonths(1);
        SetupWebhook(subscription, WebhookEventType.PaymentConfirmed, newPeriodEnd);

        await _service.HandlePaymentWebhookAsync("{}", new Dictionary<string, string>());

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(newPeriodEnd, subscription.CurrentPeriodEnd);
        Assert.Null(subscription.GraceEndsAt);
    }

    [Fact]
    public async Task HandlePaymentWebhookAsync_PaymentFailed_ShouldMarkPastDueWithGrace()
    {
        var subscription = new Subscription { UserId = "user1", Status = SubscriptionStatus.Active };
        SetupWebhook(subscription, WebhookEventType.PaymentFailed, null);

        await _service.HandlePaymentWebhookAsync("{}", new Dictionary<string, string>());

        Assert.Equal(SubscriptionStatus.PastDue, subscription.Status);
        Assert.NotNull(subscription.GraceEndsAt);
        Assert.True(subscription.GraceEndsAt > DateTime.UtcNow.AddDays(4));
    }

    [Fact]
    public async Task HandlePaymentWebhookAsync_SubscriptionCancelled_ShouldCancel()
    {
        var subscription = new Subscription { UserId = "user1", Status = SubscriptionStatus.Active };
        SetupWebhook(subscription, WebhookEventType.SubscriptionCancelled, null);

        await _service.HandlePaymentWebhookAsync("{}", new Dictionary<string, string>());

        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        Assert.NotNull(subscription.CancelledAt);
    }

    [Fact]
    public async Task CancelAsync_WithExternalSubscription_ShouldCancelOnGatewayAndLocally()
    {
        var subscription = new Subscription
        {
            UserId = "user1",
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = "ext-1"
        };
        _subscriptionRepo.GetByUserIdAsync("user1").Returns(subscription);

        await _service.CancelAsync("user1");

        await _paymentGateway.Received(1).CancelSubscriptionAsync("ext-1");
        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        await _subscriptionRepo.Received(1).UpdateAsync(subscription);
    }

    [Fact]
    public async Task CancelAsync_WithoutExternalSubscription_ShouldNotCallGateway()
    {
        var subscription = new Subscription { UserId = "user1", Status = SubscriptionStatus.Trial };
        _subscriptionRepo.GetByUserIdAsync("user1").Returns(subscription);

        await _service.CancelAsync("user1");

        await _paymentGateway.DidNotReceive().CancelSubscriptionAsync(Arg.Any<string>());
        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
    }

    [Fact]
    public async Task CancelAsync_WithoutSubscription_ShouldThrowKeyNotFound()
    {
        _subscriptionRepo.GetByUserIdAsync("user1").Returns((Subscription?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CancelAsync("user1"));
    }

    [Fact]
    public async Task GetByUserIdAsync_WithSubscription_ShouldReturnDto()
    {
        _subscriptionRepo.GetByUserIdAsync("user1").Returns(new Subscription
        {
            UserId = "user1",
            Plan = PlanType.Premium,
            Status = SubscriptionStatus.Active
        });

        var result = await _service.GetByUserIdAsync("user1");

        Assert.Equal("Premium", result.Plan);
        Assert.Equal("Active", result.Status);
        Assert.True(result.IsPremiumActive);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithoutSubscription_ShouldThrowKeyNotFound()
    {
        _subscriptionRepo.GetByUserIdAsync("user1").Returns((Subscription?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByUserIdAsync("user1"));
    }

    [Fact]
    public async Task EnsurePremiumAccessAsync_WithoutSubscription_ShouldThrowPremiumRequired()
    {
        _subscriptionRepo.GetByUserIdAsync("user1").Returns((Subscription?)null);

        await Assert.ThrowsAsync<PremiumRequiredException>(() => _service.EnsurePremiumAccessAsync("user1"));
    }

    [Fact]
    public async Task EnsurePremiumAccessAsync_WithInactivePremium_ShouldThrowPremiumRequired()
    {
        _subscriptionRepo.GetByUserIdAsync("user1")
            .Returns(new Subscription { UserId = "user1", Status = SubscriptionStatus.Expired });

        await Assert.ThrowsAsync<PremiumRequiredException>(() => _service.EnsurePremiumAccessAsync("user1"));
    }

    [Fact]
    public async Task EnsurePremiumAccessAsync_WithActivePremium_ShouldNotThrow()
    {
        _subscriptionRepo.GetByUserIdAsync("user1")
            .Returns(new Subscription { UserId = "user1", Status = SubscriptionStatus.Active });

        await _service.EnsurePremiumAccessAsync("user1");
    }

    [Fact]
    public async Task GetAllForAdminAsync_ShouldListAllUsers_EvenWithoutSubscription()
    {
        var users = new List<User>
        {
            new() { Id = "u1", Name = "Com Assinatura", Email = "a@a.com" },
            new() { Id = "u2", Name = "Sem Assinatura", Email = "b@b.com" }
        };
        _userRepo.GetPagedAsync(0, 10).Returns(users);
        _subscriptionRepo.GetByUserIdAsync("u1")
            .Returns(new Subscription { UserId = "u1", Plan = PlanType.Premium, Status = SubscriptionStatus.Active });
        _subscriptionRepo.GetByUserIdAsync("u2").Returns((Subscription?)null);

        var result = await _service.GetAllForAdminAsync(1, 10);

        Assert.Equal(2, result.Count);

        Assert.Equal("u1", result[0].UserId);
        Assert.Equal("Premium", result[0].Plan);
        Assert.Equal("Active", result[0].Status);
        Assert.True(result[0].IsPremiumActive);

        // Usuário sem documento de assinatura (cadastro anterior ao premium) aparece como Free/Expired.
        Assert.Equal("u2", result[1].UserId);
        Assert.Equal("Free", result[1].Plan);
        Assert.Equal("Expired", result[1].Status);
        Assert.False(result[1].IsPremiumActive);
        Assert.Null(result[1].PaymentProvider);
    }

    [Fact]
    public async Task GetAllForAdminAsync_ShouldApplyPagination()
    {
        _userRepo.GetPagedAsync(20, 10).Returns(new List<User>());

        await _service.GetAllForAdminAsync(3, 10);

        await _userRepo.Received(1).GetPagedAsync(20, 10);
    }

    [Fact]
    public async Task ActivatePremiumManuallyAsync_WithoutSubscription_ShouldCreateNewActive()
    {
        _subscriptionRepo.GetByUserIdAsync("user1").Returns((Subscription?)null);
        _subscriptionRepo.AddAsync(Arg.Any<Subscription>()).Returns(x => x.Arg<Subscription>());
        _userRepo.GetByIdAsync("user1").Returns(new User { Id = "user1", Name = "Luan", Email = "l@l.com" });

        var result = await _service.ActivatePremiumManuallyAsync("user1", 30, "admin1");

        Assert.Equal("Premium", result.Plan);
        Assert.Equal("Active", result.Status);
        Assert.True(result.IsPremiumActive);
        Assert.Equal("manual", result.PaymentProvider);
        await _subscriptionRepo.Received(1).AddAsync(Arg.Any<Subscription>());
        await _subscriptionRepo.DidNotReceive().UpdateAsync(Arg.Any<Subscription>());
    }

    [Fact]
    public async Task ActivatePremiumManuallyAsync_WithExistingSubscription_ShouldUpdate()
    {
        var subscription = new Subscription { UserId = "user1", Status = SubscriptionStatus.Expired };
        _subscriptionRepo.GetByUserIdAsync("user1").Returns(subscription);
        _userRepo.GetByIdAsync("user1").Returns(new User { Id = "user1" });

        var result = await _service.ActivatePremiumManuallyAsync("user1", 30, "admin1");

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal("admin1", subscription.PaymentMetadata?["activatedByAdminId"]);
        Assert.True(result.IsPremiumActive);
        await _subscriptionRepo.Received(1).UpdateAsync(subscription);
        await _subscriptionRepo.DidNotReceive().AddAsync(Arg.Any<Subscription>());
    }

    [Fact]
    public async Task ActivatePremiumManuallyAsync_WithMissingUser_ShouldThrowKeyNotFound()
    {
        _subscriptionRepo.GetByUserIdAsync("user1").Returns((Subscription?)null);
        _subscriptionRepo.AddAsync(Arg.Any<Subscription>()).Returns(x => x.Arg<Subscription>());
        _userRepo.GetByIdAsync("user1").Returns((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.ActivatePremiumManuallyAsync("user1", 30, "admin1"));
    }

    [Fact]
    public async Task RevokePremiumManuallyAsync_ShouldExpireSubscriptionImmediately()
    {
        var subscription = new Subscription
        {
            UserId = "user1",
            Plan = PlanType.Premium,
            Status = SubscriptionStatus.Active,
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(20)
        };
        _subscriptionRepo.GetByUserIdAsync("user1").Returns(subscription);
        _userRepo.GetByIdAsync("user1").Returns(new User { Id = "user1" });

        var result = await _service.RevokePremiumManuallyAsync("user1");

        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
        Assert.Equal(PlanType.Free, subscription.Plan);
        Assert.False(result.IsPremiumActive);
        await _subscriptionRepo.Received(1).UpdateAsync(subscription);
    }

    [Fact]
    public async Task RevokePremiumManuallyAsync_WithoutSubscription_ShouldThrowKeyNotFound()
    {
        _subscriptionRepo.GetByUserIdAsync("user1").Returns((Subscription?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.RevokePremiumManuallyAsync("user1"));
    }

    private void SetupWebhook(Subscription subscription, WebhookEventType eventType, DateTime? periodEnd)
    {
        _paymentGateway.ValidateAndParseWebhookAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, string>>())
            .Returns(new WebhookValidationResult
            {
                IsValid = true,
                EventType = eventType,
                ExternalSubscriptionId = "ext-1",
                PeriodStart = periodEnd.HasValue ? DateTime.UtcNow : null,
                PeriodEnd = periodEnd
            });
        _subscriptionRepo.GetByExternalSubscriptionIdAsync("ext-1").Returns(subscription);
    }
}
