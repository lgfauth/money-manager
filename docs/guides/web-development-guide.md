# Web Development Guide

## Scope

This guide covers the current Blazor WebAssembly frontend in `MoneyManager.Web`.

Reference: [src/MoneyManager.Web](../../src/MoneyManager.Web)

## Current Structure

- Startup: [src/MoneyManager.Web/Program.cs](../../src/MoneyManager.Web/Program.cs)
- Pages: [src/MoneyManager.Web/Pages](../../src/MoneyManager.Web/Pages)
- Services: [src/MoneyManager.Web/Services](../../src/MoneyManager.Web/Services)
- Localization files: [src/MoneyManager.Web/wwwroot/i18n](../../src/MoneyManager.Web/wwwroot/i18n)

## How The Web Layer Works Today

1. The app is a Blazor WebAssembly SPA.
2. Authentication state is managed in the client.
3. API calls are made through typed service wrappers.
4. Localization is loaded from JSON files under `wwwroot/i18n`.

## Development Rules

### Add a new page

1. Create the Razor page under `Pages/`.
2. Prefer page-level orchestration and keep HTTP calls inside the matching service under `Services/`.
3. If the page needs localization, add keys to all active locale files.

### Add a new API integration

1. Create or extend a service interface in `Services/`.
2. Implement the HTTP calls in the matching service class.
3. Keep endpoint paths, payload shape and error handling aligned with the API controller.

### Keep UI logic maintainable

1. Avoid embedding large API or data transformation logic directly in `.razor` files.
2. Use reusable components only when they are shared or clearly reduce duplication.
3. Prefer explicit loading, empty and error states.

## Current Risks

1. The API URL is hardcoded in startup.
2. Some historical encoding issues affected Web UI strings and JSON localization files.

## Recommended Working Pattern

1. Start from the target page.
2. Identify the service that owns the HTTP call.
3. Confirm the matching API controller and DTO shape.
4. Update localization files in the same change.
5. Validate visible text in `pt-BR` and at least one secondary locale.

## Practical Example

When adding a new settings section:

1. Add the UI in `Pages/Settings.razor` or a related component.
2. Extend the relevant service in `Services/`.
3. Verify the backing endpoint exists in the API.
4. Add localization keys in `pt-BR.json`, `en-US.json`, `es-ES.json` and `fr-FR.json`.
