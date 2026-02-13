using System.ComponentModel.DataAnnotations;

namespace MoneyManager.Application.DTOs.Request;

/// <summary>
/// DTO para ajustar o preço de mercado de um ativo.
/// </summary>
public class AdjustPriceRequestDto
{
    /// <summary>
    /// Novo preço de mercado por unidade.
    /// </summary>
    [Required(ErrorMessage = "O novo preço é obrigatório.")]
    [Range(0, double.MaxValue, ErrorMessage = "O preço não pode ser negativo.")]
    public decimal NewPrice { get; set; }

    /// <summary>
    /// Data de referência para o ajuste de preço.
    /// </summary>
    [Required(ErrorMessage = "A data é obrigatória.")]
    public DateTime Date { get; set; } = DateTime.Today;
}
