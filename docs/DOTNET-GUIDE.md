# .NET Developer Guide — MoneyManager

## 1. Repository Overview

The .NET solution (`MoneyManager.sln`) is organized into four project groups under `src/`:

| Project | Type | Role |
|---|---|---|
| `src/APIs/MoneyManager.Api.Operational` | ASP.NET Core Web API (.NET 10) | Main user-facing API; handles auth, transactions, budgets, accounts, credit cards, reports, push notifications, AI receipt analysis |
| `src/APIs/MoneyManager.Api.Backoffice` | ASP.NET Core Web API (.NET 10) | Internal admin API; serves the observability portal (jobs, audit, financial maintenance, legal documents) |
| `src/Supports/MoneyManager.Domain` | Class Library (.NET 10) | Entities, enums, domain exceptions, repository and unit-of-work interfaces. Zero external dependencies |
| `src/Supports/MoneyManager.Application` | Class Library (.NET 10) | Application services, DTOs, FluentValidation validators |
| `src/Supports/MoneyManager.Infrastructure` | Class Library (.NET 10) | MongoDB repositories, JWT token service, AI service, push notification service, process log store, worker command queue |
| `src/Supports/MoneyManager.Observability` | Class Library (.NET 10) | Structured process logging (`IProcessLogger`, `ProcessLogger`, `IProcessLogStore`) |
| `src/Workers/MoneyManager.Worker.Operational` | .NET Generic Host Worker (.NET 10) | Background jobs: recurring transaction processing, daily reminders, credit-card invoice closure |
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

- `Domain` → no references to any other project or NuGet package
- `Application` → references `Domain` and `Observability`
- `Infrastructure` → references `Domain`, `Application`, and `Observability`
- `MoneyManager.Api.Operational` (Presentation) → references `Application`, `Infrastructure`, `Observability`, `Domain`
- `MoneyManager.Worker.Operational` → references `Application`, `Infrastructure`, `Observability`
- **Infrastructure is never referenced by Application or Domain** — only by Presentation and Workers via DI

### 2.4 CQRS Pattern

Not applicable — not used in this codebase.

### 2.5 Result / Error Handling Pattern

This codebase uses **exception-based error signaling**, not a Union/Result type. Specific exception types map to HTTP status codes through `ExceptionHandlingMiddleware`:

```csharp
private static int DetermineStatusCode(Exception exception) => exception switch
{
    KeyNotFoundException       => StatusCodes.Status404NotFound,
    InvalidOperationException  => StatusCodes.Status400BadRequest,
    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
    ConcurrencyException       => StatusCodes.Status409Conflict,
    _                          => StatusCodes.Status500InternalServerError
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
public class ApiErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; }
    public string TraceId { get; set; }
    public string Path { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string? Details { get; set; }  // only in Development
}
```

---

## 3. How to Build New Features

### 3.1 Adding a New API Endpoint

**Step 1 — Define the Request DTO** in `MoneyManager.Application/DTOs/Request/`:

```csharp
// MoneyManager.Application/DTOs/Request/CreateBudgetRequestDto.cs
namespace MoneyManager.Application.DTOs.Request;

public class CreateBudgetRequestDto
{
    public string CategoryId { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}
```

**Step 2 — Define the Response DTO** in `MoneyManager.Application/DTOs/Response/`:

```csharp
// MoneyManager.Application/DTOs/Response/BudgetResponseDto.cs
namespace MoneyManager.Application.DTOs.Response;

public class BudgetResponseDto
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
// MoneyManager.Application/Validators/CreateBudgetValidator.cs
using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class CreateBudgetValidator : AbstractValidator<CreateBudgetRequestDto>
{
    public CreateBudgetValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category ID is required");
        RuleFor(x => x.Limit).GreaterThan(0).WithMessage("Limit must be greater than 0");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12");
    }
}
```

**Step 4 — Add the Service** in `MoneyManager.Application/Services/`. Interface and implementation go in the same file:

```csharp
// MoneyManager.Application/Services/BudgetService.cs
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public interface IBudgetService
{
    Task<BudgetResponseDto> CreateAsync(string userId, CreateBudgetRequestDto request);
}

public class BudgetService : IBudgetService
{
    private readonly IUnitOfWork _unitOfWork;

    public BudgetService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BudgetResponseDto> CreateAsync(string userId, CreateBudgetRequestDto request)
    {
        var budget = new Budget
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Limit = request.Limit,
            Year = request.Year,
            Month = request.Month
        };

        await _unitOfWork.Budgets.AddAsync(budget);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(budget);
    }

    private static BudgetResponseDto MapToDto(Budget b) => new()
    {
        Id = b.Id,
        CategoryId = b.CategoryId,
        Limit = b.Limit,
        Year = b.Year,
        Month = b.Month
    };
}
```

**Step 5 — Add the Controller** in `MoneyManager.Api.Operational/Controllers/`:

```csharp
// MoneyManager.Api.Operational/Controllers/BudgetsController.cs
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
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    private readonly IValidator<CreateBudgetRequestDto> _validator;
    private readonly ILogger<BudgetsController> _logger;

    public BudgetsController(
        IBudgetService budgetService,
        IValidator<CreateBudgetRequestDto> validator,
        ILogger<BudgetsController> logger)
    {
        _budgetService = budgetService;
        _validator = validator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequestDto request)
    {
        var userId = HttpContext.GetUserId();

        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _budgetService.CreateAsync(userId, request);
            _logger.LogInformation("Budget {BudgetId} created for user {UserId}", result.Id, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating budget for user {UserId}", userId);
            return this.ApiBadRequest(ex.Message);
        }
    }
}
```

**Step 6 — Register in `Program.cs`**:

```csharp
// In Program.cs — follow the existing registration block
builder.Services.AddScoped<IBudgetService, BudgetService>();
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
// MoneyManager.Infrastructure/Repositories/BudgetRepository.cs
using MongoDB.Driver;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class BudgetRepository : Repository<Budget>, IBudgetRepository
{
    public BudgetRepository(MongoContext context) : base(context, "budgets") { }

    public async Task<IEnumerable<Budget>> GetByUserAndMonthAsync(string userId, int year, int month)
    {
        var filter = Builders<Budget>.Filter.And(
            Builders<Budget>.Filter.Eq(b => b.UserId, userId),
            Builders<Budget>.Filter.Eq(b => b.Year, year),
            Builders<Budget>.Filter.Eq(b => b.Month, month),
            Builders<Budget>.Filter.Eq(b => b.IsDeleted, false)
        );

        return await Collection.Find(filter).ToListAsync();
    }
}
```

**Always filter by `UserId` and `IsDeleted = false`** in every user-scoped query.

The `UnitOfWork` uses **lazy initialization** — add new repositories there:

```csharp
// In UnitOfWork.cs
private IBudgetRepository? _budgetRepository;
public IBudgetRepository Budgets => _budgetRepository ??= new BudgetRepository(_context);
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

**Step 4 — Add schedule options to `appsettings.json`** using the `Schedule` section conventions.

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
_logger.LogInformation("Budget {BudgetId} created for user {UserId}", result.Id, userId);
_logger.LogWarning("Budget limit exceeded for user {UserId}, category {CategoryId}", userId, categoryId);
_logger.LogError(ex, "Error creating budget for user {UserId}", userId);
```

**Process logging with `IProcessLogger`** — use this for multi-step business operations to produce a structured JSON trace:

```csharp
public class BudgetService : IBudgetService
{
    private readonly IProcessLogger _processLogger;

    public async Task<BudgetResponseDto> CreateAsync(string userId, CreateBudgetRequestDto request)
    {
        _processLogger.AddStep("Creating budget", new Dictionary<string, object?>
        {
            ["userId"] = userId,
            ["categoryId"] = request.CategoryId
        });

        // ... business logic ...

        _processLogger.AddStep("Budget created", new Dictionary<string, object?> { ["budgetId"] = budget.Id });
        return MapToDto(budget);
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

Request DTOs have public init-style setters (not records). Response DTOs use standard public setters.

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

- **Soft delete everywhere**: All entities have `IsDeleted = true` as the delete mechanism. No physical deletes for user data.

- **Optimistic concurrency**: `Repository<T>.UpdateAsync` checks a `version` field and increments it. If the document was updated concurrently, it throws `ConcurrencyException` (→ HTTP 409).

- **Idempotent transaction creation**: `Transaction.ClientRequestId` allows the client to send a unique key per creation attempt. If the same key is seen again, the existing transaction is returned without creating a duplicate.

- **Rate limiting on auth endpoints**: Fixed-window limiter (10 req/min) applied to the `auth` policy on login/register endpoints.

- **Token blacklist for logout**: `ITokenBlacklistService` (in-memory) tracks revoked JTIs so logout is effective within the token's remaining lifetime.

- **HttpClient from factory**: The Anthropic service uses `IHttpClientFactory.CreateClient("anthropic")` — never `new HttpClient()`.

- **Process logging for multi-step operations**: `IProcessLogger` is injected into services that perform complex workflows (e.g., `TransactionService`, `AuthService`). Logs are structured JSON with elapsed-time steps.

- **NLog structured logging**: Log messages use structured parameters (`{UserId}`, `{BudgetId}`) rather than string interpolation so the log provider can index them.

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

This codebase targets **net10.0** for all projects. The following .NET 10 features are already in use:

- **Primary constructors** — used in Worker services:
  ```csharp
  internal sealed class ScheduledTransactionWorker(
      ILogger<ScheduledTransactionWorker> logger,
      IOptions<WorkerOptions> options,
      IServiceScopeFactory scopeFactory) : BackgroundService
  ```

- **Collection expressions** — used throughout:
  ```csharp
  public List<string> Tags { get; set; } = [];
  var errors = new List<string>();
  ```

- **`switch` expression for status codes** — used in `ExceptionHandlingMiddleware` and `ApiErrorResponseFactory`

- **`JsonStringEnumConverter`** — registered globally in `Program.cs` so enum values are serialized as strings in JSON responses

- **`ClockSkew = TimeSpan.Zero`** on JWT validation — tokens expire exactly at their `exp` claim with no tolerance buffer

- **`ForwardedHeaders`** middleware configured for Railway (proxy-behind-HTTPS deployment)

No `.NET 10` features are in use at the time of writing.
