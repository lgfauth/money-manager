using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Infrastructure.Observability;
using MoneyManager.Infrastructure.Repositories;
using MoneyManager.Infrastructure.Services;
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

        // Conexão bancária
        services.AddHttpClient("bancoMcp");
        services.AddScoped<IBankMcpClient, BankMcpClient>();
        services.AddScoped<IEncryptionService, AesEncryptionService>();
        services.AddScoped<IBankConnectionService, BankConnectionService>();
        services.AddScoped<IOpenBankingCategoryMigrationService, OpenBankingCategoryMigrationService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPaymentGateway, NullPaymentGateway>();

        return services;
    }
}

// Stub de IPaymentGateway para o contexto do worker — os jobs nunca invocam métodos de
// pagamento. Satisfaz o DI do SubscriptionService sem exigir credenciais Efí Bank no
// worker (mesmo padrão adotado no Backoffice).
file sealed class NullPaymentGateway : IPaymentGateway
{
    public string ProviderName => "none";
    public Task<CreateSubscriptionGatewayResult> CreateSubscriptionAsync(CreateSubscriptionGatewayRequest request) => throw new NotSupportedException();
    public Task CancelSubscriptionAsync(string externalSubscriptionId) => throw new NotSupportedException();
    public Task<WebhookValidationResult> ValidateAndParseWebhookAsync(string rawPayload, IDictionary<string, string> headers) => throw new NotSupportedException();
}
