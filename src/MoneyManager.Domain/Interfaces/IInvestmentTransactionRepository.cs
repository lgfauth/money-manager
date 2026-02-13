using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

/// <summary>
/// Repositório para operações de dados de transações de investimento.
/// </summary>
public interface IInvestmentTransactionRepository : IRepository<InvestmentTransaction>
{
    /// <summary>
    /// Obtém todas as transações de um ativo específico.
    /// </summary>
    Task<IEnumerable<InvestmentTransaction>> GetByAssetIdAsync(string assetId);

    /// <summary>
    /// Obtém transações de um usuário em um período específico.
    /// </summary>
    Task<IEnumerable<InvestmentTransaction>> GetByUserIdAsync(string userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Obtém todas as transações de um usuário.
    /// </summary>
    Task<IEnumerable<InvestmentTransaction>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Obtém transações de uma conta específica.
    /// </summary>
    Task<IEnumerable<InvestmentTransaction>> GetByAccountIdAsync(string accountId);
}
