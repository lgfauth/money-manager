using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using MoneyManager.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<MoneyManager.Web.App>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

// Register Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Register API configuration service
builder.Services.AddScoped<IApiConfigService, ApiConfigService>();

// Configure HttpClient factory that uses API config
builder.Services.AddScoped(sp => 
{
    var apiConfigService = sp.GetRequiredService<IApiConfigService>();
    var apiUrl = apiConfigService.GetApiUrlAsync().GetAwaiter().GetResult();
    var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl) };
    Console.WriteLine($"[HttpClient] Configured with base address: {apiUrl}");
    return httpClient;
});

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

// Configure authorization
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthenticationStateProvider>());

var host = builder.Build();

// Initialize authentication state provider
var authProvider = host.Services.GetRequiredService<CustomAuthenticationStateProvider>();
await authProvider.InitializeAsync();

await host.RunAsync();
