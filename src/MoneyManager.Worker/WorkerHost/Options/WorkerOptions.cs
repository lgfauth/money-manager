using System.ComponentModel.DataAnnotations;

namespace TransactionSchedulerWorker.WorkerHost.Options;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>
    /// Intervalo padrão de execução.
    /// Requisito: a cada 3 horas.
    /// </summary>
    [Range(1, 24)]
    public int IntervalHours { get; init; } = 3;

    /// <summary>
    /// Timeout para uma execução (evita rodar indefinidamente).
    /// </summary>
    [Range(1, 120)]
    public int ExecutionTimeoutMinutes { get; init; } = 10;
}
