namespace MoneyManager.Application.DTOs.Response;

public class UnlinkAccountResponseDto
{
    public string AccountId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool HasDeactivatedRecurrences { get; set; }
}

public class DisconnectBankResponseDto
{
    public string ItemId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int UnlinkedAccountsCount { get; set; }
}
