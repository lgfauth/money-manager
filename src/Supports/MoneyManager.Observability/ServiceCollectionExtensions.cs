using Microsoft.Extensions.DependencyInjection;

namespace MoneyManager.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProcessLogger(this IServiceCollection services)
    {
        services.AddScoped<IProcessLogger, ProcessLogger>();
        return services;
    }
}
