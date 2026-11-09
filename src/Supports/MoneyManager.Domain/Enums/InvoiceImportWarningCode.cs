using System.Text.Json.Serialization;

namespace MoneyManager.Domain.Enums;

/// <summary>
/// Códigos de alerta de uma importação de fatura — usados nos itens extraídos e nos alertas gerais.
/// O caráter bloqueante de cada alerta é definido pelas regras de revisão.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceImportWarningCode
{
    // Alertas de item
    MissingDate,
    MissingDescription,
    InvalidAmount,
    FutureDate,
    OutOfAllowedPeriod,
    PossibleDuplicate,
    AlreadyImported,
    AlreadyRegistered,
    PreviousInstallmentsNotRegistered,
    NotFoundInSpreadsheet,
    LowConfidence,

    // Alertas gerais da importação
    ClosingDateCalculated,
    ClosingDayMismatch,
    DueDayMismatch,
    InvoiceAlreadyImported,
    UnreadableContent,
    CellTruncated
}
