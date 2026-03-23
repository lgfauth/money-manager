namespace MoneyManager.Application.DTOs.Request;

public class PushSubscribeRequestDto
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
}

public class PushUnsubscribeRequestDto
{
    public string Endpoint { get; set; } = string.Empty;
}

public class PushSendTestRequestDto
{
    // Empty body — the test payload is defined in the service
}
