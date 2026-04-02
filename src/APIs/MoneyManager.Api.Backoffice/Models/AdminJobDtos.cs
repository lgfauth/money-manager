namespace MoneyManager.Api.Administration.Models;

public sealed class RunNowJobRequest
{
    public string? Reason { get; set; }
}

public sealed class JobCommandResponse
{
    public string CommandId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public bool AlreadyQueued { get; set; }
}

public sealed class UpdateJobScheduleRequest
{
    public string? TimeZoneId { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int LoopDelaySeconds { get; set; }
    public string? Reason { get; set; }
}

public sealed class JobScheduleResponse
{
    public string JobName { get; set; } = string.Empty;
    public string? TimeZoneId { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int LoopDelaySeconds { get; set; }
    public DateTime? LastChangedAtUtc { get; set; }
    public string? LastChangedBy { get; set; }
}