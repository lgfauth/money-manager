using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Interfaces;
using WebPush;
using DomainPushSubscription = MoneyManager.Domain.Entities.PushSubscription;

namespace MoneyManager.Application.Services;

public interface IPushService
{
    /// <summary>Saves or updates a push subscription for the authenticated user.</summary>
    Task<PushSubscriptionResponseDto> SubscribeAsync(string userId, PushSubscribeRequestDto request);

    /// <summary>Removes (soft-deletes) the subscription matching the given endpoint.</summary>
    Task UnsubscribeAsync(string userId, string endpoint);

    /// <summary>Sends a push notification to all active subscriptions of a user.</summary>
    Task SendToUserAsync(string userId, PushNotificationPayload payload);

    /// <summary>Sends a test notification to all subscriptions of a user.</summary>
    Task SendTestAsync(string userId);
}

public class PushService : IPushService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly VapidSettings _vapid;
    private readonly ILogger<PushService> _logger;

    public PushService(
        IUnitOfWork unitOfWork,
        IOptions<VapidSettings> vapidOptions,
        ILogger<PushService> logger)
    {
        _unitOfWork = unitOfWork;
        _vapid = vapidOptions.Value;
        _logger = logger;
    }

    public async Task<PushSubscriptionResponseDto> SubscribeAsync(string userId, PushSubscribeRequestDto request)
    {
        _logger.LogInformation("Saving push subscription for user {UserId}", userId);

        var existing = await _unitOfWork.PushSubscriptions.GetByEndpointAsync(request.Endpoint);

        if (existing != null)
        {
            // Update keys in case the browser rotated them
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
            existing.UserId = userId;
            existing.UserAgent = request.UserAgent;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.IsDeleted = false;
            await _unitOfWork.PushSubscriptions.UpdateAsync(existing);

            _logger.LogInformation("Updated existing push subscription {Id} for user {UserId}", existing.Id, userId);
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

        _logger.LogInformation("Created push subscription {Id} for user {UserId}", subscription.Id, userId);
        return MapToDto(subscription);
    }

    public async Task UnsubscribeAsync(string userId, string endpoint)
    {
        _logger.LogInformation("Removing push subscription for user {UserId}", userId);

        var subscription = await _unitOfWork.PushSubscriptions.GetByEndpointAsync(endpoint);
        if (subscription == null || subscription.UserId != userId)
            throw new KeyNotFoundException("Subscription not found");

        subscription.IsDeleted = true;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.PushSubscriptions.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Removed push subscription {Id} for user {UserId}", subscription.Id, userId);
    }

    public async Task SendToUserAsync(string userId, PushNotificationPayload payload)
    {
        var subscriptions = await _unitOfWork.PushSubscriptions.GetByUserIdAsync(userId);

        _logger.LogInformation("Found {Count} subscriptions for user {UserId}", subscriptions.Count(), userId);

        await SendToSubscriptionsAsync(subscriptions, payload);
    }

    public async Task SendTestAsync(string userId)
    {
        var payload = new PushNotificationPayload
        {
            Title = "MoneyManager",
            Body = "Notificações push estão funcionando! ??",
            Icon = "/favicon.svg"
        };

        await SendToUserAsync(userId, payload);
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

                _logger.LogInformation("Push notification sent to subscription {Id}", sub.Id);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                _logger.LogWarning("Subscription {Id} is expired (410 Gone). Soft-deleting.", sub.Id);
                sub.IsDeleted = true;
                sub.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.PushSubscriptions.UpdateAsync(sub);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push notification to subscription {Id}", sub.Id);
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

/// <summary>
/// Strongly-typed VAPID configuration bound from appsettings "Vapid" section.
/// </summary>
public sealed class VapidSettings
{
    public const string SectionName = "Vapid";

    /// <summary>mailto: or https: URL that identifies the application owner.</summary>
    public string Subject { get; set; } = "mailto:admin@moneymanager.app";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}

