using System.Text.Json.Serialization;

namespace MoneyManager.Domain.Enums;

/// <summary>
/// Tipo do cartão informado na fatura para o lançamento (físico ou virtual).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceImportCardType
{
    Unknown,
    Physical,
    Virtual
}
