namespace MoneyManager.Application.DTOs.Request;

public class CreateSubscriptionRequestDto
{
    public string PayerName { get; set; } = string.Empty;
    public string PayerCpf { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
}
