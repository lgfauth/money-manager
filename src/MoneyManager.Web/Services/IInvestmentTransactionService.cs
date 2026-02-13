using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;

namespace MoneyManager.Web.Services;

/// <summary>
/// Interface do serviço de comunicação HTTP para transações de investimento.
/// </summary>
public interface IInvestmentTransactionService
{
    /// <summary>
    /// Obtém todas as transações de investimento com filtros opcionais.
    /// </summary>
    Task<IEnumerable<InvestmentTransactionResponseDto>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Obtém transações de um ativo específico.
    /// </summary>
    Task<IEnumerable<InvestmentTransactionResponseDto>> GetByAssetIdAsync(string assetId);

    /// <summary>
    /// Registra um rendimento (dividendo, juros, aluguel).
    /// </summary>
    Task<InvestmentTransactionResponseDto> RecordYieldAsync(RecordYieldRequestDto request);
}
