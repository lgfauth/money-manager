using System.Text.Json.Serialization;

namespace MoneyManager.Domain.Enums;

/// <summary>
/// Indica se o lançamento da fatura pertence ao titular ou a um cartão adicional.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceImportHolderType
{
    Unknown,
    Primary,
    Additional
}
