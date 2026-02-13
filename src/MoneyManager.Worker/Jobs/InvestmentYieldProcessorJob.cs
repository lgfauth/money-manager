using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Domain.Enums;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Worker.Jobs;

/// <summary>
/// Job que processa rendimentos recorrentes de investimentos diariamente.
/// Executado automaticamente pelo Worker Service.
/// </summary>
public class InvestmentYieldProcessorJob : BackgroundService
{
    private readonly ILogger<InvestmentYieldProcessorJob> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);
    private readonly TimeSpan _executionTime = TimeSpan.FromHours(0); // 00:00 (meia-noite)

    public InvestmentYieldProcessorJob(
        ILogger<InvestmentYieldProcessorJob> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Investment Yield Processor Job iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var nextRun = GetNextRunTime(now);
                var delay = nextRun - now;

                if (delay.TotalMilliseconds > 0)
                {
                    _logger.LogInformation(
                        "Próxima execução agendada para: {NextRun}. Aguardando {Minutes} minutos...",
                        nextRun.ToString("dd/MM/yyyy HH:mm:ss"),
                        delay.TotalMinutes);

                    await Task.Delay(delay, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await ProcessInvestmentYields(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico no Investment Yield Processor Job");
                // Aguarda 1 hora antes de tentar novamente em caso de erro crítico
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Investment Yield Processor Job finalizado");
    }

    private DateTime GetNextRunTime(DateTime currentTime)
    {
        var nextRun = currentTime.Date.Add(_executionTime);
        
        // Se já passou do horário hoje, agenda para amanhã
        if (nextRun <= currentTime)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun;
    }

    private async Task ProcessInvestmentYields(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var investmentTransactionService = scope.ServiceProvider.GetRequiredService<IInvestmentTransactionService>();

        var today = DateTime.Today;
        var processedCount = 0;
        var errorCount = 0;

        _logger.LogInformation("Iniciando processamento de rendimentos recorrentes para {Date}", today.ToString("dd/MM/yyyy"));

        try
        {
            // Buscar todas as transações recorrentes ativas vinculadas a investimentos
            var allRecurring = await unitOfWork.RecurringTransactions.GetAllAsync();
            
            var investmentRecurring = allRecurring
                .Where(r => r.IsActive && 
                           !r.IsDeleted && 
                           !string.IsNullOrEmpty(r.LinkedInvestmentAssetId) &&
                           r.NextOccurrenceDate.Date <= today)
                .ToList();

            _logger.LogInformation("Encontradas {Count} transações recorrentes de investimento para processar", investmentRecurring.Count);

            foreach (var recurring in investmentRecurring)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Processamento cancelado pelo sistema");
                    break;
                }

                try
                {
                    await ProcessSingleYield(recurring, unitOfWork, investmentTransactionService);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, 
                        "Erro ao processar rendimento recorrente ID: {RecurringId}, Ativo: {AssetId}", 
                        recurring.Id, 
                        recurring.LinkedInvestmentAssetId);
                }
            }

            _logger.LogInformation(
                "Processamento concluído. Sucesso: {Processed}, Erros: {Errors}",
                processedCount,
                errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar transações recorrentes de investimento");
        }
    }

    private async Task ProcessSingleYield(
        Domain.Entities.RecurringTransaction recurring,
        IUnitOfWork unitOfWork,
        IInvestmentTransactionService investmentTransactionService)
    {
        // Verificar se o ativo ainda existe
        var asset = await unitOfWork.InvestmentAssets.GetByIdAsync(recurring.LinkedInvestmentAssetId!);
        if (asset == null)
        {
            _logger.LogWarning(
                "Ativo {AssetId} não encontrado para rendimento recorrente {RecurringId}. Desativando recorrência.",
                recurring.LinkedInvestmentAssetId,
                recurring.Id);

            recurring.IsActive = false;
            recurring.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.RecurringTransactions.UpdateAsync(recurring);
            await unitOfWork.SaveChangesAsync();
            return;
        }

        // Verificar se já foi processado hoje (idempotência)
        if (recurring.LastProcessedDate.HasValue && 
            recurring.LastProcessedDate.Value.Date == DateTime.Today)
        {
            _logger.LogInformation(
                "Rendimento recorrente {RecurringId} já foi processado hoje. Pulando.",
                recurring.Id);
            return;
        }

        _logger.LogInformation(
            "Processando rendimento de {Amount:C} para ativo {AssetName} (ID: {AssetId})",
            recurring.Amount,
            asset.Name,
            asset.Id);

        // Determinar o tipo de rendimento
        var yieldType = DetermineYieldType(asset.AssetType, recurring.Description);

        // Criar o request de rendimento
        var yieldRequest = new RecordYieldRequestDto
        {
            AssetId = asset.Id,
            Amount = recurring.Amount,
            YieldType = yieldType,
            Date = DateTime.Today,
            Description = recurring.Description ?? $"Rendimento recorrente - {asset.Name}"
        };

        // Registrar o rendimento através do serviço
        await investmentTransactionService.RecordYieldAsync(asset.UserId, yieldRequest);

        // Atualizar a transação recorrente
        recurring.LastProcessedDate = DateTime.Today;
        recurring.NextOccurrenceDate = CalculateNextOccurrence(recurring);
        recurring.UpdatedAt = DateTime.UtcNow;

        // Verificar se chegou ao fim da recorrência
        if (recurring.EndDate.HasValue && recurring.NextOccurrenceDate > recurring.EndDate.Value)
        {
            _logger.LogInformation(
                "Recorrência {RecurringId} atingiu data final. Desativando.",
                recurring.Id);
            recurring.IsActive = false;
        }

        await unitOfWork.RecurringTransactions.UpdateAsync(recurring);
        await unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Rendimento processado com sucesso. Próxima ocorrência: {NextDate}",
            recurring.NextOccurrenceDate.ToString("dd/MM/yyyy"));
    }

    private InvestmentTransactionType DetermineYieldType(InvestmentAssetType assetType, string? description)
    {
        // Lógica para determinar o tipo de rendimento baseado no tipo de ativo
        return assetType switch
        {
            InvestmentAssetType.Stock => InvestmentTransactionType.Dividend,
            InvestmentAssetType.FixedIncome => InvestmentTransactionType.Interest,
            InvestmentAssetType.RealEstate => InvestmentTransactionType.YieldPayment,
            InvestmentAssetType.Fund => InvestmentTransactionType.Dividend,
            InvestmentAssetType.ETF => InvestmentTransactionType.Dividend,
            _ => InvestmentTransactionType.YieldPayment
        };
    }

    private DateTime CalculateNextOccurrence(Domain.Entities.RecurringTransaction recurring)
    {
        var next = recurring.NextOccurrenceDate;

        return recurring.Frequency switch
        {
            RecurrenceFrequency.Daily => next.AddDays(1),
            RecurrenceFrequency.Weekly => next.AddDays(7),
            RecurrenceFrequency.Biweekly => next.AddDays(14),
            RecurrenceFrequency.Monthly => CalculateNextMonthly(next, recurring.DayOfMonth),
            RecurrenceFrequency.Quarterly => next.AddMonths(3),
            RecurrenceFrequency.Semiannual => next.AddMonths(6),
            RecurrenceFrequency.Annual => next.AddYears(1),
            _ => next.AddMonths(1)
        };
    }

    private DateTime CalculateNextMonthly(DateTime current, int? preferredDay)
    {
        var nextMonth = current.AddMonths(1);
        
        if (preferredDay.HasValue)
        {
            var maxDay = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
            var day = Math.Min(preferredDay.Value, maxDay);
            return new DateTime(nextMonth.Year, nextMonth.Month, day);
        }

        return nextMonth;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Investment Yield Processor Job sendo finalizado...");
        await base.StopAsync(cancellationToken);
    }
}
