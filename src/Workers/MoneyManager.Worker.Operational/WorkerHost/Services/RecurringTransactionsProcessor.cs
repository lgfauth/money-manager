using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class RecurringTransactionsProcessor(
    IProcessLogger processLogger,
    IRecurringTransactionService recurringTransactionService,
    IUnitOfWork unitOfWork,
    IPushService pushService) : ITransactionScheduleProcessor
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        processLogger.AddStep("Processando recorrências vencidas");
        var summary = await recurringTransactionService.ProcessDueRecurrencesAsync();
        processLogger.AddStep("Processamento de recorrências finalizado", new Dictionary<string, object?>
        {
            ["usersAffected"] = summary.ProcessedByUser.Count,
            ["totalProcessed"] = summary.ProcessedByUser.Values.Sum()
        });

        cancellationToken.ThrowIfCancellationRequested();

        await SendPushNotificationsAsync(summary, cancellationToken);
    }

    private async Task SendPushNotificationsAsync(
        RecurringProcessingSummary summary,
        CancellationToken cancellationToken)
    {
        if (summary.ProcessedByUser.Count == 0)
        {
            processLogger.AddStep("Nenhuma recorrência processada — sem push notifications a enviar");
            return;
        }

        var sent = 0;
        var skipped = 0;

        foreach (var (userId, count) in summary.ProcessedByUser)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var userSettings = await unitOfWork.UserSettings.GetAllAsync();
                var settings = userSettings.FirstOrDefault(s => s.UserId == userId);

                if (settings is { PushRecurringProcessed: false })
                {
                    skipped++;
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
                sent++;
            }
            catch (Exception ex)
            {
                processLogger.AddError($"Falha ao enviar push para usuário {userId}", ex);
            }
        }

        processLogger.AddStep("Push notifications enviadas", new Dictionary<string, object?>
        {
            ["sent"] = sent,
            ["skipped"] = skipped
        });
    }
}
