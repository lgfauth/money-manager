# MoneyManager Architecture Overview

## Purpose

This document describes the current architecture that exists in code today. It also highlights known constraints and a small set of future recommendations. Recommendations are explicitly marked as such.

## Solution Structure

The solution is defined in [MoneyManager.sln](../../MoneyManager.sln) and currently contains these projects:

1. `MoneyManager.Domain`
   Current role: entities, enums and contracts for the business domain.
2. `MoneyManager.Application`
   Current role: business services and validation logic.
3. `MoneyManager.Infrastructure`
   Current role: MongoDB access, repositories, security helpers and `UnitOfWork`.
4. `MoneyManager.Presentation`
   Current role: ASP.NET Core REST API.
5. `MoneyManager.Web`
   Current role: Blazor WebAssembly frontend.
6. `MoneyManager.Web.Host`
   Current role: static host for the Web frontend.
7. `MoneyManager.Worker`
   Current role: scheduled background processing.
8. `MoneyManager.Tests`
   Current role: automated tests.

## Current Layer Responsibilities

### Domain

Current state:

- Core entities include `Account`, `Budget`, `Category`, `CreditCardInvoice`, `RecurringTransaction`, `Transaction`, `User` and `UserSettings`.
- Contracts include repository abstractions and `IUnitOfWork`.

Reference: [src/MoneyManager.Domain/Entities](../../src/MoneyManager.Domain/Entities), [src/MoneyManager.Domain/Interfaces](../../src/MoneyManager.Domain/Interfaces)

### Application

Current state:

- Business services handle auth, accounts, categories, budgets, transactions, recurring transactions, reports, profile, onboarding and credit card invoices.
- Validation uses FluentValidation.

Reference: [src/MoneyManager.Application/Services](../../src/MoneyManager.Application/Services), [src/MoneyManager.Application/Validators](../../src/MoneyManager.Application/Validators)

### Infrastructure

Current state:

- MongoDB is the persistence mechanism.
- `UnitOfWork` exposes repositories through lazy-loaded properties.
- `SaveChangesAsync()` is currently a no-op because MongoDB writes are handled directly.

Reference: [src/MoneyManager.Infrastructure/Repositories/UnitOfWork.cs](../../src/MoneyManager.Infrastructure/Repositories/UnitOfWork.cs)

### API

Current state:

- Controllers expose the REST surface for auth, accounts, budgets, categories, transactions, reports, recurring transactions, settings, profile, onboarding, account deletion, admin and credit card invoices.
- JWT authentication is configured in the startup pipeline.
- Swagger is enabled.
- CORS is currently permissive.

Reference: [src/MoneyManager.Presentation/Program.cs](../../src/MoneyManager.Presentation/Program.cs), [src/MoneyManager.Presentation/Controllers](../../src/MoneyManager.Presentation/Controllers)

### Web

Current state:

- The frontend is Blazor WebAssembly.
- The client uses a configured `HttpClient` with an authorization handler.
- Localization files exist under `wwwroot/i18n` for `pt-BR`, `en-US`, `es-ES` and `fr-FR`.
- Web services are HTTP wrappers around API endpoints.

Reference: [src/MoneyManager.Web/Program.cs](../../src/MoneyManager.Web/Program.cs), [src/MoneyManager.Web/Services](../../src/MoneyManager.Web/Services), [src/MoneyManager.Web/wwwroot/i18n](../../src/MoneyManager.Web/wwwroot/i18n)

### Web Host

Current state:

- `MoneyManager.Web.Host` serves the Blazor output as static files.
- MIME types for `.wasm`, `.json`, `.js`, `.css` and `.html` are explicitly configured.

Reference: [src/MoneyManager.Web.Host/Program.cs](../../src/MoneyManager.Web.Host/Program.cs)

### Worker

Current state:

- The worker registers two hosted services:
  - `ScheduledTransactionWorker`
  - `InvoiceClosureWorker`
- The current schedules in [src/MoneyManager.Worker/appsettings.json](../../src/MoneyManager.Worker/appsettings.json) are:
  - recurring transactions at `08:00`
  - invoice closure at `00:01`
- The worker uses the same application and infrastructure layers used by the API.

Reference: [src/MoneyManager.Worker/WorkerHost/DependencyInjection/ServiceCollectionExtensions.cs](../../src/MoneyManager.Worker/WorkerHost/DependencyInjection/ServiceCollectionExtensions.cs)

## Runtime Communication

### Web to API

- `MoneyManager.Web` calls the API over HTTP.
- JWT is attached through `AuthorizationMessageHandler`.
- The API base URL is currently hardcoded in the Web startup.

Reference: [src/MoneyManager.Web/Program.cs](../../src/MoneyManager.Web/Program.cs)

### API to MongoDB

- `MoneyManager.Presentation` composes application services and repositories.
- MongoDB configuration is loaded from `appsettings` and dependency injection.

Reference: [src/MoneyManager.Presentation/Program.cs](../../src/MoneyManager.Presentation/Program.cs)

### Worker to MongoDB

- The worker does not call the API.
- It uses the same business services directly against the shared backend layers.

## Current Constraints And Risks

### Current constraints found in code

1. The Web project currently hardcodes the API URL.
2. The API currently allows permissive CORS behavior.
3. `UnitOfWork.SaveChangesAsync()` exists as a compatibility abstraction, but does not persist a transaction boundary.
4. The API and Worker appsettings currently use different MongoDB database names in local configuration.
5. Legacy documentation contains encoding corruption and duplicated material.

## Recommendations For Future Work

These points are recommendations, not statements about code that already exists.

1. Move API base URL configuration out of the compiled Web startup and into environment-driven configuration.
2. Replace permissive CORS with an explicit allow-list.
3. Keep `architecture-overview.md` updated whenever project boundaries or runtime flows change.
4. Avoid adding new feature documentation directly under `docs/`; place it under `guides/`, `operations/`, `history/` or `archive/`.

## Documentation Governance

- Update this document when adding or removing projects, changing runtime communication, or changing responsibilities between layers.
- Historical fix narratives belong in `docs/history/` or `docs/archive/`, not here.
