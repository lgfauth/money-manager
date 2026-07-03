using System.ComponentModel.DataAnnotations;

namespace TransactionSchedulerWorker.WorkerHost.Options;

public sealed class SubscriptionExpirationScheduleOptions
{
    public const string SectionName = "SubscriptionExpirationSchedule";

    public string? TimeZoneId { get; init; }

    [Range(0, 23)]
    public int Hour { get; init; } = 2;

    [Range(0, 59)]
    public int Minute { get; init; } = 0;

    [Range(5, 3600)]
    public int LoopDelaySeconds { get; init; } = 60;
}
