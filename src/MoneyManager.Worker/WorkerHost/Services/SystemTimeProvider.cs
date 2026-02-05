namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class SystemTimeProvider(TimeProvider timeProvider) : ITimeProvider
{
    public DateTimeOffset GetUtcNow() => timeProvider.GetUtcNow();

    public Task Delay(TimeSpan dueTime, CancellationToken cancellationToken) =>
        Task.Delay(dueTime, cancellationToken);
}
