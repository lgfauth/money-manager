using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

/// <summary>
/// Processamento das importações de fatura pelo Worker: recupera jobs presos, expira jobs antigos,
/// reserva e processa os jobs da fila (IA → revisão), grava o resultado, apaga os arquivos e avisa
/// o usuário por push. Nunca registra em log conteúdo do documento, nomes de titulares ou base64.
/// </summary>
public interface IInvoiceImportProcessingService
{
    /// <summary>Indica se há trabalho para o Worker (job na fila ou preso em processamento).</summary>
    Task<bool> HasPendingWorkAsync(DateTime nowUtc, InvoiceImportProcessingSettings settings);

    Task<InvoiceImportProcessingSummary> ProcessPendingAsync(
        DateTime nowUtc,
        DateOnly today,
        InvoiceImportProcessingSettings settings,
        CancellationToken cancellationToken);
}

/// <summary>Parâmetros do processamento (vindos das opções do Worker).</summary>
public sealed record InvoiceImportProcessingSettings
{
    /// <summary>Tentativas antes de o job ir para Failed em falhas temporárias.</summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>Job em Processing há mais tempo que isto é considerado preso e volta para a fila.</summary>
    public TimeSpan StuckAfter { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>Job Queued há mais tempo que isto expira (os arquivos já foram removidos pelo TTL).</summary>
    public TimeSpan QueuedExpiration { get; init; } = TimeSpan.FromHours(24);

    /// <summary>Limite de tempo do processamento de um job; deve ser menor que <see cref="StuckAfter"/>.</summary>
    public TimeSpan JobTimeout { get; init; } = TimeSpan.FromMinutes(8);

    /// <summary>Máximo de jobs processados por ciclo do Worker.</summary>
    public int MaxJobsPerCycle { get; init; } = 5;
}

public sealed record InvoiceImportProcessingSummary(int Recovered, int Expired, int Succeeded, int Failed, int Retried);

public class InvoiceImportProcessingService : IInvoiceImportProcessingService
{
    public const string ExpiredFilesMessage = "Os arquivos da fatura expiraram. Envie a fatura novamente.";
    public const string CardNotFoundMessage = "O cartão escolhido para a importação não foi encontrado. Envie a fatura novamente.";
    public const string TooManyAttemptsMessage = "Não foi possível ler a fatura depois de várias tentativas. Envie a fatura novamente mais tarde.";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IInvoiceDocumentAnalysisService _analysisService;
    private readonly IInvoiceImportReviewService _reviewService;
    private readonly ICreditCardImportEligibilityService _eligibilityService;
    private readonly IPushService _pushService;
    private readonly IProcessLogger _processLogger;

    public InvoiceImportProcessingService(
        IUnitOfWork unitOfWork,
        IInvoiceDocumentAnalysisService analysisService,
        IInvoiceImportReviewService reviewService,
        ICreditCardImportEligibilityService eligibilityService,
        IPushService pushService,
        IProcessLogger processLogger)
    {
        _unitOfWork = unitOfWork;
        _analysisService = analysisService;
        _reviewService = reviewService;
        _eligibilityService = eligibilityService;
        _pushService = pushService;
        _processLogger = processLogger;
    }

    /// <summary>URL do frontend para revisar a importação (aberta pela notificação push).</summary>
    public static string ReviewUrl(string importId) => $"/credit-cards/invoice-imports/{importId}";

    public async Task<bool> HasPendingWorkAsync(DateTime nowUtc, InvoiceImportProcessingSettings settings)
    {
        if (await _unitOfWork.CreditCardInvoiceImports.AnyQueuedAsync())
            return true;

        var stuck = await _unitOfWork.CreditCardInvoiceImports.GetStuckProcessingAsync(nowUtc - settings.StuckAfter);
        return stuck.Any();
    }

    public async Task<InvoiceImportProcessingSummary> ProcessPendingAsync(
        DateTime nowUtc,
        DateOnly today,
        InvoiceImportProcessingSettings settings,
        CancellationToken cancellationToken)
    {
        var recovered = await RecoverStuckJobsAsync(nowUtc, settings);
        var expired = await ExpireOldQueuedJobsAsync(nowUtc, settings);

        int succeeded = 0, failed = 0, retried = 0;

        for (var i = 0; i < settings.MaxJobsPerCycle && !cancellationToken.IsCancellationRequested; i++)
        {
            var job = await _unitOfWork.CreditCardInvoiceImports.ClaimNextQueuedAsync(nowUtc);
            if (job is null)
                break;

            var outcome = await ProcessJobAsync(job, today, settings, cancellationToken);
            switch (outcome)
            {
                case JobOutcome.Succeeded: succeeded++; break;
                case JobOutcome.Failed: failed++; break;
                case JobOutcome.Retried: retried++; break;
            }
        }

        var summary = new InvoiceImportProcessingSummary(recovered, expired, succeeded, failed, retried);
        _processLogger.AddStep("Importações de fatura processadas", new Dictionary<string, object?>
        {
            ["recovered"] = summary.Recovered,
            ["expired"] = summary.Expired,
            ["succeeded"] = summary.Succeeded,
            ["failed"] = summary.Failed,
            ["retried"] = summary.Retried
        });

        return summary;
    }

    // --- Recuperação e expiração ---

    private async Task<int> RecoverStuckJobsAsync(DateTime nowUtc, InvoiceImportProcessingSettings settings)
    {
        var stuck = await _unitOfWork.CreditCardInvoiceImports.GetStuckProcessingAsync(nowUtc - settings.StuckAfter);
        var count = 0;

        foreach (var job in stuck)
        {
            count++;
            job.Attempts++;
            _processLogger.AddWarning("Importação presa em processamento recuperada", JobData(job));

            if (!await HasAllFilesAsync(job))
                await FailAsync(job, ExpiredFilesMessage);
            else if (job.Attempts >= settings.MaxAttempts)
                await FailAsync(job, TooManyAttemptsMessage);
            else
                await RequeueAsync(job);
        }

        return count;
    }

    private async Task<int> ExpireOldQueuedJobsAsync(DateTime nowUtc, InvoiceImportProcessingSettings settings)
    {
        var old = await _unitOfWork.CreditCardInvoiceImports.GetQueuedCreatedBeforeAsync(nowUtc - settings.QueuedExpiration);
        var count = 0;

        foreach (var job in old)
        {
            count++;
            await FailAsync(job, ExpiredFilesMessage);
        }

        return count;
    }

    // --- Processamento de um job ---

    private enum JobOutcome { Succeeded, Failed, Retried }

    private async Task<JobOutcome> ProcessJobAsync(
        CreditCardInvoiceImport job,
        DateOnly today,
        InvoiceImportProcessingSettings settings,
        CancellationToken stoppingToken)
    {
        _processLogger.AddStep("Processando importação de fatura", JobData(job));

        try
        {
            var card = await _unitOfWork.CreditCards.GetByIdAsync(job.CreditCardId);
            if (card is null || card.IsDeleted || card.UserId != job.UserId)
                return await FailAsync(job, CardNotFoundMessage);

            // O vínculo com o Open Finance pode ter surgido depois do upload
            if (await _eligibilityService.IsSyncedByOpenFinanceAsync(job.UserId, card.Id))
                return await FailAsync(job, CreditCardImportEligibilityService.SyncedCardMessage);

            var files = (await _unitOfWork.CreditCardInvoiceImportFiles.GetByImportAsync(job.UserId, job.Id)).ToList();
            if (files.Count == 0 || files.Count < job.FileCount)
                return await FailAsync(job, ExpiredFilesMessage);

            InvoiceDocumentAnalysisResultDto analysis;
            using (var jobTimeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken))
            {
                jobTimeout.CancelAfter(settings.JobTimeout);
                try
                {
                    analysis = await _analysisService.AnalyzeAsync(
                        new InvoiceDocumentAnalysisRequestDto(
                            job.SourceType,
                            files.Select(f => new InvoiceDocumentFileDto(f.ContentType, f.Content)).ToList(),
                            today),
                        jobTimeout.Token);
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // Tempo do job esgotado: tratado como falha temporária
                    return await RetryOrFailAsync(job, settings, "Tempo de leitura da fatura esgotado");
                }
            }

            // Planilha: o texto gravado é a própria fonte da conferência de cada item
            var spreadsheet = job.SourceType == InvoiceImportSourceType.Spreadsheet
                ? SpreadsheetText.Parse(System.Text.Encoding.UTF8.GetString(files[0].Content))
                : null;

            var review = await _reviewService.BuildReviewAsync(job.UserId, card, analysis, today, spreadsheet);
            if (!review.IsSuccess)
                return await FailAsync(job, review.FailureMessage!);

            job.Status = InvoiceImportStatus.AwaitingReview;
            job.ErrorMessage = null;
            job.ReferenceMonth = review.ReferenceMonth;
            job.Invoice = review.Invoice;
            job.Holders = review.Holders;
            job.Warnings = review.Warnings;
            job.Items = review.Items;
            job.ProcessedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CreditCardInvoiceImports.UpdateAsync(job);
            await DeleteFilesAsync(job);

            _processLogger.AddStep("Importação pronta para revisão", new Dictionary<string, object?>
            {
                ["importId"] = job.Id,
                ["items"] = job.Items.Count,
                ["holders"] = job.Holders.Count,
                ["warnings"] = job.Warnings.Count
            });

            await NotifyAsync(job, "Fatura pronta para revisão",
                $"Encontramos {job.Items.Count} lançamento(s) na fatura. Revise e confirme a importação.");
            return JobOutcome.Succeeded;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Desligamento do host: devolve o job para a fila sem contar tentativa
            await RequeueAsync(job);
            throw;
        }
        catch (InvoiceDocumentAnalysisException ex) when (ex.IsTransient)
        {
            return await RetryOrFailAsync(job, settings, "Falha temporária na leitura da fatura");
        }
        catch (InvoiceDocumentAnalysisException ex)
        {
            return await FailAsync(job, ex.Message);
        }
        catch (Exception ex)
        {
            // Erro inesperado: tenta de novo, sem expor detalhes ao usuário
            _processLogger.AddError("Erro inesperado ao processar importação de fatura", ex, JobData(job));
            return await RetryOrFailAsync(job, settings, "Erro inesperado no processamento");
        }
    }

    private async Task<JobOutcome> RetryOrFailAsync(CreditCardInvoiceImport job, InvoiceImportProcessingSettings settings, string reason)
    {
        job.Attempts++;
        _processLogger.AddWarning(reason, JobData(job));

        if (job.Attempts >= settings.MaxAttempts)
            return await FailAsync(job, TooManyAttemptsMessage);

        await RequeueAsync(job);
        return JobOutcome.Retried;
    }

    // --- Transições ---

    private async Task RequeueAsync(CreditCardInvoiceImport job)
    {
        job.Status = InvoiceImportStatus.Queued;
        job.ProcessingStartedAt = null;
        job.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.CreditCardInvoiceImports.UpdateAsync(job);
    }

    private async Task<JobOutcome> FailAsync(CreditCardInvoiceImport job, string message)
    {
        job.Status = InvoiceImportStatus.Failed;
        job.ErrorMessage = message;
        job.ProcessedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.CreditCardInvoiceImports.UpdateAsync(job);
        await DeleteFilesAsync(job);

        _processLogger.AddWarning("Importação de fatura falhou", JobData(job));

        await NotifyAsync(job, "Não foi possível importar a fatura", message);
        return JobOutcome.Failed;
    }

    private async Task<bool> HasAllFilesAsync(CreditCardInvoiceImport job)
    {
        var count = await _unitOfWork.CreditCardInvoiceImportFiles.CountByImportAsync(job.UserId, job.Id);
        return count > 0 && count >= job.FileCount;
    }

    private async Task DeleteFilesAsync(CreditCardInvoiceImport job)
    {
        try
        {
            await _unitOfWork.CreditCardInvoiceImportFiles.DeleteByImportAsync(job.UserId, job.Id);
        }
        catch (Exception ex)
        {
            // O índice TTL de 24 h remove o que sobrar
            _processLogger.AddError("Falha ao apagar arquivos da importação", ex, JobData(job));
        }
    }

    private async Task NotifyAsync(CreditCardInvoiceImport job, string title, string body)
    {
        try
        {
            await _pushService.SendToUserAsync(job.UserId, new PushNotificationPayload
            {
                Title = title,
                Body = body,
                Url = ReviewUrl(job.Id)
            });
        }
        catch (Exception ex)
        {
            // Falha no push não altera o resultado do job
            _processLogger.AddWarning("Falha ao enviar push da importação de fatura", new Dictionary<string, object?>
            {
                ["importId"] = job.Id,
                ["error"] = ex.GetType().Name
            });
        }
    }

    private static Dictionary<string, object?> JobData(CreditCardInvoiceImport job) => new()
    {
        ["importId"] = job.Id,
        ["userId"] = job.UserId,
        ["sourceType"] = job.SourceType.ToString(),
        ["fileCount"] = job.FileCount,
        ["attempts"] = job.Attempts,
        ["status"] = job.Status.ToString()
    };
}
