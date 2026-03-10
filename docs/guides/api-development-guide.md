# API Development Guide

## Scope

This guide covers the current ASP.NET Core API in `MoneyManager.Presentation`.

Reference: [src/MoneyManager.Presentation](../../src/MoneyManager.Presentation)

## Current Structure

- Startup and middleware composition: [src/MoneyManager.Presentation/Program.cs](../../src/MoneyManager.Presentation/Program.cs)
- Controllers: [src/MoneyManager.Presentation/Controllers](../../src/MoneyManager.Presentation/Controllers)
- Middleware: [src/MoneyManager.Presentation/Middlewares](../../src/MoneyManager.Presentation/Middlewares)

## Current API Responsibilities

1. Expose REST endpoints to the Blazor Web client.
2. Authenticate users with JWT.
3. Compose business services from `Application` and persistence from `Infrastructure`.
4. Expose operational support like Swagger and health checks.

## Development Rules

### Add a new endpoint

1. Start with the business behavior in `Application`.
2. Add or extend a controller only after the service contract is clear.
3. Keep controllers thin. Orchestration belongs in `Application` services.
4. Validate inputs with FluentValidation where appropriate.

### Error handling

1. Let the central middleware handle consistent HTTP responses.
2. Avoid duplicating generic try/catch blocks inside every controller action unless the action needs special mapping.

### Persistence rules

1. Repositories belong in `Infrastructure`.
2. Domain contracts belong in `Domain`.
3. Application services should not depend on Web-specific concerns.

## Current Risks

1. CORS is currently permissive and should be treated as an operational risk.
2. Local `appsettings.json` currently shows a MongoDB database name different from the worker config.

## Practical Example

To add a new report endpoint:

1. Implement the business query in the report service under `Application`.
2. Add the controller action in `ReportsController` or a new controller if the scope is distinct.
3. Return a stable response shape.
4. Update the matching Web service wrapper.

## Recommended Review Checklist

1. Is the behavior in the correct layer?
2. Is the controller thin?
3. Are auth requirements explicit?
4. Are response and error contracts stable?
5. Does the Web client already have a corresponding integration path?
