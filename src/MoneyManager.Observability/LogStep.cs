using System.Text.Json.Serialization;

namespace MoneyManager.Observability;

public sealed class LogStep
{
    /// <summary>
    /// Milliseconds elapsed since the process started.
    /// </summary>
    [JsonPropertyName("at")]
    public long At { get; set; }

    [JsonPropertyName("level")]
    public string Level { get; set; } = "Information";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? Data { get; set; }
}
