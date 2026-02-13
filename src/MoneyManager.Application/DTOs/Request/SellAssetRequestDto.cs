using System.ComponentModel.DataAnnotations;

namespace MoneyManager.Application.DTOs.Request;

/// <summary>
/// DTO para registrar uma venda de ativo de investimento.
/// </summary>
public class SellAssetRequestDto
{
    /// <summary>
    /// Quantidade de unidades vendidas.
    /// </summary>
    [Required(ErrorMessage = "A quantidade é obrigatória.")]
    [Range(0.000001, double.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Preço unitário da venda.
    /// </summary>
    [Required(ErrorMessage = "O preço é obrigatório.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero.")]
    public decimal Price { get; set; }

    /// <summary>
    /// Data da venda.
    /// </summary>
    [Required(ErrorMessage = "A data da venda é obrigatória.")]
    public DateTime Date { get; set; } = DateTime.Today;

    /// <summary>
    /// Taxas ou corretagens pagas na operação.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "As taxas não podem ser negativas.")]
    public decimal Fees { get; set; } = 0;

    /// <summary>
    /// Descrição ou observação sobre a venda (opcional).
    /// </summary>
    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string? Description { get; set; }
}
