using System.ComponentModel.DataAnnotations;

namespace TransactionSchedulerWorker.WorkerHost.Options;

/// <summary>
/// Configuracoes de agendamento para marcacao automatica de faturas vencidas.
/// </summary>
public sealed class OverdueInvoiceScheduleOptions
{
    public const string SectionName = "OverdueInvoiceSchedule";

    /// <summary>
    /// Time zone ID usado para avaliar agendamento (ex: "E. South America Standard Time").
    /// Se vazio, usa fuso horario local.
    /// </summary>
    public string? TimeZoneId { get; init; }

    /// <summary>
    /// Hora do dia para executar marcacao de vencidas.
    /// </summary>
    [Range(0, 23)]
    public int Hour { get; init; } = 3;

    /// <summary>
    /// Minuto da hora para executar.
    /// </summary>
    [Range(0, 59)]
    public int Minute { get; init; } = 15;

    /// <summary>
    /// Delay entre verificacoes do loop em segundos.
    /// </summary>
    [Range(5, 3600)]
    public int LoopDelaySeconds { get; init; } = 60;
}
