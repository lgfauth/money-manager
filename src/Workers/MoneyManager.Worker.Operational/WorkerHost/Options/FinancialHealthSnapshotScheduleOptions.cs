using System.ComponentModel.DataAnnotations;

namespace TransactionSchedulerWorker.WorkerHost.Options;

public sealed class FinancialHealthSnapshotScheduleOptions
{
    public const string SectionName = "FinancialHealthSnapshotSchedule";

    public string? TimeZoneId { get; init; }

    [Range(0, 23)]
    public int Hour { get; init; } = 1;

    [Range(0, 59)]
    public int Minute { get; init; } = 30;

    [Range(5, 3600)]
    public int LoopDelaySeconds { get; init; } = 60;
}
