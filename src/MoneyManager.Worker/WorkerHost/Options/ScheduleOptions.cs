using System.ComponentModel.DataAnnotations;

namespace TransactionSchedulerWorker.WorkerHost.Options;

public sealed class ScheduleOptions
{
    public const string SectionName = "Schedule";

    /// <summary>
    /// Time zone ID used to evaluate schedule (e.g. "E. South America Standard Time").
    /// If empty, local time zone is used.
    /// </summary>
    public string? TimeZoneId { get; init; }

    [Range(0, 23)]
    public int Hour { get; init; } = 8;

    [Range(0, 59)]
    public int Minute { get; init; } = 0;

    /// <summary>
    /// Safety delay to avoid re-running multiple times within the same minute.
    /// </summary>
    [Range(5, 3600)]
    public int LoopDelaySeconds { get; init; } = 30;
}
