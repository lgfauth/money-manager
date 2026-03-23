using Microsoft.Extensions.Logging;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class RecurringTransactionsProcessor(
    ILogger<RecurringTransactionsProcessor> logger,
    IRecurringTransactionService recurringTransactionService,
    IUnitOfWork unitOfWork,
    IPushService pushService) : ITransactionScheduleProcessor
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Processando recorrências vencidas...");
        var summary = await recurringTransactionService.ProcessDueRecurrencesAsync();
        logger.LogInformation("Processamento de recorrências finalizado.");

        cancellationToken.ThrowIfCancellationRequested();

        await SendPushNotificationsAsync(summary, cancellationToken);
    }

    private async Task SendPushNotificationsAsync(
        RecurringProcessingSummary summary,
        CancellationToken cancellationToken)
    {
        if (summary.ProcessedByUser.Count == 0)
        {
            logger.LogDebug("Nenhuma recorrência processada — sem push notifications a enviar.");
            return;
        }

        foreach (var (userId, count) in summary.ProcessedByUser)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                // Respeita a preferência do usuário
                var userSettings = await unitOfWork.UserSettings.GetAllAsync();
                var settings = userSettings.FirstOrDefault(s => s.UserId == userId);

                if (settings is { PushRecurringProcessed: false })
                {
                    logger.LogDebug("Usuário {UserId} desativou push de recorrentes. Pulando.", userId);
                    continue;
                }

                var payload = new PushNotificationPayload
                {
                    Title = "MoneyManager — Transações do dia",
                    Body = count == 1
                        ? "1 transação recorrente foi lançada automaticamente hoje."
                        : $"{count} transações recorrentes foram lançadas automaticamente hoje.",
                    Icon = "/favicon.svg",
                    Url = "/transactions"
                };

                await pushService.SendToUserAsync(userId, payload);
                logger.LogInformation("Push enviado para usuário {UserId} ({Count} recorrência(s))", userId, count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar push para usuário {UserId}", userId);
            }
        }
    }
}


