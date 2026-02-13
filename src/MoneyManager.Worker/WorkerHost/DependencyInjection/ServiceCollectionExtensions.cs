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
        services.AddMoneyManagerServicesForWorker(configuration);

        services
            .AddOptions<WorkerOptions>()
            .Bind(configuration.GetSection(WorkerOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<ScheduleOptions>()
            .Bind(configuration.GetSection(ScheduleOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<InvoiceClosureScheduleOptions>()
            .Bind(configuration.GetSection(InvoiceClosureScheduleOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<ITimeProvider>(sp => new SystemTimeProvider(TimeProvider.System));

        // Only recurring transactions are processed by the worker.
        services.AddScoped<ITransactionScheduleProcessor, RecurringTransactionsProcessor>();
        services.AddScoped<InvoiceClosureProcessor>();

        services.AddHostedService<ScheduledTransactionWorker>();
        services.AddHostedService<InvoiceClosureWorker>();
        services.AddHostedService<MoneyManager.Worker.Jobs.InvestmentYieldProcessorJob>();
        services.AddHostedService<MoneyManager.Worker.Jobs.PriceUpdateJob>();

        return services;
    }
}
