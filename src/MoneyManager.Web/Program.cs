using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoneyManager.Web.Services;
using MoneyManager.Web.Services.Localization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<MoneyManager.Web.App>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

// Configure HttpClient - URL da API no Railway
var apiUrl = "https://money-manager-api.up.railway.app";

Console.WriteLine($"[MoneyManager] API URL: {apiUrl}");

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
builder.Services.AddScoped<IInvestmentAssetService, InvestmentAssetService>();
builder.Services.AddScoped<IInvestmentTransactionService, InvestmentTransactionService>();

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
