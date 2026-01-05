using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using MoneyManager.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using MoneyManager.Web.Services.Localization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<MoneyManager.Web.App>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

// Configure HttpClient - URL da API no Railway
var apiUrl = "https://money-manager-api.up.railway.app";

Console.WriteLine($"[MoneyManager] API URL: {apiUrl}");

// Register AuthorizationMessageHandler
builder.Services.AddScoped<AuthorizationMessageHandler>();

// Configure HttpClient with AuthorizationMessageHandler
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
builder.Services.AddScoped<MoneyManager.Web.Services.IUserSettingsService, MoneyManager.Web.Services.UserSettingsService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<MoneyManager.Web.Services.IOnboardingService, MoneyManager.Web.Services.OnboardingService>();
builder.Services.AddScoped<MoneyManager.Web.Services.IAccountDeletionService, MoneyManager.Web.Services.AccountDeletionService>();

// Localization (JSON in wwwroot/i18n)
builder.Services.AddScoped<ILocalizationService, MoneyManager.Web.Services.Localization.LocalizationService>();

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
