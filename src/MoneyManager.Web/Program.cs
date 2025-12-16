using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using MoneyManager.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<MoneyManager.Web.App>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

// Build temporário para obter JSRuntime
var tempHost = builder.Build();
var jsRuntime = tempHost.Services.GetRequiredService<IJSRuntime>();

// Obter API URL da variável JavaScript injetada no index.html
var apiUrl = "https://localhost:5001"; // Default
try
{
    var configApiUrl = await jsRuntime.InvokeAsync<string>("eval", "window.blazorConfig?.apiUrl || ''");
    if (!string.IsNullOrEmpty(configApiUrl) && configApiUrl != "__API_URL__")
    {
        apiUrl = configApiUrl;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[MoneyManager] Erro ao ler configuração JS: {ex.Message}");
} 

Console.WriteLine($"[MoneyManager] API URL configurada: {apiUrl}");

// Recriar o builder com a configuração correta
builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<MoneyManager.Web.App>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

// Configure HttpClient com base address
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
