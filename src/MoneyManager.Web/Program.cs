using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoneyManager.Web.Services;
using MoneyManager.Web.Services.Localization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<MoneyManager.Web.App>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

// Configure HttpClient - URL da API via configuração (appsettings ou variável de ambiente via Docker)
// Em produção, o Docker entrypoint substitui #{API_URL}# no appsettings.Production.json
// Fallback: usa o BaseAddress do host (resolvido dinamicamente pelo browser)
var configuredApiUrl = builder.Configuration["ApiUrl"];
string apiUrl;

Console.WriteLine($"[MoneyManager] ApiUrl: {builder.Configuration["ApiUrl"]}");
Console.WriteLine($"[MoneyManager] API_URL: {builder.Configuration["API_URL"]}");

if (!string.IsNullOrEmpty(configuredApiUrl) && Uri.TryCreate(configuredApiUrl, UriKind.Absolute, out _))
{
    apiUrl = configuredApiUrl;
    Console.WriteLine($"[MoneyManager] API URL: {apiUrl}");
}
else
{
    apiUrl = builder.HostEnvironment.BaseAddress;
    Console.WriteLine($"[MoneyManager] API URL (from HostEnvironment): {apiUrl}");
}

// Register AuthorizationMessageHandler
builder.Services.AddScoped<AuthorizationMessageHandler>();

// Configure HttpClient with AuthorizationMessageHandler for API calls
builder.Services.AddScoped(sp =>
{
    var authHandler = sp.GetRequiredService<AuthorizationMessageHandler>();
    authHandler.InnerHandler = new HttpClientHandler();

    var httpClient = new HttpClient(authHandler)
    {
        BaseAddress = new Uri(apiUrl)
    };

    return httpClient;
});

// Register Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Register application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IAccountDeletionService, AccountDeletionService>();
builder.Services.AddScoped<ICreditCardInvoiceService, CreditCardInvoiceService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();

// Localization (JSON in wwwroot/i18n)
builder.Services.AddScoped<ILocalizationService, LocalizationService>();

// Configure authorization
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthenticationStateProvider>());

var host = builder.Build();

// Initialize authentication state provider
var authProvider = host.Services.GetRequiredService<CustomAuthenticationStateProvider>();
await authProvider.InitializeAsync();

// Initialize localization
var localization = host.Services.GetRequiredService<ILocalizationService>();
await localization.InitializeAsync();

await host.RunAsync();
