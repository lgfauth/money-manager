namespace MoneyManager.Application.DTOs.Response;

public class PushSubscriptionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PushVapidPublicKeyDto
{
    public string PublicKey { get; set; } = string.Empty;
}
