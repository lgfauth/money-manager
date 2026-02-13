using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MoneyManager.Application.Services;
using MoneyManager.Application.Validators;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Infrastructure.Repositories;
using MoneyManager.Infrastructure.Security;
using MoneyManager.Presentation.Middlewares;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Configure NLog
builder.Host.UseNLog();

// Configure MongoDB
var mongoSettings = builder.Configuration.GetSection("MongoDB").Get<MongoSettings>() ?? new MongoSettings();
builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton<MongoContext>();

// Register repositories and unit of work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IAccountDeletionService, AccountDeletionService>();
builder.Services.AddScoped<ICreditCardInvoiceService, CreditCardInvoiceService>();
builder.Services.AddScoped<IInvestmentAssetService, InvestmentAssetService>();
builder.Services.AddScoped<IInvestmentTransactionService, InvestmentTransactionService>();
builder.Services.AddScoped<IInvestmentReportService, InvestmentReportService>();

// Market Data Service com HttpClient e Cache
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IMarketDataService, BrapiMarketDataService>();

// Register validators
builder.Services.AddValidatorsFromAssembly(typeof(RegisterRequestValidator).Assembly);

// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? "your-super-secret-key-that-is-long-enough-for-256-bits";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add controllers
builder.Services.AddControllers();

// Configure forwarded headers for Railway (proxy behind HTTPS)
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MoneyManager API",
        Version = "v1",
        Description = "API para gerenciamento financeiro pessoal"
    });

    // Add JWT to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// Add CORS - Configuração permissiva para funcionar
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure for Railway deployment
app.Urls.Add("http://0.0.0.0:8080");

// Use forwarded headers FIRST (before any other middleware)
app.UseForwardedHeaders();

// CORS deve ser o PRIMEIRO middleware depois de ForwardedHeaders
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers["Origin"].FirstOrDefault();
    var method = context.Request.Method;
    var path = context.Request.Path;

    Console.WriteLine($"[CORS] Request: {method} {path} from Origin: {origin ?? "NO ORIGIN"}");

    if (!string.IsNullOrEmpty(origin))
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = origin;
        context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS, PATCH";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, Accept, X-Requested-With";
        context.Response.Headers["Access-Control-Max-Age"] = "86400";

        Console.WriteLine($"[CORS] Added headers for origin: {origin}");
    }

    if (method == "OPTIONS")
    {
        Console.WriteLine("[CORS] Handling OPTIONS (preflight) request - returning 204");
        context.Response.StatusCode = 204;
        await context.Response.CompleteAsync();
        return;
    }

    await next();

    Console.WriteLine($"[CORS] Response status: {context.Response.StatusCode}");
});

// Create MongoDB indexes (with error handling)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var mongoContext = scope.ServiceProvider.GetRequiredService<MongoContext>();

        Console.WriteLine("========================================");
        Console.WriteLine("Testing MongoDB connection...");
        await mongoContext.TestConnectionAsync();
        Console.WriteLine("? MongoDB connection successful!");
        Console.WriteLine("========================================");

        Console.WriteLine("Creating MongoDB indexes...");
        await mongoContext.CreateIndexesAsync();
        Console.WriteLine("? MongoDB indexes created successfully!");
        Console.WriteLine("========================================");
    }
    catch (Exception ex)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("? MongoDB Error:");
        Console.WriteLine($"Message: {ex.Message}");
        Console.WriteLine($"Type: {ex.GetType().Name}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner: {ex.InnerException.Message}");
        }
        Console.WriteLine("========================================");
        Console.WriteLine("? API started but MongoDB is not accessible!");
        Console.WriteLine("========================================");
    }
}

// Enable Swagger in all environments for Railway
app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        if (httpReq.Headers.ContainsKey("X-Forwarded-Proto"))
        {
            swagger.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
            {
                new Microsoft.OpenApi.Models.OpenApiServer { Url = $"https://{httpReq.Host.Value}" }
            };
        }
    });
});

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MoneyManager API v1");
    c.RoutePrefix = string.Empty;
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
});

// ExceptionHandlingMiddleware DEPOIS do CORS
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}));

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
