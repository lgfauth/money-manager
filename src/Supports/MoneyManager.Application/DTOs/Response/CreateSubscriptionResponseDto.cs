namespace MoneyManager.Application.DTOs.Response;

public class CreateSubscriptionResponseDto
{
    public string PaymentUrl { get; set; } = string.Empty;
    public string ExternalSubscriptionId { get; set; } = string.Empty;
}
