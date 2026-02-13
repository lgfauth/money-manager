using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Response;

/// <summary>
/// DTO de resposta com informações de uma transação de investimento.
/// </summary>
public class InvestmentTransactionResponseDto
{
    /// <summary>
    /// ID da transação.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID do usuário.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// ID do ativo relacionado.
    /// </summary>
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Nome do ativo.
    /// </summary>
    public string AssetName { get; set; } = string.Empty;

    /// <summary>
    /// Ticker do ativo.
    /// </summary>
    public string? AssetTicker { get; set; }

    /// <summary>
    /// ID da conta.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo da transação.
    /// </summary>
    public InvestmentTransactionType TransactionType { get; set; }

    /// <summary>
    /// Quantidade de unidades envolvidas.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Preço unitário na transação.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Valor total da transação.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Taxas pagas.
    /// </summary>
    public decimal Fees { get; set; }

    /// <summary>
    /// Data da transação.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Descrição da transação.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ID da transação regular vinculada.
    /// </summary>
    public string? LinkedTransactionId { get; set; }

    /// <summary>
    /// Data de criação do registro.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
