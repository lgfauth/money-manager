using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using MoneyManager.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<MoneyManager.Web.App>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

// Configure HttpClient - Ler URL da API do appsettings.json
var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

string apiUrl;
try
{
    // Tenta ler do appsettings.json
    var config = await httpClient.GetFromJsonAsync<Dictionary<string, string>>("appsettings.json");
    apiUrl = config?["ApiUrl"] ?? "https://money-manager-api.up.railway.app";
    Console.WriteLine($"[MoneyManager] API URL loaded from config: {apiUrl}");
}
catch (Exception ex)
{
    // Fallback se não conseguir ler
    apiUrl = "https://money-manager-api.up.railway.app";
    Console.WriteLine($"[MoneyManager] Failed to load config, using default API URL: {apiUrl}");
    Console.WriteLine($"[MoneyManager] Error: {ex.Message}");
}

builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiUrl) 
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

// Configure authorization
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthenticationStateProvider>());

var host = builder.Build();

// Initialize authentication state provider
var authProvider = host.Services.GetRequiredService<CustomAuthenticationStateProvider>();
await authProvider.InitializeAsync();

await host.RunAsync();
