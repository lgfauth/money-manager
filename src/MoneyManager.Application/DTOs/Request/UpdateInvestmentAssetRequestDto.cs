using System.ComponentModel.DataAnnotations;

namespace MoneyManager.Application.DTOs.Request;

/// <summary>
/// DTO para atualização de informações de um ativo de investimento.
/// </summary>
public class UpdateInvestmentAssetRequestDto
{
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
    /// Observações ou notas sobre o ativo (opcional).
    /// </summary>
    [StringLength(1000, ErrorMessage = "As notas devem ter no máximo 1000 caracteres.")]
    public string? Notes { get; set; }
}
