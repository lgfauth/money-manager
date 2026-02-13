using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;

namespace MoneyManager.Application.Services;

/// <summary>
/// Interface do serviço de gerenciamento de ativos de investimento.
/// </summary>
public interface IInvestmentAssetService
{
    /// <summary>
    /// Cria um novo ativo de investimento.
    /// </summary>
    Task<InvestmentAssetResponseDto> CreateAsync(string userId, CreateInvestmentAssetRequestDto request);

    /// <summary>
    /// Obtém todos os ativos de investimento de um usuário.
    /// </summary>
    Task<IEnumerable<InvestmentAssetResponseDto>> GetAllAsync(string userId);

    /// <summary>
    /// Obtém um ativo específico por ID.
    /// </summary>
    Task<InvestmentAssetResponseDto> GetByIdAsync(string userId, string assetId);

    /// <summary>
    /// Atualiza informações de um ativo.
    /// </summary>
    Task<InvestmentAssetResponseDto> UpdateAsync(string userId, string assetId, UpdateInvestmentAssetRequestDto request);

    /// <summary>
    /// Deleta um ativo (soft delete).
    /// </summary>
    Task DeleteAsync(string userId, string assetId);

    /// <summary>
    /// Registra uma compra de ativo (aumenta quantidade e recalcula preço médio).
    /// </summary>
    Task<InvestmentAssetResponseDto> BuyAsync(string userId, string assetId, BuyAssetRequestDto request);

    /// <summary>
    /// Registra uma venda de ativo (reduz quantidade e calcula lucro/prejuízo).
    /// </summary>
    Task<InvestmentAssetResponseDto> SellAsync(string userId, string assetId, SellAssetRequestDto request);

    /// <summary>
    /// Ajusta o preço de mercado de um ativo.
    /// </summary>
    Task<InvestmentAssetResponseDto> AdjustPriceAsync(string userId, string assetId, AdjustPriceRequestDto request);

    /// <summary>
    /// Obtém resumo consolidado de todos os investimentos do usuário.
    /// </summary>
    Task<InvestmentSummaryResponseDto> GetSummaryAsync(string userId);
}
