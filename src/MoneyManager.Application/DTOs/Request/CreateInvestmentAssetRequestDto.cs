using System.ComponentModel.DataAnnotations;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Request;

/// <summary>
/// DTO para criação de um novo ativo de investimento.
/// </summary>
public class CreateInvestmentAssetRequestDto
{
    /// <summary>
    /// ID da conta de investimento onde o ativo será registrado.
    /// </summary>
    [Required(ErrorMessage = "A conta é obrigatória.")]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do ativo (Ações, Renda Fixa, FII, Cripto, etc.)
    /// </summary>
    [Required(ErrorMessage = "O tipo do ativo é obrigatório.")]
    public InvestmentAssetType AssetType { get; set; }

    /// <summary>
    /// Nome do ativo.
    /// </summary>
    [Required(ErrorMessage = "O nome do ativo é obrigatório.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Código/ticker do ativo (opcional).
    /// </summary>
    [StringLength(20, ErrorMessage = "O ticker deve ter no máximo 20 caracteres.")]
    public string? Ticker { get; set; }

    /// <summary>
    /// Quantidade inicial de unidades do ativo.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "A quantidade inicial deve ser maior ou igual a zero.")]
    public decimal InitialQuantity { get; set; }

    /// <summary>
    /// Preço inicial de compra por unidade.
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "O preço inicial deve ser maior que zero.")]
    public decimal InitialPrice { get; set; }

    /// <summary>
    /// Taxas ou corretagens pagas na compra inicial (opcional).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "As taxas não podem ser negativas.")]
    public decimal InitialFees { get; set; } = 0;

    /// <summary>
    /// Observações ou notas sobre o ativo (opcional).
    /// </summary>
    [StringLength(1000, ErrorMessage = "As notas devem ter no máximo 1000 caracteres.")]
    public string? Notes { get; set; }
}
