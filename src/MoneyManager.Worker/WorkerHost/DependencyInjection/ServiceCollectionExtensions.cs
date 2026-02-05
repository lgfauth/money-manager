using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionSchedulerWorker.WorkerHost.Options;
using TransactionSchedulerWorker.WorkerHost.Services;

namespace TransactionSchedulerWorker.WorkerHost.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerHost(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<WorkerOptions>()
            .Bind(configuration.GetSection(WorkerOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<ITimeProvider>(sp => new SystemTimeProvider(TimeProvider.System));

        // Implementação default (placeholder). Substituir por uma implementação real que consome MongoDB.
        services.AddScoped<ITransactionScheduleProcessor, NoOpTransactionScheduleProcessor>();

        services.AddHostedService<ScheduledTransactionWorker>();

        return services;
    }
}
