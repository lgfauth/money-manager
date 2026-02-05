using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Infrastructure.Repositories;

namespace TransactionSchedulerWorker.WorkerHost.DependencyInjection;

internal static class ApplicationServicesExtensions
{
    internal static IServiceCollection AddMoneyManagerServicesForWorker(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB
        var mongoSettings = configuration.GetSection("MongoDB").Get<MongoSettings>() ?? new MongoSettings();
        services.AddSingleton(mongoSettings);
        services.AddSingleton<MongoContext>();

        // UoW + Services used by the processor
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();

        return services;
    }
}
