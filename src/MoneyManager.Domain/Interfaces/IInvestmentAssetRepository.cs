using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

/// <summary>
/// Repositório para operações de dados de ativos de investimento.
/// </summary>
public interface IInvestmentAssetRepository : IRepository<InvestmentAsset>
{
    /// <summary>
    /// Obtém todos os ativos de investimento de um usuário.
    /// </summary>
    Task<IEnumerable<InvestmentAsset>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Obtém todos os ativos de uma conta específica.
    /// </summary>
    Task<IEnumerable<InvestmentAsset>> GetByAccountIdAsync(string accountId);

    /// <summary>
    /// Busca um ativo pelo ticker e usuário.
    /// </summary>
    Task<InvestmentAsset?> GetByTickerAsync(string userId, string ticker);

    /// <summary>
    /// Obtém todos os ativos não deletados de um usuário.
    /// </summary>
    Task<IEnumerable<InvestmentAsset>> GetActiveByUserIdAsync(string userId);
}
