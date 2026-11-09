using System.ComponentModel.DataAnnotations;

namespace TransactionSchedulerWorker.WorkerHost.Options;

/// <summary>
/// Opções do worker de importação de fatura de cartão (seção "InvoiceImport").
/// </summary>
public sealed class InvoiceImportOptions
{
    public const string SectionName = "InvoiceImport";

    /// <summary>Intervalo entre verificações da fila.</summary>
    [Range(1, 3600)]
    public int PollIntervalSeconds { get; init; } = 10;

    /// <summary>Tentativas antes de o job falhar em erros temporários da IA.</summary>
    [Range(1, 10)]
    public int MaxAttempts { get; init; } = 3;

    /// <summary>Job em processamento há mais que isto volta para a fila (conta tentativa).</summary>
    [Range(1, 1440)]
    public int StuckAfterMinutes { get; init; } = 10;

    /// <summary>Job na fila há mais que isto falha com a mensagem de arquivos expirados.</summary>
    [Range(1, 24)]
    public int QueuedExpirationHours { get; init; } = 24;

    /// <summary>Tempo máximo de processamento de um job; precisa ser menor que StuckAfterMinutes.</summary>
    [Range(1, 60)]
    public int JobTimeoutMinutes { get; init; } = 8;

    /// <summary>Máximo de jobs processados em cada ciclo.</summary>
    [Range(1, 100)]
    public int MaxJobsPerCycle { get; init; } = 5;
}
