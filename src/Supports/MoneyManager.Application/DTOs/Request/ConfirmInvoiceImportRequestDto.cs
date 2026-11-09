namespace MoneyManager.Application.DTOs.Request;

/// <summary>
/// Confirmação da importação: titulares escolhidos e os itens a cadastrar, já editados pelo usuário.
/// Itens fora da lista não são cadastrados.
/// </summary>
public class ConfirmInvoiceImportRequestDto
{
    public List<string> SelectedHolders { get; set; } = [];
    public List<ConfirmInvoiceImportItemRequestDto> Items { get; set; } = [];
}

public class ConfirmInvoiceImportItemRequestDto
{
    /// <summary>Id do item extraído (<c>InvoiceImportItem.Id</c>).</summary>
    public string Id { get; set; } = string.Empty;

    public DateTime? Date { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Valor da parcela (positivo, inclusive para estorno).</summary>
    public decimal Amount { get; set; }

    public string? CategoryId { get; set; }
    public int? InstallmentNumber { get; set; }
    public int? TotalInstallments { get; set; }
}
