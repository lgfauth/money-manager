using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Response;

/// <summary>
/// DTO de resposta com relatório de vendas para declaração de IR.
/// </summary>
public class InvestmentSalesReportDto
{
    /// <summary>
    /// Ano de referência do relatório.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// ID do usuário.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Nome do usuário.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Lista de vendas realizadas no ano.
    /// </summary>
    public List<SaleItemDto> Sales { get; set; } = new();

    /// <summary>
    /// Total vendido no ano.
    /// </summary>
    public decimal TotalSold { get; set; }

    /// <summary>
    /// Total de lucro no ano.
    /// </summary>
    public decimal TotalProfit { get; set; }

    /// <summary>
    /// Total de prejuízo no ano.
    /// </summary>
    public decimal TotalLoss { get; set; }

    /// <summary>
    /// Resultado líquido (lucro - prejuízo).
    /// </summary>
    public decimal NetResult { get; set; }

    /// <summary>
    /// IR devido estimado (15% sobre lucro para ações).
    /// </summary>
    public decimal EstimatedTaxDue { get; set; }

    /// <summary>
    /// Data de geração do relatório.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Item de venda no relatório.
/// </summary>
public class SaleItemDto
{
    /// <summary>
    /// Data da venda.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Nome do ativo vendido.
    /// </summary>
    public string AssetName { get; set; } = string.Empty;

    /// <summary>
    /// Ticker do ativo.
    /// </summary>
    public string? Ticker { get; set; }

    /// <summary>
    /// Tipo do ativo.
    /// </summary>
    public InvestmentAssetType AssetType { get; set; }

    /// <summary>
    /// Quantidade vendida.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Preço médio de compra.
    /// </summary>
    public decimal AveragePurchasePrice { get; set; }

    /// <summary>
    /// Preço de venda.
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Valor total da venda (Quantidade × Preço Venda).
    /// </summary>
    public decimal TotalSaleValue { get; set; }

    /// <summary>
    /// Custo total de aquisição (Quantidade × Preço Médio).
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Lucro ou prejuízo da operação.
    /// </summary>
    public decimal ProfitLoss { get; set; }

    /// <summary>
    /// Percentual de lucro ou prejuízo.
    /// </summary>
    public decimal ProfitLossPercentage { get; set; }

    /// <summary>
    /// Taxas e corretagem.
    /// </summary>
    public decimal Fees { get; set; }
}

/// <summary>
/// DTO de resposta com relatório de rendimentos.
/// </summary>
public class InvestmentYieldsReportDto
{
    /// <summary>
    /// Ano de referência do relatório.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// ID do usuário.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Nome do usuário.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Lista de rendimentos recebidos no ano.
    /// </summary>
    public List<YieldItemDto> Yields { get; set; } = new();

    /// <summary>
    /// Total de rendimentos recebidos.
    /// </summary>
    public decimal TotalYields { get; set; }

    /// <summary>
    /// Total de dividendos.
    /// </summary>
    public decimal TotalDividends { get; set; }

    /// <summary>
    /// Total de juros.
    /// </summary>
    public decimal TotalInterest { get; set; }

    /// <summary>
    /// Total de rendimentos de fundos imobiliários.
    /// </summary>
    public decimal TotalRealEstateYields { get; set; }

    /// <summary>
    /// Data de geração do relatório.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Item de rendimento no relatório.
/// </summary>
public class YieldItemDto
{
    /// <summary>
    /// Data do recebimento.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Nome do ativo.
    /// </summary>
    public string AssetName { get; set; } = string.Empty;

    /// <summary>
    /// Ticker do ativo.
    /// </summary>
    public string? Ticker { get; set; }

    /// <summary>
    /// Tipo do ativo.
    /// </summary>
    public InvestmentAssetType AssetType { get; set; }

    /// <summary>
    /// Tipo de rendimento (Dividendo, Juros, Aluguel).
    /// </summary>
    public InvestmentTransactionType YieldType { get; set; }

    /// <summary>
    /// Valor recebido.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Descrição do rendimento.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// DTO de resposta com extrato consolidado de investimentos.
/// </summary>
public class InvestmentConsolidatedStatementDto
{
    /// <summary>
    /// Data inicial do período.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Data final do período.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// ID do usuário.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Nome do usuário.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Lista de todas as transações no período.
    /// </summary>
    public List<ConsolidatedTransactionDto> Transactions { get; set; } = new();

    /// <summary>
    /// Total investido no período (compras).
    /// </summary>
    public decimal TotalPurchases { get; set; }

    /// <summary>
    /// Total vendido no período (vendas).
    /// </summary>
    public decimal TotalSales { get; set; }

    /// <summary>
    /// Total de rendimentos recebidos.
    /// </summary>
    public decimal TotalYields { get; set; }

    /// <summary>
    /// Total de taxas pagas.
    /// </summary>
    public decimal TotalFees { get; set; }

    /// <summary>
    /// Resultado líquido do período.
    /// </summary>
    public decimal NetResult { get; set; }

    /// <summary>
    /// Data de geração do relatório.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Transação consolidada no extrato.
/// </summary>
public class ConsolidatedTransactionDto
{
    /// <summary>
    /// Data da transação.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Nome do ativo.
    /// </summary>
    public string AssetName { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de transação.
    /// </summary>
    public InvestmentTransactionType TransactionType { get; set; }

    /// <summary>
    /// Quantidade (para compras e vendas).
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Preço unitário (para compras e vendas).
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Valor total da transação.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Taxas da operação.
    /// </summary>
    public decimal Fees { get; set; }

    /// <summary>
    /// Descrição da transação.
    /// </summary>
    public string? Description { get; set; }
}
