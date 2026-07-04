namespace MoneyManager.Application.DTOs.Response;

public class BankConnectionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ConnectedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public List<SelectedBankAccountDto> SelectedAccounts { get; set; } = [];

    // Sinaliza ao frontend que a migração de categorias Open Banking ocorreu neste link,
    // para exibir o aviso informativo na tela de sucesso.
    public bool CategoriesMigrated { get; set; }
}

public class SelectedBankAccountDto
{
    public string ExternalAccountId { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Subtype { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? MoneyManagerAccountId { get; set; }
    public string MoneyManagerEntityType { get; set; } = "Account";
    public DateTime? LastSyncAt { get; set; }
}
