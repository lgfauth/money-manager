using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;

namespace MoneyManager.Application.Services;

/// <summary>
/// Interface do serviço de transações de investimento.
/// </summary>
public interface IInvestmentTransactionService
{
    /// <summary>
    /// Obtém todas as transações de investimento de um ativo.
    /// </summary>
    Task<IEnumerable<InvestmentTransactionResponseDto>> GetByAssetIdAsync(string assetId);

    /// <summary>
    /// Obtém transações de investimento de um usuário com filtros opcionais.
    /// </summary>
    Task<IEnumerable<InvestmentTransactionResponseDto>> GetByUserIdAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Registra um rendimento (dividendo, juros, aluguel) de um ativo.
    /// </summary>
    Task<InvestmentTransactionResponseDto> RecordYieldAsync(string userId, RecordYieldRequestDto request);
}
