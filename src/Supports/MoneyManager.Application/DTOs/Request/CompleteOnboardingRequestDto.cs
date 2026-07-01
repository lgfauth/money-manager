using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Request;

public class CompleteOnboardingRequestDto
{
    // Accounts selecionados pelo usuário + mapeamento para contas existentes.
    public List<AccountMappingDto> AccountMappings { get; set; } = [];

    // CleanSlate ou Coexistence.
    public OnboardingStrategy Strategy { get; set; }

    // Só usado em Coexistence — se null, o service calcula automaticamente.
    public DateTime? CustomCutoffDate { get; set; }
}

public class AccountMappingDto
{
    public string ExternalAccountId { get; set; } = string.Empty;
    public string ExternalAccountName { get; set; } = string.Empty;
    public string ExternalAccountType { get; set; } = string.Empty;
    public string MoneyManagerAccountId { get; set; } = string.Empty; // obrigatório — sem mapping, sem sync
}
