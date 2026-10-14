# .NET Developer Guide — MoneyManager

## 1. Repository Overview

The .NET solution (`MoneyManager.sln`) is organized into four project groups under `src/`:

| Project | Type | Role |
|---|---|---|
| `src/APIs/MoneyManager.Api.Operational` | ASP.NET Core Web API (.NET 10) | Main user-facing API; handles auth, transactions, budgets, accounts, credit cards, reports, push notifications, AI receipt analysis |
| `src/APIs/MoneyManager.Api.Backoffice` | ASP.NET Core Web API (.NET 10) | Internal admin API; serves the observability portal (jobs, audit, financial maintenance, legal documents) |
| `src/Supports/MoneyManager.Domain` | Class Library (.NET 10) | Entities, enums, domain exceptions, seeds, repository and unit-of-work interfaces. Only depends on `MongoDB.Bson` (mapping attributes) and `FluentValidation` |
| `src/Supports/MoneyManager.Application` | Class Library (.NET 10) | Application services, DTOs, FluentValidation validators |
| `src/Supports/MoneyManager.Infrastructure` | Class Library (.NET 10) | MongoDB context, migrations and repositories, JWT token service, AES encryption, AI receipt analysis, Banco MCP (Open Finance) client, EfiBank payment gateway, process log store, worker command queue |
| `src/Supports/MoneyManager.Observability` | Class Library (.NET 10) | Structured process logging (`IProcessLogger`, `ProcessLogger`, `IProcessLogStore`) |
| `src/Workers/MoneyManager.Worker.Operational` | .NET Generic Host Worker (.NET 10) | Background jobs: recurring transactions, daily reminders, credit-card invoice status, financial-health snapshots, bank sync (Open Finance), subscription expiration |
| `tests/MoneyManager.Tests` | xUnit Test Project (.NET 10) | Unit tests covering Application services and Presentation controllers |

---

## 2. Architecture

### 2.1 Architectural Pattern

The solution follows **Clean Architecture** with four explicit layers. Each layer depends only on layers below it:

```
Presentation (API / Worker)
        ↓
   Application
        ↓
     Domain
        ↑
  Infrastructure  ←  injects implementations, referenced only by DI root (Program.cs)
```

There is **no CQRS, no MediatR, no AutoMapper** in this codebase.

### 2.2 Layer Responsibilities

| Layer / Project | What belongs here | Examples |
|---|---|---|
| **Domain** `MoneyManager.Domain` | Entities, enums, domain exceptions, repository interfaces, unit-of-work interface | `Transaction`, `User`, `Category`, `IRepository<T>`, `IUnitOfWork`, `ConcurrencyException` |
| **Application** `MoneyManager.Application` | Service interfaces, service implementations, request/response DTOs, FluentValidation validators | `TransactionService`, `ITransactionService`, `CreateTransactionRequestDto`, `TransactionResponseDto`, `CreateTransactionValidator` |
| **Infrastructure** `MoneyManager.Infrastructure` | Concrete repository implementations, MongoDB context, security (JWT), AI integration, push service, observability store | `Repository<T>`, `TransactionRepository`, `UnitOfWork`, `MongoContext`, `TokenService`, `AnthropicReceiptAnalysisService` |
| **Presentation** `MoneyManager.Api.Operational` | Thin controllers, middleware pipeline, error extensions, HttpContext helpers | `TransactionsController`, `ExceptionHandlingMiddleware`, `ControllerBaseErrorExtensions`, `HttpContextExtensions` |
| **Observability** `MoneyManager.Observability` | Structured process logger interfaces and implementations | `IProcessLogger`, `ProcessLogger`, `IProcessLogStore`, `ProcessLogDocument` |

> **Note**: Service interfaces (`ITransactionService`, etc.) are co-located with their implementations inside `MoneyManager.Application/Services/` in the same file. This is the established convention — do not move interfaces to separate files unless explicitly asked.

### 2.3 Dependency Rules

- `Domain` → no references to any other project; only the `MongoDB.Bson` and `FluentValidation` NuGet packages
- `Application` → references `Domain` and `Observability`
- `Infrastructure` → references `Domain`, `Application`, and `Observability`
- `MoneyManager.Api.Operational` (Presentation) → references `Application`, `Infrastructure`, `Observability`, `Domain`
- `MoneyManager.Worker.Operational` → references `Application`, `Infrastructure`, `Observability`, `Domain`
- **Infrastructure is never referenced by Application or Domain** — only by Presentation and Workers via DI

### 2.4 CQRS Pattern

Not applicable — not used in this codebase.

### 2.5 Result / Error Handling Pattern

This codebase uses **exception-based error signaling**, not a Union/Result type. Specific exception types map to HTTP status codes through `ExceptionHandlingMiddleware`:

```csharp
private static int DetermineStatusCode(Exception exception) => exception switch
{
    KeyNotFoundException        => StatusCodes.Status404NotFound,
    InvalidOperationException   => StatusCodes.Status400BadRequest,
    BankMcpKeyExpiredException  => StatusCodes.Status401Unauthorized,
    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
    ConcurrencyException        => StatusCodes.Status409Conflict,
    PremiumRequiredException    => StatusCodes.Status403Forbidden,
    _                           => StatusCodes.Status500InternalServerError
};
```

Services throw these exceptions; controllers do not need a try/catch for domain errors (the middleware handles them). However, controllers do catch exceptions explicitly in several places for finer-grained responses via the `ControllerBaseErrorExtensions` helpers:

```csharp
// From TransactionsController
catch (KeyNotFoundException)
{
    return this.ApiNotFound();
}
catch (Exception ex)
{
    return this.ApiBadRequest(ex.Message);
}
```

All error responses are shaped by `ApiErrorResponseFactory` into `ApiErrorResponse`:

```csharp
// MoneyManager.Api.Operational/Models/ApiErrorResponse.cs
public sealed class ApiErrorResponse
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<string> Errors { get; init; } = [];
    public string TraceId { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string? Details { get; init; }  // only in Development
}
```

---

## 3. How to Build New Features

### 3.1 Adding a New API Endpoint

> The examples in sections 3.1–3.7 use a **hypothetical `SavingsGoal` feature** to illustrate the pattern. It does not exist in the codebase — check real files (e.g. `TransactionService`, `TransactionsController`) for the current shape of each piece.

**Step 1 — Define the Request DTO** in `MoneyManager.Application/DTOs/Request/`:

```csharp
// MoneyManager.Application/DTOs/Request/CreateSavingsGoalRequestDto.cs
namespace MoneyManager.Application.DTOs.Request;

public class CreateSavingsGoalRequestDto
{
    public string CategoryId { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}
```

**Step 2 — Define the Response DTO** in `MoneyManager.Application/DTOs/Response/`:

```csharp
// MoneyManager.Application/DTOs/Response/SavingsGoalResponseDto.cs
namespace MoneyManager.Application.DTOs.Response;

public class SavingsGoalResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}
```

**Step 3 — Add the Validator** in `MoneyManager.Application/Validators/`:

```csharp
// MoneyManager.Application/Validators/CreateSavingsGoalValidator.cs
using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class CreateSavingsGoalValidator : AbstractValidator<CreateSavingsGoalRequestDto>
{
    public CreateSavingsGoalValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category ID is required");
        RuleFor(x => x.Limit).GreaterThan(0).WithMessage("Limit must be greater than 0");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12");
    }
}
```

**Step 4 — Add the Service** in `MoneyManager.Application/Services/`. Interface and implementation go in the same file:

```csharp
// MoneyManager.Application/Services/SavingsGoalService.cs
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public interface ISavingsGoalService
{
    Task<SavingsGoalResponseDto> CreateAsync(string userId, CreateSavingsGoalRequestDto request);
}

public class SavingsGoalService : ISavingsGoalService
{
    private readonly IUnitOfWork _unitOfWork;

    public SavingsGoalService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SavingsGoalResponseDto> CreateAsync(string userId, CreateSavingsGoalRequestDto request)
    {
        var goal = new SavingsGoal
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Limit = request.Limit,
            Year = request.Year,
            Month = request.Month
        };

        await _unitOfWork.SavingsGoals.AddAsync(goal);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(goal);
    }

    private static SavingsGoalResponseDto MapToDto(SavingsGoal g) => new()
    {
        Id = g.Id,
        CategoryId = g.CategoryId,
        Limit = g.Limit,
        Year = g.Year,
        Month = g.Month
    };
}
```

**Step 5 — Add the Controller** in `MoneyManager.Api.Operational/Controllers/`:

```csharp
// MoneyManager.Api.Operational/Controllers/SavingsGoalsController.cs
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SavingsGoalsController : ControllerBase
{
    private readonly ISavingsGoalService _savingsGoalService;
    private readonly IValidator<CreateSavingsGoalRequestDto> _validator;
    private readonly ILogger<SavingsGoalsController> _logger;

    public SavingsGoalsController(
        ISavingsGoalService savingsGoalService,
        IValidator<CreateSavingsGoalRequestDto> validator,
        ILogger<SavingsGoalsController> logger)
    {
        _savingsGoalService = savingsGoalService;
        _validator = validator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSavingsGoalRequestDto request)
    {
        var userId = HttpContext.GetUserId();

        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _savingsGoalService.CreateAsync(userId, request);
            _logger.LogInformation("Meta {SavingsGoalId} criada para o usuário {UserId}", result.Id, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar meta para o usuário {UserId}", userId);
            return this.ApiBadRequest(ex.Message);
        }
    }
}
```

**Step 6 — Register in `Program.cs`**:

```csharp
// In Program.cs — follow the existing registration block
builder.Services.AddScoped<ISavingsGoalService, SavingsGoalService>();
// Validators are auto-registered:
// builder.Services.AddValidatorsFromAssembly(typeof(RegisterRequestValidator).Assembly);
// No need to register individual validators manually.
```

### 3.2 Integrating an External API

The existing pattern uses `IHttpClientFactory` with a named client. Example from `AnthropicReceiptAnalysisService`:

**Step 1 — Register the named `HttpClient` in `Program.cs`**:

```csharp
builder.Services.AddHttpClient("myExternalService");
```

**Step 2 — Create the service class** in `MoneyManager.Infrastructure/Services/`:

```csharp
// MoneyManager.Infrastructure/Services/MyExternalService.cs
using System.Net.Http.Json;
using MoneyManager.Application.Services;

namespace MoneyManager.Infrastructure.Services;

public class MyExternalService : IMyExternalService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public MyExternalService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("myExternalService");
        _apiKey = configuration["MyService:ApiKey"] ?? string.Empty;
    }

    public async Task<MyResponseDto> CallAsync(string input)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/endpoint");
        request.Headers.Add("x-api-key", _apiKey);
        request.Content = JsonContent.Create(new { input });

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<MyResponseDto>()
            ?? throw new InvalidOperationException("Empty response from external service");
    }
}
```

**Step 3 — Register in `Program.cs`**:

```csharp
builder.Services.AddScoped<IMyExternalService, MyExternalService>();
```

**Step 4 — Add the configuration key to `appsettings.json`**:

```json
"MyService": {
  "ApiKey": ""
}
```

### 3.3 Database Access

All database access goes through `IUnitOfWork`. For simple entities, the generic `Repository<T>` is sufficient. For queries beyond what the base repository provides, create a specialized repository.

**Adding a simple query to an existing repository** — inherit from `Repository<T>` and override or add methods:

```csharp
// MoneyManager.Infrastructure/Repositories/SavingsGoalRepository.cs
using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class SavingsGoalRepository : Repository<SavingsGoal>, ISavingsGoalRepository
{
    public SavingsGoalRepository(MongoContext context) : base(context, "savings_goals") { }

    public async Task<IEnumerable<SavingsGoal>> GetByUserAndMonthAsync(string userId, int year, int month)
    {
        var filter = Builders<SavingsGoal>.Filter.And(
            Builders<SavingsGoal>.Filter.Eq(g => g.UserId, userId),
            Builders<SavingsGoal>.Filter.Eq(g => g.Year, year),
            Builders<SavingsGoal>.Filter.Eq(g => g.Month, month),
            Builders<SavingsGoal>.Filter.Eq(g => g.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }
}
```

**Always filter by `UserId` and `IsDeleted = false`** in every user-scoped query.

The `UnitOfWork` uses **lazy initialization** — add new repositories there:

```csharp
// In UnitOfWork.cs
private ISavingsGoalRepository? _savingsGoalRepository;
public ISavingsGoalRepository SavingsGoals => _savingsGoalRepository ??= new SavingsGoalRepository(_context);
```

### 3.4 Adding a New Worker or Job

Workers are `BackgroundService` implementations hosted in `MoneyManager.Worker.Operational`. The pattern is: one `*Worker.cs` (the `BackgroundService` loop) + one `*Processor.cs` (the actual business logic).

**Step 1 — Create the Processor**:

```csharp
// MoneyManager.Worker.Operational/WorkerHost/Services/MyJobProcessor.cs
using MoneyManager.Application.Services;
using MoneyManager.Observability;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class MyJobProcessor(
    IProcessLogger processLogger,
    IMyService myService) 
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        processLogger.AddStep("Iniciando processamento do job");

        await myService.DoWorkAsync();

        processLogger.AddStep("Processamento finalizado");
    }
}
```

**Step 2 — Create the Worker**:

```csharp
// MoneyManager.Worker.Operational/WorkerHost/Services/MyJobWorker.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionSchedulerWorker.WorkerHost.Options;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class MyJobWorker(
    ILogger<MyJobWorker> logger,
    IOptions<ScheduleOptions> scheduleOptions,
    ITimeProvider timeProvider,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly ScheduleOptions _schedule = scheduleOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Check if it is time to run based on _schedule.Hour / _schedule.Minute
            // (follow the same time-zone-aware pattern from ScheduledTransactionWorker)

            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<MyJobProcessor>();
            await processor.ProcessAsync(stoppingToken);

            await timeProvider.Delay(
                TimeSpan.FromSeconds(_schedule.LoopDelaySeconds), stoppingToken);
        }
    }
}
```

**Step 3 — Register in `ServiceCollectionExtensions.cs`**:

```csharp
services.AddScoped<MyJobProcessor>();
services.AddHostedService<MyJobWorker>();
```

**Step 4 — Add schedule options**: each worker has its own `*ScheduleOptions` class in `WorkerHost/Options/` with a `SectionName` constant (e.g. `DailyReminderScheduleOptions`), bound in `ServiceCollectionExtensions` with `.ValidateOnStart()`, plus the matching section in the Worker's `appsettings.json`.

> Real workers also receive `WorkerCommandQueueService` to honour `run-now`/pause commands queued by the backoffice, and their processors open each execution with `IProcessLogger.Start("<ProcessName>")` so it shows up in `worker_process_logs`. Follow `ScheduledTransactionWorker` / `DailyReminderWorker` for the complete loop.

### 3.5 Adding a New Domain Entity

```csharp
// MoneyManager.Domain/Entities/MyEntity.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoneyManager.Domain.Entities;

[BsonIgnoreExtraElements]
public class MyEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

Rules:
- Every entity that belongs to a user must have `UserId`
- Every entity must have `IsDeleted` for soft delete
- Use `[BsonElement("camelCase")]` for all fields
- Use `[BsonIgnoreExtraElements]` on the class
- The `Id` must use `[BsonId]` + `[BsonRepresentation(BsonType.ObjectId)]`

### 3.6 Adding Validation

Validators live in `MoneyManager.Application/Validators/` and are auto-discovered by the assembly scan in `Program.cs`. Create a class extending `AbstractValidator<TRequest>`:

```csharp
// MoneyManager.Application/Validators/CreateMyEntityValidator.cs
using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class CreateMyEntityValidator : AbstractValidator<CreateMyEntityRequestDto>
{
    public CreateMyEntityValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
    }
}
```

In the controller, invoke the validator explicitly — **do not rely on `[ApiController]` automatic model validation**:

```csharp
var validation = await _validator.ValidateAsync(request);
if (!validation.IsValid)
    return this.ApiValidationError(validation.Errors);
```

### 3.7 Logging and Observability

**Structured logging with `ILogger<T>`** (NLog):

```csharp
_logger.LogInformation("Meta {SavingsGoalId} criada para o usuário {UserId}", result.Id, userId);
_logger.LogWarning("Limite da meta excedido para o usuário {UserId}, categoria {CategoryId}", userId, categoryId);
_logger.LogError(ex, "Erro ao criar meta para o usuário {UserId}", userId);
```

**Process logging with `IProcessLogger`** — use this for multi-step business operations to produce a structured JSON trace:

```csharp
public class SavingsGoalService : ISavingsGoalService
{
    private readonly IProcessLogger _processLogger;

    public async Task<SavingsGoalResponseDto> CreateAsync(string userId, CreateSavingsGoalRequestDto request)
    {
        _processLogger.AddStep("Criando meta", new Dictionary<string, object?>
        {
            ["userId"] = userId,
            ["categoryId"] = request.CategoryId
        });

        // ... business logic ...

        _processLogger.AddStep("Meta criada", new Dictionary<string, object?> { ["goalId"] = goal.Id });
        return MapToDto(goal);
    }
}
```

`IProcessLogger` is scoped — it is started by the outer scope (controller or worker) and finished at the end of the request. For a new service that should produce a process trace, add `IProcessLogger` as a constructor dependency. It is registered via `services.AddProcessLogger()` which is already present in `Program.cs`.

### 3.8 Dependency Injection Registration

All DI registrations are in `Program.cs` for the APIs. Workers use `ServiceCollectionExtensions.AddWorkerHost()`. Register services by following the existing grouping:

```csharp
// Program.cs — add alongside the existing service registrations

// Singletons (stateless, cross-request shared instances)
builder.Services.AddSingleton<IMyStatelessService, MyStatelessService>();

// Scoped (one instance per HTTP request)
builder.Services.AddScoped<IMyService, MyService>();

// Transient (a new instance every time requested — avoid unless necessary)
builder.Services.AddTransient<IMyTransientHelper, MyTransientHelper>();
```

Always register the **interface → concrete type** pair. Never call `new MyService()` inside application code.

---

## 4. Code Conventions

### 4.1 Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes | PascalCase | `TransactionService`, `MongoContext` |
| Interfaces | `I` prefix + PascalCase | `ITransactionService`, `IUnitOfWork` |
| Methods | PascalCase | `CreateAsync`, `GetByIdAsync` |
| Properties | PascalCase | `UserId`, `IsDeleted` |
| Parameters / local vars | camelCase | `userId`, `request`, `result` |
| Private fields | `_camelCase` | `_unitOfWork`, `_logger` |
| Constants | PascalCase | `SectionName` |
| Request DTOs | `*RequestDto` | `CreateTransactionRequestDto` |
| Response DTOs | `*ResponseDto` | `TransactionResponseDto` |
| Validators | `*Validator` | `CreateTransactionValidator` |
| Services | `*Service` | `TransactionService` |
| Repositories | `*Repository` | `TransactionRepository` |
| Controllers | `*Controller` | `TransactionsController` |
| Worker classes | `*Worker` | `ScheduledTransactionWorker` |
| Processor classes | `*Processor` | `RecurringTransactionsProcessor` |
| Log messages | **português** | `"Processando recorrências vencidas"` |
| Comments | **português** | `// Valida que a connection string não é o placeholder` |

### 4.2 File and Folder Organization

| File type | Location |
|---|---|
| Domain entities | `MoneyManager.Domain/Entities/` |
| Domain enums | `MoneyManager.Domain/Enums/` |
| Domain exceptions | `MoneyManager.Domain/Exceptions/` |
| Repository interfaces | `MoneyManager.Domain/Interfaces/` |
| Service interface + implementation | `MoneyManager.Application/Services/` (same file) |
| Request DTOs | `MoneyManager.Application/DTOs/Request/` |
| Response DTOs | `MoneyManager.Application/DTOs/Response/` |
| Validators | `MoneyManager.Application/Validators/` |
| Repository implementations | `MoneyManager.Infrastructure/Repositories/` |
| MongoDB context/settings | `MoneyManager.Infrastructure/Data/` |
| External service integrations | `MoneyManager.Infrastructure/Services/` |
| Controllers | `MoneyManager.Api.Operational/Controllers/` |
| Middleware | `MoneyManager.Api.Operational/Middlewares/` |
| Controller extension methods | `MoneyManager.Api.Operational/Extensions/` |

### 4.3 DTO and Mapping Conventions

There is no AutoMapper. Mapping is done with **private static `MapToDto` methods** inside the service class:

```csharp
private static CategoryResponseDto MapToDto(Category c) => new()
{
    Id = c.Id,
    Name = c.Name,
    Type = c.Type,
    Color = c.Color
};
```

For mappings that require async lookups (e.g., resolving account name from id), use `private async Task<TDto> MapToDtoAsync(...)`.

Request and response DTOs are plain classes (not records) with standard `{ get; set; }` properties.

### 4.4 Configuration and Secrets

Configuration keys are read in `Program.cs` or in service constructors via `IConfiguration`:

```csharp
// Constructor injection pattern (from TokenService)
public TokenService(IConfiguration configuration)
{
    var jwtSettings = configuration.GetSection("Jwt");
    _secretKey = jwtSettings["SecretKey"]
        ?? throw new InvalidOperationException("JWT SecretKey não configurada.");
}
```

For strongly-typed options (worker schedules), use the Options pattern:

```csharp
// Registration
services.AddOptions<ScheduleOptions>()
    .Bind(configuration.GetSection(ScheduleOptions.SectionName))
    .ValidateOnStart();

// Consumption
public MyWorker(IOptions<ScheduleOptions> scheduleOptions)
{
    _schedule = scheduleOptions.Value;
}
```

Secrets are **never hardcoded**. In `appsettings.json` they are left as placeholder strings (e.g., `"strong_text_for_secret_here"`). Real values are injected via environment variables in production (Railway convention: `Jwt__SecretKey` maps to `Jwt:SecretKey`).

---

## 5. Good Practices Observed in This Codebase

- **User isolation on every query**: Every repository query that operates on user data includes a `UserId` filter. The pattern `account.UserId != userId` is checked even after fetching by ID.

- **Soft delete for regular CRUD**: User entities are deleted by setting `IsDeleted = true`. Physical deletes are intentional and limited to the LGPD account-deletion flow (`AccountDeletionService`, via `DeleteManyByUserIdAsync`) and to unlinking a bank connection.

- **Optimistic concurrency**: `Repository<T>.UpdateAsync` checks a `version` field and increments it. If the document was updated concurrently, it throws `ConcurrencyException` (→ HTTP 409).

- **Idempotent transaction creation**: `Transaction.ClientRequestId` allows the client to send a unique key per creation attempt. If the same key is seen again, the existing transaction is returned without creating a duplicate.

- **Rate limiting on auth endpoints**: Fixed-window limiter (10 req/min) applied to the `auth` policy on login/register endpoints.

- **Token blacklist for logout**: `ITokenBlacklistService` (in-memory) tracks revoked JTIs so logout is effective within the token's remaining lifetime.

- **HttpClient from factory**: The Anthropic service uses `IHttpClientFactory.CreateClient("anthropic")` — never `new HttpClient()`.

- **Process logging for multi-step operations**: `IProcessLogger` is injected into services that perform complex workflows (e.g., `TransactionService`, `AuthService`). Logs are structured JSON with elapsed-time steps.

- **NLog structured logging**: Log messages use structured parameters (`{UserId}`, `{SavingsGoalId}`) rather than string interpolation so the log provider can index them.

- **Centralized error response shape**: All errors — whether from middleware or from explicit controller returns — go through `ApiErrorResponseFactory` and produce the same `ApiErrorResponse` structure with `StatusCode`, `Message`, `Errors[]`, `TraceId`, `Path`, and `TimestampUtc`.

- **Validation in Application, not Services**: `FluentValidation` validators are in `MoneyManager.Application/Validators/` and invoked at the controller boundary before calling the service.

- **Options pattern with `ValidateOnStart()`**: Worker schedule options use `AddOptions<T>().Bind(...).ValidateOnStart()` so misconfigured deployments fail fast at startup.

---

## 6. What NOT to Do

### Business logic in controllers

```csharp
// WRONG — controller performs business logic
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateTransactionRequestDto request)
{
    var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
    if (account == null) return NotFound();
    var tx = new Transaction { /* ... */ };
    await _unitOfWork.Transactions.AddAsync(tx);
    return Ok(tx);
}

// CORRECT — controller delegates to service
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateTransactionRequestDto request)
{
    var validation = await _validator.ValidateAsync(request);
    if (!validation.IsValid) return this.ApiValidationError(validation.Errors);

    var result = await _transactionService.CreateAsync(HttpContext.GetUserId(), request);
    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
}
```

### Direct MongoDB queries outside repositories

```csharp
// WRONG — bypasses repository pattern
var col = _mongoContext.GetCollection<Transaction>("transactions");
var filter = Builders<Transaction>.Filter.Eq(t => t.UserId, userId);
var result = await col.Find(filter).ToListAsync();

// CORRECT — use IUnitOfWork
var result = await _unitOfWork.Transactions.GetByUserIdAsync(userId);
```

### Validation inside services

```csharp
// WRONG — validation in service
public async Task<CategoryResponseDto> CreateAsync(string userId, CreateCategoryRequestDto request)
{
    if (string.IsNullOrEmpty(request.Name))
        throw new ArgumentException("Name required");
    // ...
}

// CORRECT — validation in the validator class (Application/Validators/) and invoked in controller
```

### Instantiating services with `new`

```csharp
// WRONG
public class TransactionsController
{
    private readonly ITransactionService _service = new TransactionService(/* ... */);
}

// CORRECT — constructor injection
public TransactionsController(ITransactionService transactionService)
{
    _transactionService = transactionService;
}
```

### Referencing Infrastructure from Application or Domain

```csharp
// WRONG — Application depending on Infrastructure
// MoneyManager.Application/Services/CategoryService.cs
using MoneyManager.Infrastructure.Repositories; // FORBIDDEN

// CORRECT — Application depends only on Domain interfaces
using MoneyManager.Domain.Interfaces; // IRepository<Category> from IUnitOfWork
```

### Physical deletes instead of soft deletes

```csharp
// WRONG
await _unitOfWork.Categories.DeleteAsync(id);

// CORRECT
var category = await _unitOfWork.Categories.GetByIdAsync(id);
category.IsDeleted = true;
await _unitOfWork.Categories.UpdateAsync(category);
```

### Using `.Result` or `.Wait()` on async calls

```csharp
// WRONG
var result = _transactionService.CreateAsync(userId, request).Result;

// CORRECT
var result = await _transactionService.CreateAsync(userId, request);
```

### Ignoring `UserId` isolation in queries

```csharp
// WRONG — returns any user's category
var category = await _unitOfWork.Categories.GetByIdAsync(id);
return MapToDto(category);

// CORRECT — validates ownership
var category = await _unitOfWork.Categories.GetByIdAsync(id);
if (category == null || category.UserId != userId || category.IsDeleted)
    throw new KeyNotFoundException("Category not found");
return MapToDto(category);
```

---

## 7. .NET Version Notes

This codebase targets **net10.0** for all projects (some `Microsoft.*` packages are still on the 9.0.x line). The following modern C#/ASP.NET Core features are in use:

- **Primary constructors** (C# 12) — used in Worker services:
  ```csharp
  internal sealed class ScheduledTransactionWorker(
      ILogger<ScheduledTransactionWorker> logger,
      IOptions<WorkerOptions> options,
      IOptions<ScheduleOptions> scheduleOptions,
      ITimeProvider timeProvider,
      WorkerCommandQueueService commandQueue,
      IServiceScopeFactory scopeFactory) : BackgroundService
  ```

- **Collection expressions** (C# 12) — used throughout:
  ```csharp
  public List<string> Tags { get; set; } = [];
  List<string> errors = [];
  ```

- **`switch` expression for status codes** — used in `ExceptionHandlingMiddleware` and `ApiErrorResponseFactory`

- **`JsonStringEnumConverter`** — registered globally in `Program.cs` so enum values are serialized as strings in JSON responses

- **`ClockSkew = TimeSpan.Zero`** on JWT validation — tokens expire exactly at their `exp` claim with no tolerance buffer

- **`ForwardedHeaders`** middleware configured for Railway (proxy-behind-HTTPS deployment)

No .NET 10–specific APIs are in use at the time of writing; the features above are available since C# 12 / .NET 8.
