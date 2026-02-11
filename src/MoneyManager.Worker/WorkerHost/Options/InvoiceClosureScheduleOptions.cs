using System.ComponentModel.DataAnnotations;

namespace TransactionSchedulerWorker.WorkerHost.Options;

/// <summary>
/// Configurações de agendamento para fechamento de faturas de cartão de crédito
/// Executado diariamente à meia-noite e 1 minuto
/// </summary>
public sealed class InvoiceClosureScheduleOptions
{
    public const string SectionName = "InvoiceClosureSchedule";

    /// <summary>
    /// Time zone ID usado para avaliar agendamento (ex: "E. South America Standard Time")
    /// Se vazio, usa fuso horário local
    /// </summary>
    public string? TimeZoneId { get; init; }

    /// <summary>
    /// Hora do dia para executar fechamento (padrão: 0 = meia-noite)
    /// </summary>
    [Range(0, 23)]
    public int Hour { get; init; } = 0;

    /// <summary>
    /// Minuto da hora para executar (padrão: 1 = 00:01)
    /// </summary>
    [Range(0, 59)]
    public int Minute { get; init; } = 1;

    /// <summary>
    /// Delay entre verificações do loop em segundos
    /// </summary>
    [Range(5, 3600)]
    public int LoopDelaySeconds { get; init; } = 60;
}
