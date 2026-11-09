using System.Text.Json.Serialization;

namespace MoneyManager.Domain.Enums;

/// <summary>
/// Estados do job de importação de fatura de cartão de crédito.
/// Fluxo: Queued → Processing → AwaitingReview | Failed;
/// AwaitingReview → Confirming → Confirmed (ou volta a AwaitingReview se o cadastro falhar);
/// AwaitingReview | Failed → Discarded.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceImportStatus
{
    Queued,
    Processing,
    AwaitingReview,
    Failed,
    Confirming,
    Confirmed,
    Discarded
}
