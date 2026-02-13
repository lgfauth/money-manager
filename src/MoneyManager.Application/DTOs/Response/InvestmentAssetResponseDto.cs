using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Response;

/// <summary>
/// DTO de resposta com informações completas de um ativo de investimento.
/// </summary>
public class InvestmentAssetResponseDto
{
    /// <summary>
    /// ID do ativo.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID do usuário proprietário.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// ID da conta de investimento.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do ativo.
    /// </summary>
    public InvestmentAssetType AssetType { get; set; }

    /// <summary>
    /// Nome do ativo.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Código/ticker do ativo.
    /// </summary>
    public string? Ticker { get; set; }

    /// <summary>
    /// Quantidade de unidades.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Preço médio de compra.
    /// </summary>
    public decimal AveragePurchasePrice { get; set; }

    /// <summary>
    /// Preço atual de mercado.
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Total investido (incluindo taxas).
    /// </summary>
    public decimal TotalInvested { get; set; }

    /// <summary>
    /// Valor atual do ativo (Quantidade × Preço Atual).
    /// </summary>
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Lucro ou prejuízo absoluto.
    /// </summary>
    public decimal ProfitLoss { get; set; }

    /// <summary>
    /// Percentual de lucro ou prejuízo.
    /// </summary>
    public decimal ProfitLossPercentage { get; set; }

    /// <summary>
    /// Data/hora da última atualização de preço.
    /// </summary>
    public DateTime? LastPriceUpdate { get; set; }

    /// <summary>
    /// Observações sobre o ativo.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Data de criação do registro.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Data da última atualização.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
