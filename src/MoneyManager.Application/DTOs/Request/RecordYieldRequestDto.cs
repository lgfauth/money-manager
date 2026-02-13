using System.ComponentModel.DataAnnotations;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Request;

/// <summary>
/// DTO para registrar um rendimento (dividendo, juros, aluguel) de um ativo.
/// </summary>
public class RecordYieldRequestDto
{
    /// <summary>
    /// ID do ativo que gerou o rendimento.
    /// </summary>
    [Required(ErrorMessage = "O ativo é obrigatório.")]
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Valor líquido do rendimento recebido.
    /// </summary>
    [Required(ErrorMessage = "O valor é obrigatório.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Tipo de rendimento (Dividendo, Juros, Rendimento Geral).
    /// </summary>
    [Required(ErrorMessage = "O tipo de rendimento é obrigatório.")]
    public InvestmentTransactionType YieldType { get; set; } = InvestmentTransactionType.YieldPayment;

    /// <summary>
    /// Data do recebimento do rendimento.
    /// </summary>
    [Required(ErrorMessage = "A data é obrigatória.")]
    public DateTime Date { get; set; } = DateTime.Today;

    /// <summary>
    /// Descrição ou observação sobre o rendimento (opcional).
    /// </summary>
    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string? Description { get; set; }
}
