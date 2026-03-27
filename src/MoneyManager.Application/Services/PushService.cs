using Microsoft.Extensions.Options;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;
using WebPush;
using DomainPushSubscription = MoneyManager.Domain.Entities.PushSubscription;

namespace MoneyManager.Application.Services;

public interface IPushService
{
    Task<PushSubscriptionResponseDto> SubscribeAsync(string userId, PushSubscribeRequestDto request);
    Task UnsubscribeAsync(string userId, string endpoint);
    Task SendToUserAsync(string userId, PushNotificationPayload payload);
    Task SendTestAsync(string userId);
    Task<bool> HasActiveSubscriptionAsync(string userId);
}

public class PushService : IPushService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly VapidSettings _vapid;
    private readonly IProcessLogger _processLogger;

    public PushService(
        IUnitOfWork unitOfWork,
        IOptions<VapidSettings> vapidOptions,
        IProcessLogger processLogger)
    {
        _unitOfWork = unitOfWork;
        _vapid = vapidOptions.Value;
        _processLogger = processLogger;
    }

    public async Task<PushSubscriptionResponseDto> SubscribeAsync(string userId, PushSubscribeRequestDto request)
    {
        _processLogger.AddStep("Saving push subscription", new Dictionary<string, object?>
        {
            ["userId"] = userId
        });

        var existing = await _unitOfWork.PushSubscriptions.GetByEndpointAsync(request.Endpoint);

        if (existing != null)
        {
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
            existing.UserId = userId;
            existing.UserAgent = request.UserAgent;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.IsDeleted = false;
            await _unitOfWork.PushSubscriptions.UpdateAsync(existing);

            _processLogger.AddStep("Updated existing push subscription", new Dictionary<string, object?>
            {
                ["subscriptionId"] = existing.Id
            });
            return MapToDto(existing);
        }

        var subscription = new DomainPushSubscription
        {
            UserId = userId,
            Endpoint = request.Endpoint,
            P256dh = request.P256dh,
            Auth = request.Auth,
            UserAgent = request.UserAgent
        };

        await _unitOfWork.PushSubscriptions.AddAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Created push subscription", new Dictionary<string, object?>
        {
            ["subscriptionId"] = subscription.Id
        });
        return MapToDto(subscription);
    }

    public async Task UnsubscribeAsync(string userId, string endpoint)
    {
        _processLogger.AddStep("Removing push subscription");

        var subscription = await _unitOfWork.PushSubscriptions.GetByEndpointAsync(endpoint);
        if (subscription == null || subscription.UserId != userId)
            throw new KeyNotFoundException("Subscription not found");

        subscription.IsDeleted = true;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.PushSubscriptions.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Push subscription removed", new Dictionary<string, object?>
        {
            ["subscriptionId"] = subscription.Id
        });
    }

    public async Task SendToUserAsync(string userId, PushNotificationPayload payload)
    {
        var subscriptions = await _unitOfWork.PushSubscriptions.GetByUserIdAsync(userId);
        var subsList = subscriptions.ToList();

        _processLogger.AddStep("Sending push notifications", new Dictionary<string, object?>
        {
            ["userId"] = userId,
            ["subscriptionCount"] = subsList.Count
        });

        await SendToSubscriptionsAsync(subsList, payload);
    }

    public async Task SendTestAsync(string userId)
    {
        var payload = new PushNotificationPayload
        {
            Title = "MoneyManager",
            Body = "Notificações push estão funcionando!",
            Icon = "/favicon.svg"
        };

        await SendToUserAsync(userId, payload);
    }

    public async Task<bool> HasActiveSubscriptionAsync(string userId)
    {
        var subs = await _unitOfWork.PushSubscriptions.GetByUserIdAsync(userId);
        return subs.Any();
    }

    private async Task SendToSubscriptionsAsync(
        IEnumerable<DomainPushSubscription> subscriptions,
        PushNotificationPayload payload)
    {
        var client = new WebPushClient();
        var vapidDetails = new VapidDetails(
            _vapid.Subject,
            _vapid.PublicKey,
            _vapid.PrivateKey
        );

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSub, payloadJson, vapidDetails);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                _processLogger.AddWarning("Subscription expired (410 Gone), soft-deleting", new Dictionary<string, object?>
                {
                    ["subscriptionId"] = sub.Id
                });
                sub.IsDeleted = true;
                sub.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.PushSubscriptions.UpdateAsync(sub);
            }
            catch (Exception ex)
            {
                _processLogger.AddError($"Failed to send push to subscription {sub.Id}", ex);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private static PushSubscriptionResponseDto MapToDto(DomainPushSubscription sub) => new()
    {
        Id = sub.Id,
        UserId = sub.UserId,
        Endpoint = sub.Endpoint,
        CreatedAt = sub.CreatedAt
    };
}

public sealed class VapidSettings
{
    public const string SectionName = "Vapid";
    public string Subject { get; set; } = "mailto:admin@moneymanager.app";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}
