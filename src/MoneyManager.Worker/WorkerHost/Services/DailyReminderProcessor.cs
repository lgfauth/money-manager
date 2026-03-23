using Microsoft.Extensions.Logging;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;

namespace TransactionSchedulerWorker.WorkerHost.Services;

/// <summary>
/// Envia um lembrete push às 21h para os usuários que têm a opção ativa,
/// incentivando o registro dos gastos e receitas do dia.
/// </summary>
internal sealed class DailyReminderProcessor(
    ILogger<DailyReminderProcessor> logger,
    IUnitOfWork unitOfWork,
    IPushService pushService)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Iniciando envio de lembretes diários push...");

        var allSettings = await unitOfWork.UserSettings.GetAllAsync();
        var targets = allSettings
            .Where(s => s.PushDailyReminder)
            .ToList();

        logger.LogInformation("{Count} usuário(s) com lembrete diário ativo.", targets.Count);

        foreach (var userSettings in targets)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var payload = new PushNotificationPayload
                {
                    Title = "MoneyManager — Lembrete do dia ??",
                    Body = "Não se esqueça de registrar seus gastos e receitas de hoje!",
                    Icon = "/favicon.svg",
                    Url = "/transactions"
                };

                await pushService.SendToUserAsync(userSettings.UserId, payload);
                logger.LogInformation("Lembrete diário enviado para usuário {UserId}", userSettings.UserId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar lembrete diário para usuário {UserId}", userSettings.UserId);
            }
        }

        logger.LogInformation("Envio de lembretes diários concluído.");
    }
}
