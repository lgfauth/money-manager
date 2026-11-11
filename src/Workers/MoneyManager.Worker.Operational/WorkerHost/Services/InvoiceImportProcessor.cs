using Microsoft.Extensions.Options;
using MoneyManager.Application.Services;
using TransactionSchedulerWorker.WorkerHost.Options;

namespace TransactionSchedulerWorker.WorkerHost.Services;

/// <summary>
/// Ponte entre o worker e o <see cref="IInvoiceImportProcessingService"/>: aplica as opções e o relógio.
/// </summary>
internal sealed class InvoiceImportProcessor(
    IInvoiceImportProcessingService processingService,
    IOptions<InvoiceImportOptions> options,
    ITimeProvider timeProvider)
{
    // Datas da fatura são do calendário brasileiro
    private const string BrazilTimeZoneId = "E. South America Standard Time";

    private readonly InvoiceImportProcessingSettings _settings = new()
    {
        MaxAttempts = options.Value.MaxAttempts,
        StuckAfter = TimeSpan.FromMinutes(options.Value.StuckAfterMinutes),
        QueuedExpiration = TimeSpan.FromHours(options.Value.QueuedExpirationHours),
        // O job precisa terminar antes de ser considerado preso
        JobTimeout = TimeSpan.FromMinutes(Math.Clamp(options.Value.JobTimeoutMinutes, 1, Math.Max(1, options.Value.StuckAfterMinutes - 1))),
        MaxJobsPerCycle = options.Value.MaxJobsPerCycle,
        DefaultModelReads = options.Value.DefaultModelReads,
        FallbackModel = options.Value.FallbackModel
    };

    public Task<bool> HasPendingWorkAsync() =>
        processingService.HasPendingWorkAsync(timeProvider.GetUtcNow().UtcDateTime, _settings);

    public Task<InvoiceImportProcessingSummary> ProcessAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var brazilNow = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById(BrazilTimeZoneId));
        return processingService.ProcessPendingAsync(now.UtcDateTime, DateOnly.FromDateTime(brazilNow.DateTime), _settings, cancellationToken);
    }
}
