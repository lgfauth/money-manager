using System.Text.Json.Serialization;

namespace MoneyManager.Domain.Enums;

/// <summary>
/// Tipo de origem dos arquivos de uma importação de fatura (um único tipo por upload).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceImportSourceType
{
    Pdf,
    Images,
    Spreadsheet
}
