using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Entities;

/// <summary>
/// Representa uma transação relacionada a um ativo de investimento
/// (compra, venda, rendimento, ajuste, etc.)
/// </summary>
public class InvestmentTransaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// ID do usuário proprietário da transação.
    /// </summary>
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// ID do ativo de investimento relacionado.
    /// </summary>
    [BsonElement("assetId")]
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// ID da conta onde a transação foi registrada.
    /// </summary>
    [BsonElement("accountId")]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo da transação (Compra, Venda, Dividendo, Juros, etc.)
    /// </summary>
    [BsonElement("transactionType")]
    public InvestmentTransactionType TransactionType { get; set; }

    /// <summary>
    /// Quantidade de unidades envolvidas na transação.
    /// </summary>
    [BsonElement("quantity")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Preço unitário no momento da transação.
    /// </summary>
    [BsonElement("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Valor total da transação (Quantidade × Preço ± Taxas).
    /// </summary>
    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Taxas ou corretagens pagas na transação.
    /// </summary>
    [BsonElement("fees")]
    public decimal Fees { get; set; }

    /// <summary>
    /// Data da transação.
    /// </summary>
    [BsonElement("date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// Descrição ou observação sobre a transação.
    /// </summary>
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ID da transação regular criada no sistema de transações.
    /// </summary>
    [BsonElement("linkedTransactionId")]
    public string? LinkedTransactionId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Calcula o valor total da transação baseado em quantidade, preço e taxas.
    /// </summary>
    public void CalculateTotalAmount()
    {
        TotalAmount = TransactionType switch
        {
            InvestmentTransactionType.Buy => (Quantity * Price) + Fees,
            InvestmentTransactionType.Sell => (Quantity * Price) - Fees,
            InvestmentTransactionType.Dividend or 
            InvestmentTransactionType.Interest or 
            InvestmentTransactionType.YieldPayment => Price, // Para rendimentos, Price é o valor recebido
            InvestmentTransactionType.Fee => Fees,
            InvestmentTransactionType.MarketAdjustment => Quantity * Price,
            _ => 0
        };
    }

    /// <summary>
    /// Calcula o lucro ou prejuízo de uma venda.
    /// </summary>
    /// <param name="averagePurchasePrice">Preço médio de compra do ativo</param>
    /// <returns>Lucro (positivo) ou prejuízo (negativo)</returns>
    public decimal CalculateSaleProfitLoss(decimal averagePurchasePrice)
    {
        if (TransactionType != InvestmentTransactionType.Sell)
        {
            throw new InvalidOperationException("Cálculo de lucro/prejuízo só é aplicável a vendas.");
        }

        var saleValue = (Quantity * Price) - Fees;
        var costBasis = Quantity * averagePurchasePrice;
        return saleValue - costBasis;
    }
}
