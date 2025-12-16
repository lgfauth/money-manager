using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using MoneyManager.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<MoneyManager.Web.App>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

// Carregar configuração do appsettings.json
var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var apiUrl = "https://localhost:5001"; // Default

try 
{
    // Tentar carregar appsettings.Production.json primeiro (para produção)
    var productionConfig = await http.GetFromJsonAsync<Dictionary<string, string>>("appsettings.Production.json");
    if (productionConfig != null && productionConfig.ContainsKey("ApiUrl"))
    {
        apiUrl = productionConfig["ApiUrl"];
    }
}
catch 
{
    try 
    {
        // Fallback para appsettings.json
        var config = await http.GetFromJsonAsync<Dictionary<string, string>>("appsettings.json");
        if (config != null && config.ContainsKey("ApiUrl"))
        {
            apiUrl = config["ApiUrl"];
        }
    }
    catch 
    {
        // Usar valor padrão
    }
}

Console.WriteLine($"API URL configurada: {apiUrl}");

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
