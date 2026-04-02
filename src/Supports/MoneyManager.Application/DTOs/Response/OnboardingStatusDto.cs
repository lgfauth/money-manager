namespace MoneyManager.Application.DTOs.Response;

public class OnboardingStatusDto
{
    public bool IsCompleted { get; set; }
    public bool HasAccounts { get; set; }
    public bool HasCategories { get; set; }
    public bool HasBudget { get; set; }
    public bool HasRecurringTransactions { get; set; }
    public int CompletionPercentage { get; set; }
    public List<string> PendingSteps { get; set; } = new();
}
