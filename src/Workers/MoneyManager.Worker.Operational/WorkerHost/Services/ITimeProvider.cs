namespace TransactionSchedulerWorker.WorkerHost.Services;

public interface ITimeProvider
{
    DateTimeOffset GetUtcNow();
    Task Delay(TimeSpan dueTime, CancellationToken cancellationToken);
}
