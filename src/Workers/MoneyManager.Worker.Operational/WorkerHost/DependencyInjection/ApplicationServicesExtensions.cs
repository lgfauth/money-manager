using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Infrastructure.Observability;
using MoneyManager.Infrastructure.Repositories;
using MoneyManager.Infrastructure.WorkerControl;
using MoneyManager.Observability;

namespace TransactionSchedulerWorker.WorkerHost.DependencyInjection;

internal static class ApplicationServicesExtensions
{
    internal static IServiceCollection AddMoneyManagerServicesForWorker(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB
        var mongoSettings = configuration.GetSection("MongoDB").Get<MongoSettings>() ?? new MongoSettings();
        services.AddSingleton(mongoSettings);
        services.AddSingleton<MongoContext>();
        services.AddSingleton<MongoProcessLogStore>();
        services.AddSingleton<WorkerCommandQueueService>();
        services.AddSingleton<IProcessLogStore>(sp => sp.GetRequiredService<MongoProcessLogStore>());
        services.AddSingleton<IProcessLogHistoryReader>(sp => sp.GetRequiredService<MongoProcessLogStore>());

        // UoW + Services used by the processor
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
        services.AddScoped<ICreditCardInvoiceService, CreditCardInvoiceService>();
        services.AddScoped<ICreditCardService, CreditCardService>();
        services.AddScoped<ICreditCardTransactionService, CreditCardTransactionService>();
        // Push notifications
        services.Configure<VapidSettings>(configuration.GetSection(VapidSettings.SectionName));
        services.AddScoped<IPushService, PushService>();

        return services;
    }
}
