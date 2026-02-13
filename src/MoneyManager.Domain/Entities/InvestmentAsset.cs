using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Entities;

/// <summary>
/// Representa um ativo de investimento (ação, FII, renda fixa, cripto, etc.)
/// </summary>
public class InvestmentAsset
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// ID do usuário proprietário do ativo.
    /// </summary>
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// ID da conta de investimento onde o ativo está registrado.
    /// </summary>
    [BsonElement("accountId")]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do ativo (Ações, Renda Fixa, FII, Cripto, etc.)
    /// </summary>
    [BsonElement("assetType")]
    public InvestmentAssetType AssetType { get; set; }

    /// <summary>
    /// Nome do ativo (ex: "Petrobras PN", "Tesouro Selic 2025", "HGLG11").
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Código/ticker do ativo (ex: "PETR4", "HGLG11", "BTC").
    /// </summary>
    [BsonElement("ticker")]
    public string? Ticker { get; set; }

    /// <summary>
    /// Quantidade de unidades do ativo que o usuário possui.
    /// </summary>
    [BsonElement("quantity")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Preço médio de compra ponderado (custo médio por unidade).
    /// </summary>
    [BsonElement("averagePurchasePrice")]
    public decimal AveragePurchasePrice { get; set; }

    /// <summary>
    /// Preço atual de mercado por unidade.
    /// </summary>
    [BsonElement("currentPrice")]
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Valor total investido (inclui taxas e corretagens).
    /// </summary>
    [BsonElement("totalInvested")]
    public decimal TotalInvested { get; set; }

    /// <summary>
    /// Valor atual do ativo (Quantidade × Preço Atual).
    /// </summary>
    [BsonElement("currentValue")]
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Lucro ou prejuízo absoluto (Valor Atual - Total Investido).
    /// </summary>
    [BsonElement("profitLoss")]
    public decimal ProfitLoss { get; set; }

    /// <summary>
    /// Percentual de lucro ou prejuízo.
    /// </summary>
    [BsonElement("profitLossPercentage")]
    public decimal ProfitLossPercentage { get; set; }

    /// <summary>
    /// Data/hora da última atualização de preço de mercado.
    /// </summary>
    [BsonElement("lastPriceUpdate")]
    public DateTime? LastPriceUpdate { get; set; }

    /// <summary>
    /// Observações ou notas sobre o ativo.
    /// </summary>
    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Calcula o valor atual do ativo (Quantidade × Preço Atual).
    /// </summary>
    public void CalculateCurrentValue()
    {
        CurrentValue = Quantity * CurrentPrice;
    }

    /// <summary>
    /// Calcula o lucro/prejuízo absoluto e percentual.
    /// </summary>
    public void CalculateProfitLoss()
    {
        CalculateCurrentValue();
        ProfitLoss = CurrentValue - TotalInvested;

        if (TotalInvested > 0)
        {
            ProfitLossPercentage = (ProfitLoss / TotalInvested) * 100;
        }
        else
        {
            ProfitLossPercentage = 0;
        }
    }

    /// <summary>
    /// Atualiza o preço médio ponderado após uma compra.
    /// Fórmula: NovoPreçoMédio = (TotalInvestido + NovaCompra) / (QuantidadeTotal + NovaQuantidade)
    /// </summary>
    /// <param name="quantity">Quantidade comprada</param>
    /// <param name="price">Preço unitário da compra</param>
    /// <param name="fees">Taxas e corretagens</param>
    public void UpdateAveragePriceOnBuy(decimal quantity, decimal price, decimal fees)
    {
        var purchaseValue = (quantity * price) + fees;
        TotalInvested += purchaseValue;
        Quantity += quantity;

        if (Quantity > 0)
        {
            AveragePurchasePrice = TotalInvested / Quantity;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza o total investido e quantidade após uma venda.
    /// O preço médio permanece o mesmo, reduz-se proporcionalmente o total investido.
    /// </summary>
    /// <param name="quantity">Quantidade vendida</param>
    public void UpdateAveragePriceOnSell(decimal quantity)
    {
        if (quantity > Quantity)
        {
            throw new InvalidOperationException($"Quantidade a vender ({quantity}) é maior que a quantidade disponível ({Quantity}).");
        }

        // Reduz o total investido proporcionalmente
        var proportionSold = quantity / Quantity;
        TotalInvested -= TotalInvested * proportionSold;
        Quantity -= quantity;

        // Preço médio permanece o mesmo, apenas recalcula se necessário
        if (Quantity > 0)
        {
            AveragePurchasePrice = TotalInvested / Quantity;
        }
        else
        {
            AveragePurchasePrice = 0;
            TotalInvested = 0;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza o preço de mercado e recalcula valores derivados.
    /// </summary>
    /// <param name="newPrice">Novo preço de mercado</param>
    public void UpdateMarketPrice(decimal newPrice)
    {
        if (newPrice < 0)
        {
            throw new ArgumentException("O preço de mercado não pode ser negativo.", nameof(newPrice));
        }

        CurrentPrice = newPrice;
        LastPriceUpdate = DateTime.UtcNow;
        CalculateProfitLoss();
        UpdatedAt = DateTime.UtcNow;
    }
}
