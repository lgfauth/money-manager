namespace MoneyManager.Application.Services;

public interface IPaymentGateway
{
    string ProviderName { get; }

    Task<CreateSubscriptionGatewayResult> CreateSubscriptionAsync(CreateSubscriptionGatewayRequest request);
    Task CancelSubscriptionAsync(string externalSubscriptionId);
    Task<WebhookValidationResult> ValidateAndParseWebhookAsync(string rawPayload, IDictionary<string, string> headers);
}

public class CreateSubscriptionGatewayRequest
{
    public string UserId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerCpf { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
}

public class CreateSubscriptionGatewayResult
{
    public string ExternalSubscriptionId { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
}

public class WebhookValidationResult
{
    public bool IsValid { get; set; }
    public WebhookEventType EventType { get; set; }
    public string ExternalSubscriptionId { get; set; } = string.Empty;
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}

public enum WebhookEventType { PaymentConfirmed, PaymentFailed, SubscriptionCancelled, Unknown }
