using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Worker.Jobs;

/// <summary>
/// Job que atualiza preços de investimentos automaticamente 3x ao dia.
/// Horários: 12:00, 15:00 e 18:00 (após fechamento do mercado).
/// </summary>
public class PriceUpdateJob : BackgroundService
{
    private readonly ILogger<PriceUpdateJob> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan[] _executionTimes = 
    {
        TimeSpan.FromHours(12), // 12:00 - Meio do dia
        TimeSpan.FromHours(15), // 15:00 - Próximo ao fechamento
        TimeSpan.FromHours(18)  // 18:00 - Após fechamento
    };

    public PriceUpdateJob(
        ILogger<PriceUpdateJob> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Price Update Job iniciado");

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
                        "Próxima atualização de preços agendada para: {NextRun}. Aguardando {Minutes} minutos...",
                        nextRun.ToString("dd/MM/yyyy HH:mm:ss"),
                        delay.TotalMinutes);

                    await Task.Delay(delay, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await UpdatePrices(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico no Price Update Job");
                // Aguarda 1 hora antes de tentar novamente em caso de erro crítico
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Price Update Job finalizado");
    }

    private DateTime GetNextRunTime(DateTime currentTime)
    {
        var today = currentTime.Date;

        // Encontrar próximo horário de execução hoje
        foreach (var time in _executionTimes.OrderBy(t => t))
        {
            var scheduledTime = today.Add(time);
            if (scheduledTime > currentTime)
            {
                return scheduledTime;
            }
        }

        // Se todos os horários de hoje já passaram, próximo é o primeiro horário de amanhã
        return today.AddDays(1).Add(_executionTimes[0]);
    }

    private async Task UpdatePrices(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var marketDataService = scope.ServiceProvider.GetRequiredService<IMarketDataService>();

        var now = DateTime.Now;
        var updatedCount = 0;
        var errorCount = 0;
        var skippedCount = 0;

        _logger.LogInformation("Iniciando atualização de preços às {Time}", now.ToString("HH:mm:ss"));

        try
        {
            // Verificar se API está disponível
            var isAvailable = await marketDataService.IsAvailableAsync();
            if (!isAvailable)
            {
                _logger.LogWarning("API de mercado não está disponível. Pulando atualização.");
                return;
            }

            // Buscar todos os ativos que têm ticker
            var allAssets = await unitOfWork.InvestmentAssets.GetAllAsync();
            var assetsWithTicker = allAssets
                .Where(a => !a.IsDeleted && !string.IsNullOrWhiteSpace(a.Ticker))
                .ToList();

            if (!assetsWithTicker.Any())
            {
                _logger.LogInformation("Nenhum ativo com ticker encontrado para atualizar");
                return;
            }

            _logger.LogInformation("Encontrados {Count} ativos com ticker para atualizar", assetsWithTicker.Count);

            foreach (var asset in assetsWithTicker)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Atualização de preços cancelada pelo sistema");
                    break;
                }

                try
                {
                    // Buscar preço atual
                    var price = await marketDataService.GetCurrentPriceAsync(asset.Ticker!, asset.AssetType);

                    if (!price.HasValue || price.Value <= 0)
                    {
                        _logger.LogWarning(
                            "Preço não encontrado ou inválido para {Ticker} ({AssetName})",
                            asset.Ticker, asset.Name);
                        skippedCount++;
                        continue;
                    }

                    // Verificar se houve mudança significativa (evitar updates desnecessários)
                    if (Math.Abs(asset.CurrentPrice - price.Value) < 0.01m)
                    {
                        _logger.LogDebug(
                            "Preço de {Ticker} não mudou significativamente: R$ {Price:F2}",
                            asset.Ticker, price.Value);
                        skippedCount++;
                        continue;
                    }

                    var oldPrice = asset.CurrentPrice;
                    asset.CurrentPrice = price.Value;
                    asset.LastPriceUpdate = DateTime.UtcNow;

                    // Recalcular valores
                    asset.CalculateCurrentValue();
                    asset.CalculateProfitLoss();

                    asset.UpdatedAt = DateTime.UtcNow;

                    await unitOfWork.InvestmentAssets.UpdateAsync(asset);

                    updatedCount++;

                    var priceChange = price.Value - oldPrice;
                    var priceChangePercentage = oldPrice > 0 ? (priceChange / oldPrice) * 100 : 0;

                    _logger.LogInformation(
                        "Preço atualizado: {Ticker} ({AssetName}) - De R$ {OldPrice:F2} para R$ {NewPrice:F2} ({Change:+0.00;-0.00}%)",
                        asset.Ticker, asset.Name, oldPrice, price.Value, priceChangePercentage);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex,
                        "Erro ao atualizar preço de {Ticker} ({AssetName})",
                        asset.Ticker, asset.Name);
                }

                // Pequeno delay entre requisições para não sobrecarregar a API
                await Task.Delay(200, cancellationToken);
            }

            // Salvar todas as mudanças de uma vez
            if (updatedCount > 0)
            {
                await unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation(
                "Atualização de preços concluída. Atualizados: {Updated}, Pulados: {Skipped}, Erros: {Errors}",
                updatedCount, skippedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ou atualizar ativos");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Price Update Job sendo finalizado...");
        await base.StopAsync(cancellationToken);
    }
}
