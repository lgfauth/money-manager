namespace TransactionSchedulerWorker.WorkerHost.Options;

public class BankSyncOptions
{
    public const string SectionName = "BankSync";
    public int[] SyncHours { get; set; } = [2, 9, 14, 20]; // horários de Brasília
    public int LoopDelaySeconds { get; set; } = 60;
}
