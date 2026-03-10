# Worker Development Guide

## Scope

This guide covers the current background processing in `MoneyManager.Worker`.

Reference: [src/MoneyManager.Worker](../../src/MoneyManager.Worker)

## Current Runtime Behavior

The worker currently registers two hosted services:

1. `ScheduledTransactionWorker`
   - startup execution for backlog processing
   - scheduled recurring transaction processing at `08:00`
2. `InvoiceClosureWorker`
   - scheduled invoice closure at `00:01`

References:

- [src/MoneyManager.Worker/WorkerHost/DependencyInjection/ServiceCollectionExtensions.cs](../../src/MoneyManager.Worker/WorkerHost/DependencyInjection/ServiceCollectionExtensions.cs)
- [src/MoneyManager.Worker/appsettings.json](../../src/MoneyManager.Worker/appsettings.json)

## Current Configuration Model

Configuration is split into:

1. `Schedule`
2. `InvoiceClosureSchedule`
3. `Worker`
4. `MongoDB`

## Development Rules

### Add a new background process

1. Confirm that the behavior belongs in background processing and not in synchronous API flow.
2. Prefer a dedicated processor class for business logic and keep the hosted service focused on scheduling and orchestration.
3. Make the process idempotent.
4. Use configuration objects for schedule and timeout values.

### Modify an existing schedule

1. Update the options model if needed.
2. Update `appsettings` and deployment configuration.
3. Document the operational impact in the worker guide or operations material.

### Logging

1. Log startup, trigger, completion and timeout events.
2. Avoid noisy logs inside tight loops unless they are useful for diagnosis.

## Operational Notes

Current local defaults in appsettings:

- recurring schedule: `08:00`, loop delay `30s`
- invoice closure schedule: `00:01`, loop delay `60s`
- execution timeout: `5 minutes`

## Practical Example

If a new daily reconciliation job is needed in the future:

1. Add a dedicated processor with business behavior.
2. Add a hosted service for scheduling.
3. Bind a strongly typed options class.
4. Register it in the worker dependency injection extension.
5. Update operations documentation with schedule, timeout and expected logs.
