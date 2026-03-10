# Recurring Issues

## Purpose

This document captures recurring technical issues that have already appeared in this solution and should be checked before deeper troubleshooting.

## Known Recurring Issues

### API base URL drift in Web

Current state:

- The Web project currently hardcodes the API base URL in startup.

Check:

1. Confirm the configured URL in [src/MoneyManager.Web/Program.cs](../../src/MoneyManager.Web/Program.cs).
2. Confirm the deployed API address matches the Web build.

### CORS failures in API

Current state:

- The API uses permissive CORS, but deployment changes can still break browser behavior if proxy headers or origins are inconsistent.

Check:

1. Review [src/MoneyManager.Presentation/Program.cs](../../src/MoneyManager.Presentation/Program.cs).
2. Inspect browser network errors and preflight responses.

### Text corruption and localization regressions

Current state:

- The repository has a history of encoding-related regressions in UI text and docs.

Check:

1. Review [guides/utf8-and-text-encoding-rules.md](../guides/utf8-and-text-encoding-rules.md).
2. Validate locale JSON files and rendered UI text.

### Worker schedule confusion

Current state:

- The worker currently has two schedules with different responsibilities.

Check:

1. Confirm [src/MoneyManager.Worker/appsettings.json](../../src/MoneyManager.Worker/appsettings.json).
2. Confirm `ScheduledTransactionWorker` and `InvoiceClosureWorker` registrations in [src/MoneyManager.Worker/WorkerHost/DependencyInjection/ServiceCollectionExtensions.cs](../../src/MoneyManager.Worker/WorkerHost/DependencyInjection/ServiceCollectionExtensions.cs).
