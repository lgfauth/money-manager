using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;

namespace MoneyManager.Web.Services;

/// <summary>
/// Interface do serviço de comunicação HTTP para ativos de investimento.
/// </summary>
public interface IInvestmentAssetService
{
    /// <summary>
    /// Obtém todos os ativos de investimento do usuário.
    /// </summary>
    Task<IEnumerable<InvestmentAssetResponseDto>> GetAllAsync();

    /// <summary>
    /// Obtém um ativo específico por ID.
    /// </summary>
    Task<InvestmentAssetResponseDto?> GetByIdAsync(string id);

    /// <summary>
    /// Cria um novo ativo de investimento.
    /// </summary>
    Task<InvestmentAssetResponseDto> CreateAsync(CreateInvestmentAssetRequestDto request);

    /// <summary>
    /// Atualiza informações de um ativo.
    /// </summary>
    Task UpdateAsync(string id, UpdateInvestmentAssetRequestDto request);

    /// <summary>
    /// Deleta um ativo (soft delete).
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Registra uma compra de ativo.
    /// </summary>
    Task<InvestmentAssetResponseDto> BuyAsync(string id, BuyAssetRequestDto request);

    /// <summary>
    /// Registra uma venda de ativo.
    /// </summary>
    Task<InvestmentAssetResponseDto> SellAsync(string id, SellAssetRequestDto request);

    /// <summary>
    /// Ajusta o preço de mercado de um ativo.
    /// </summary>
    Task<InvestmentAssetResponseDto> AdjustPriceAsync(string id, AdjustPriceRequestDto request);

    /// <summary>
    /// Obtém resumo consolidado de investimentos.
    /// </summary>
    Task<InvestmentSummaryResponseDto> GetSummaryAsync();

    /// <summary>
    /// Atualiza preços de mercado de todos os ativos com ticker (atualização manual).
    /// </summary>
    Task<PriceUpdateResponseDto?> UpdatePricesAsync();
}

/// <summary>
/// DTO de resposta da atualização de preços.
/// </summary>
public class PriceUpdateResponseDto
{
    public string Message { get; set; } = string.Empty;
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Total { get; set; }
}

