namespace MoneyManager.Application.DTOs.Response;

public class SubscriptionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool IsPremiumActive { get; set; }
}
