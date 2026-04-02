using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class DailyReminderProcessor(
    IProcessLogger processLogger,
    IUnitOfWork unitOfWork,
    IPushService pushService)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        processLogger.AddStep("Buscando usuários com lembrete diário ativo");

        var allSettings = await unitOfWork.UserSettings.GetAllAsync();
        var targets = allSettings
            .Where(s => s.PushDailyReminder)
            .ToList();

        processLogger.AddStep("Usuários elegíveis encontrados", new Dictionary<string, object?>
        {
            ["count"] = targets.Count
        });

        var sent = 0;
        var failed = 0;

        foreach (var userSettings in targets)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var payload = new PushNotificationPayload
                {
                    Title = "MoneyManager — Lembrete do dia",
                    Body = "Não se esqueça de registrar seus gastos e receitas de hoje!",
                    Icon = "/favicon.svg",
                    Url = "/transactions"
                };

                await pushService.SendToUserAsync(userSettings.UserId, payload);
                sent++;
            }
            catch (Exception ex)
            {
                failed++;
                processLogger.AddError($"Falha ao enviar lembrete para usuário {userSettings.UserId}", ex);
            }
        }

        processLogger.AddStep("Envio de lembretes concluído", new Dictionary<string, object?>
        {
            ["sent"] = sent,
            ["failed"] = failed
        });
    }
}
