namespace MoneyManager.Application.DTOs.Response;

/// <summary>
/// Payload serialised to JSON and sent as the body of a Web Push notification.
/// Matches the ServiceWorker's expected shape in push-manager.js.
/// </summary>
public class PushNotificationPayload
{
    public string Title { get; set; } = "MoneyManager";
    public string Body { get; set; } = string.Empty;
    public string Icon { get; set; } = "/favicon.svg";
    public string? Url { get; set; }
}
