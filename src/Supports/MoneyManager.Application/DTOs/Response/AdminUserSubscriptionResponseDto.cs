namespace MoneyManager.Application.DTOs.Response;

public class AdminUserSubscriptionResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPremiumActive { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public string? PaymentProvider { get; set; }
    public DateTime UserCreatedAt { get; set; }
}
