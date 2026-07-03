using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;
using DomainPushSubscription = MoneyManager.Domain.Entities.PushSubscription;

namespace MoneyManager.Tests.Application.Services;

public class PushServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IPushSubscriptionRepository _pushRepo;
    private readonly PushService _service;

    public PushServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _pushRepo = Substitute.For<IPushSubscriptionRepository>();
        _unitOfWorkMock.PushSubscriptions.Returns(_pushRepo);

        _service = new PushService(
            _unitOfWorkMock,
            Options.Create(new VapidSettings { PublicKey = "pub", PrivateKey = "priv" }),
            Substitute.For<IProcessLogger>());
    }

    [Fact]
    public async Task SubscribeAsync_WithNewEndpoint_ShouldCreateSubscription()
    {
        _pushRepo.GetByEndpointAsync("https://push/ep1").Returns((DomainPushSubscription?)null);
        _pushRepo.AddAsync(Arg.Any<DomainPushSubscription>()).Returns(x => x.Arg<DomainPushSubscription>());

        var result = await _service.SubscribeAsync("user1", new PushSubscribeRequestDto
        {
            Endpoint = "https://push/ep1",
            P256dh = "key",
            Auth = "auth"
        });

        Assert.Equal("user1", result.UserId);
        Assert.Equal("https://push/ep1", result.Endpoint);
        await _pushRepo.Received(1).AddAsync(Arg.Any<DomainPushSubscription>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task SubscribeAsync_WithExistingEndpoint_ShouldUpdateAndReviveSubscription()
    {
        var existing = new DomainPushSubscription
        {
            UserId = "old-user",
            Endpoint = "https://push/ep1",
            P256dh = "old",
            Auth = "old",
            IsDeleted = true
        };
        _pushRepo.GetByEndpointAsync("https://push/ep1").Returns(existing);

        var result = await _service.SubscribeAsync("user1", new PushSubscribeRequestDto
        {
            Endpoint = "https://push/ep1",
            P256dh = "new-key",
            Auth = "new-auth",
            UserAgent = "Firefox"
        });

        Assert.Equal("user1", existing.UserId);
        Assert.Equal("new-key", existing.P256dh);
        Assert.Equal("new-auth", existing.Auth);
        Assert.Equal("Firefox", existing.UserAgent);
        Assert.False(existing.IsDeleted);
        Assert.Equal(existing.Id, result.Id);
        await _pushRepo.Received(1).UpdateAsync(existing);
        await _pushRepo.DidNotReceive().AddAsync(Arg.Any<DomainPushSubscription>());
    }

    [Fact]
    public async Task UnsubscribeAsync_WithOwnSubscription_ShouldSoftDelete()
    {
        var subscription = new DomainPushSubscription { UserId = "user1", Endpoint = "https://push/ep1" };
        _pushRepo.GetByEndpointAsync("https://push/ep1").Returns(subscription);

        await _service.UnsubscribeAsync("user1", "https://push/ep1");

        Assert.True(subscription.IsDeleted);
        await _pushRepo.Received(1).UpdateAsync(subscription);
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UnsubscribeAsync_WithUnknownEndpoint_ShouldThrowKeyNotFound()
    {
        _pushRepo.GetByEndpointAsync("https://push/none").Returns((DomainPushSubscription?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UnsubscribeAsync("user1", "https://push/none"));
    }

    [Fact]
    public async Task UnsubscribeAsync_WithSubscriptionOfAnotherUser_ShouldThrowKeyNotFound()
    {
        _pushRepo.GetByEndpointAsync("https://push/ep1")
            .Returns(new DomainPushSubscription { UserId = "other-user", Endpoint = "https://push/ep1" });

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UnsubscribeAsync("user1", "https://push/ep1"));
    }

    [Fact]
    public async Task HasActiveSubscriptionAsync_WithSubscriptions_ShouldReturnTrue()
    {
        _pushRepo.GetByUserIdAsync("user1")
            .Returns(new List<DomainPushSubscription> { new() { UserId = "user1" } });

        Assert.True(await _service.HasActiveSubscriptionAsync("user1"));
    }

    [Fact]
    public async Task HasActiveSubscriptionAsync_WithoutSubscriptions_ShouldReturnFalse()
    {
        _pushRepo.GetByUserIdAsync("user1").Returns(new List<DomainPushSubscription>());

        Assert.False(await _service.HasActiveSubscriptionAsync("user1"));
    }

    [Fact]
    public async Task SendToUserAsync_WithoutSubscriptions_ShouldNotFail()
    {
        _pushRepo.GetByUserIdAsync("user1").Returns(new List<DomainPushSubscription>());

        await _service.SendToUserAsync("user1", new MoneyManager.Application.DTOs.Response.PushNotificationPayload
        {
            Title = "t",
            Body = "b"
        });

        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }
}
