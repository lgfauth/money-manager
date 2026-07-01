namespace MoneyManager.Application.DTOs.Response;

public class BankConnectionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public string? InstitutionLogo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ConnectedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public List<SelectedBankAccountDto> SelectedAccounts { get; set; } = [];
}

public class SelectedBankAccountDto
{
    public string ExternalAccountId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? MoneyManagerAccountId { get; set; }
    public DateTime? LastSyncAt { get; set; }
}
