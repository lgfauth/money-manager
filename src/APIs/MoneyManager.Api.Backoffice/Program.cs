using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MoneyManager.Api.Administration.Models;
using MoneyManager.Api.Administration.Services;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Infrastructure.Observability;
using MoneyManager.Infrastructure.Repositories;
using MoneyManager.Infrastructure.WorkerControl;
using MoneyManager.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new MongoSettings
{
    ConnectionString = builder.Configuration["MongoDB:ConnectionString"]
        ?? Environment.GetEnvironmentVariable("MongoDB__ConnectionString")
        ?? string.Empty,
    DatabaseName = builder.Configuration["MongoDB:DatabaseName"]
        ?? Environment.GetEnvironmentVariable("MongoDB__DatabaseName")
        ?? string.Empty
});
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddSingleton<MongoProcessLogStore>();
builder.Services.AddSingleton<WorkerCommandQueueService>();
builder.Services.AddSingleton<IProcessLogStore>(sp => sp.GetRequiredService<MongoProcessLogStore>());
builder.Services.AddSingleton<IProcessLogHistoryReader>(sp => sp.GetRequiredService<MongoProcessLogStore>());

builder.Services.AddControllers();
builder.Services.AddSingleton<AdminTokenService>();
builder.Services.AddSingleton<AdminAuditService>();
builder.Services.AddSingleton<MoneyManager.Api.Administration.Services.LegalDocumentService>();
builder.Services.AddSingleton<MoneyManager.Api.Administration.Services.DocumentSeeder>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddProcessLogger();

var adminIssuer = builder.Configuration["AdminAuth:Issuer"] ?? "MoneyManager.Admin";
var adminAudience = builder.Configuration["AdminAuth:Audience"] ?? "MoneyManager.Admin.Users";
var adminSecret = Environment.GetEnvironmentVariable("ADMIN_AUTH_SECRET")
    ?? builder.Configuration["AdminAuth:SecretKey"]
    ?? "change-this-admin-secret-key-with-at-least-32-characters";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(adminSecret)),
            ValidateIssuer = true,
            ValidIssuer = adminIssuer,
            ValidateAudience = true,
            ValidAudience = adminAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminPolicies.Viewer, policy =>
        policy.RequireRole(AdminRoles.Viewer, AdminRoles.Operator, AdminRoles.Admin));

    options.AddPolicy(AdminPolicies.Operator, policy =>
        policy.RequireRole(AdminRoles.Operator, AdminRoles.Admin));

    options.AddPolicy(AdminPolicies.Admin, policy =>
        policy.RequireRole(AdminRoles.Admin));
});

var allowedOrigins = builder.Configuration
    .GetSection("AdminCors:AllowedOrigins")
    .Get<string[]>()
    ?? Array.Empty<string>();

var envOriginsRaw = Environment.GetEnvironmentVariable("ADMIN_PORTAL_ALLOWED_ORIGINS");
if (!string.IsNullOrWhiteSpace(envOriginsRaw))
{
    var envOrigins = envOriginsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    allowedOrigins = allowedOrigins.Concat(envOrigins).Distinct().ToArray();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminPortal", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MoneyManager Admin API",
        Version = "v1",
        Description = "API administrativa para operacoes e observabilidade"
    });

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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        if (httpReq.Headers.ContainsKey("X-Forwarded-Proto"))
        {
            swagger.Servers =
            [
                new Microsoft.OpenApi.Models.OpenApiServer { Url = $"https://{httpReq.Host.Value}" }
            ];
        }
    });
});

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MoneyManager Admin API v1");
});

app.UseCors("AdminPortal");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "MoneyManager.Api.Administration",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}));

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

// Seed legal documents on startup
var documentSeeder = app.Services.GetRequiredService<MoneyManager.Api.Administration.Services.DocumentSeeder>();
await documentSeeder.SeedAsync();

app.Run();
