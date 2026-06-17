using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using MoneyManager.Application.Services;
using MoneyManager.Application.Validators;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Infrastructure.Repositories;
using MoneyManager.Infrastructure.Security;
using MoneyManager.Infrastructure.Services.AI;
using MoneyManager.Observability;
using MoneyManager.Presentation.Middlewares;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Configure NLog
builder.Host.UseNLog();

// Configure MongoDB
var mongoSettings = builder.Configuration.GetSection("MongoDB").Get<MongoSettings>() ?? new MongoSettings();

// Valida que a connection string não é o placeholder de exemplo
if (mongoSettings.ConnectionString.Contains("mongo_connection_string"))
    throw new InvalidOperationException("MongoDB ConnectionString não configurada. Defina a variável de ambiente MongoDB__ConnectionString.");

builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton<MongoContext>();

// Register repositories and unit of work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register application services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
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
builder.Services.AddScoped<IUserReportService, UserReportService>();
builder.Services.AddScoped<ICreditCardInvoiceService, CreditCardInvoiceService>();
builder.Services.AddScoped<ICreditCardService, CreditCardService>();
builder.Services.AddScoped<ICreditCardTransactionService, CreditCardTransactionService>();
builder.Services.AddScoped<IFinancialHealthService, FinancialHealthService>();

// Register receipt analysis service
builder.Services.AddHttpClient("anthropic");
builder.Services.AddScoped<IReceiptAnalysisService, AnthropicReceiptAnalysisService>();

// Register VAPID settings and push service
builder.Services.Configure<VapidSettings>(
    builder.Configuration.GetSection(VapidSettings.SectionName));
builder.Services.AddScoped<IPushService, PushService>();

// Register structured process logger
builder.Services.AddProcessLogger();

// Register validators
builder.Services.AddValidatorsFromAssembly(typeof(RegisterRequestValidator).Assembly);

// Rate limiting para endpoints de autenticação (proteção contra brute-force)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", config =>
    {
        config.PermitLimit = 10;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey não configurada. Defina a variável de ambiente Jwt__SecretKey.");

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

        // Lê o JWT do cookie httpOnly quando não há Authorization header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Token))
                    ctx.Token = ctx.Request.Cookies["mm_access_token"];
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var blacklist = ctx.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                var jti = ctx.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                if (jti != null && blacklist.IsRevoked(jti))
                    ctx.Fail("Token revogado");
                return Task.CompletedTask;
            }
        };
    });

// Add controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(
            new MoneyManager.Api.Operational.Extensions.NullableDateTimeConverter());
    });

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

// Add CORS - Origens permitidas via configuração ou variável de ambiente ALLOWED_ORIGIN
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

// Também aceita variável de ambiente ALLOWED_ORIGIN (string simples, suporta múltiplas separadas por vírgula)
var envOrigin = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN");
if (!string.IsNullOrEmpty(envOrigin))
{
    var envOrigins = envOrigin.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    allowedOrigins = allowedOrigins.Concat(envOrigins).Distinct().ToArray();
}

Console.WriteLine($"[MoneyManager API] CORS AllowedOrigins: {string.Join(", ", allowedOrigins)}");

if (allowedOrigins.Length == 0)
{
    Console.WriteLine("[MoneyManager API] WARNING: No CORS origins configured. Set AllowedOrigins in appsettings or ALLOWED_ORIGIN env var.");
}

if (allowedOrigins.Length == 0)
    throw new InvalidOperationException(
        "CORS AllowedOrigins não configurado. Defina ALLOWED_ORIGIN ou AllowedOrigins no appsettings.");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .WithHeaders("Content-Type", "Authorization")
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure for Railway deployment
app.Urls.Add("http://0.0.0.0:8080");

// Use forwarded headers FIRST (before any other middleware)
app.UseForwardedHeaders();

app.UseCors();

// Create MongoDB indexes (with error handling)
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
using (var scope = app.Services.CreateScope())
{
    try
    {
        var mongoContext = scope.ServiceProvider.GetRequiredService<MongoContext>();

        startupLogger.LogInformation("Testing MongoDB connection...");
        await mongoContext.TestConnectionAsync();
        startupLogger.LogInformation("MongoDB connection successful");

        startupLogger.LogInformation("Running MongoDB migrations...");
        await mongoContext.RunMigrationsAsync();
        startupLogger.LogInformation("MongoDB migrations completed");

        startupLogger.LogInformation("Creating MongoDB indexes...");
        await mongoContext.CreateIndexesAsync();
        startupLogger.LogInformation("MongoDB indexes created successfully");
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "MongoDB initialization failed. API started but MongoDB is not accessible");
    }
}

// Enable Swagger apenas em desenvolvimento
if (app.Environment.IsDevelopment())
{
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
}

// RequestLoggingMiddleware envolve toda a pipeline — gera JSON estruturado por request
app.UseMiddleware<RequestLoggingMiddleware>();

// ExceptionHandlingMiddleware DEPOIS do CORS
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Security headers em todas as respostas
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    if (!app.Environment.IsDevelopment())
        context.Response.Headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
else
{
    app.UseHsts();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}));

app.MapGet("/", () => Results.Ok(new { status = "healthy" }));

app.Run();
