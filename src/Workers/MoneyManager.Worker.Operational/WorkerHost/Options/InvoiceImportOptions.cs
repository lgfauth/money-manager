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
    public int StuckAfterMinutes { get; init; } = 25;

    /// <summary>Job na fila há mais que isto falha com a mensagem de arquivos expirados.</summary>
    [Range(1, 24)]
    public int QueuedExpirationHours { get; init; } = 24;

    /// <summary>Tempo máximo de processamento de um job; precisa ser menor que StuckAfterMinutes.</summary>
    [Range(1, 60)]
    public int JobTimeoutMinutes { get; init; } = 20;

    /// <summary>Máximo de jobs processados em cada ciclo.</summary>
    [Range(1, 100)]
    public int MaxJobsPerCycle { get; init; } = 5;

    /// <summary>Leituras com o modelo padrão enquanto a soma não confere com os totais da fatura.</summary>
    [Range(1, 5)]
    public int DefaultModelReads { get; init; } = 3;

    /// <summary>Modelo da leitura de contingência, quando as leituras com o modelo padrão não conferem.</summary>
    [Required(AllowEmptyStrings = false)]
    public string FallbackModel { get; init; } = MoneyManager.Application.Services.InvoiceImportProcessingSettings.DefaultFallbackModel;
}
