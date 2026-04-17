using System.Text.Json.Serialization;

namespace MoneyManager.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceStatus
{
    Pending,
    Open,
    Closed,
    Paid,
    Overdue
}
