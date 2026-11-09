using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

/// <summary>
/// Regra única de elegibilidade para importação de fatura: cartões sincronizados pelo Open Finance
/// não aceitam importação, para que as duas funcionalidades nunca escrevam sobre os mesmos dados.
/// </summary>
public interface ICreditCardImportEligibilityService
{
    /// <summary>Indica se o cartão do usuário está vinculado a uma conexão Open Finance ativa (inclui status Error).</summary>
    Task<bool> IsSyncedByOpenFinanceAsync(string userId, string creditCardId);

    /// <summary>Cartões do usuário, não excluídos, que podem receber importação de fatura.</summary>
    Task<IReadOnlyList<CreditCard>> GetEligibleCardsAsync(string userId);
}

public class CreditCardImportEligibilityService : ICreditCardImportEligibilityService
{
    public const string SyncedCardMessage =
        "Este cartão é sincronizado pelo Open Finance; a importação de fatura não está disponível para ele.";

    private const string CreditCardEntityType = "CreditCard";

    private readonly IUnitOfWork _unitOfWork;

    public CreditCardImportEligibilityService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> IsSyncedByOpenFinanceAsync(string userId, string creditCardId)
    {
        var synced = await GetSyncedCardIdsAsync(userId);
        return synced.Contains(creditCardId);
    }

    public async Task<IReadOnlyList<CreditCard>> GetEligibleCardsAsync(string userId)
    {
        var cards = await _unitOfWork.CreditCards.GetByUserAsync(userId);
        var synced = await GetSyncedCardIdsAsync(userId);

        return cards
            .Where(c => c.UserId == userId && !c.IsDeleted && !synced.Contains(c.Id))
            .ToList();
    }

    private async Task<HashSet<string>> GetSyncedCardIdsAsync(string userId)
    {
        // O repositório já descarta conexões excluídas; Disconnected não sincroniza mais
        var connections = await _unitOfWork.BankConnections.GetByUserIdAsync(userId);

        return connections
            .Where(c => c.UserId == userId && !c.IsDeleted && c.Status != BankConnectionStatus.Disconnected)
            .SelectMany(c => c.SelectedAccounts)
            .Where(a => string.Equals(a.MoneyManagerEntityType, CreditCardEntityType, StringComparison.Ordinal)
                        && !string.IsNullOrEmpty(a.MoneyManagerAccountId))
            .Select(a => a.MoneyManagerAccountId!)
            .ToHashSet(StringComparer.Ordinal);
    }
}
