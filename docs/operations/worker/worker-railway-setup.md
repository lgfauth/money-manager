# Worker Railway Setup

## Scope

This document describes how to configure and operate the current `MoneyManager.Worker` service in Railway.

Reference:

- [src/MoneyManager.Worker/appsettings.json](../../../src/MoneyManager.Worker/appsettings.json)
- [src/MoneyManager.Worker/WorkerHost/DependencyInjection/ServiceCollectionExtensions.cs](../../../src/MoneyManager.Worker/WorkerHost/DependencyInjection/ServiceCollectionExtensions.cs)

## Current Worker Behavior

The worker currently runs two hosted services:

1. `ScheduledTransactionWorker`
   - immediate startup execution for overdue recurring transactions
   - scheduled recurring processing at `08:00`
2. `InvoiceClosureWorker`
   - scheduled invoice closure at `00:01`

Investment-related jobs were removed from the system and are no longer part of the worker runtime.

## Required Environment Variables

### MongoDB

```bash
MongoDB__ConnectionString=mongodb+srv://user:password@cluster.mongodb.net/?retryWrites=true&w=majority
MongoDB__DatabaseName=MoneyManager
```

Use the same logical database value expected by the deployed backend environment.

### Recurring Transaction Schedule

```bash
Schedule__Hour=8
Schedule__Minute=0
Schedule__TimeZoneId=E. South America Standard Time
Schedule__LoopDelaySeconds=30
```

### Invoice Closure Schedule

```bash
InvoiceClosureSchedule__Hour=0
InvoiceClosureSchedule__Minute=1
InvoiceClosureSchedule__TimeZoneId=E. South America Standard Time
InvoiceClosureSchedule__LoopDelaySeconds=60
```

### Execution Timeout

```bash
Worker__ExecutionTimeoutMinutes=5
```

## Expected Logs

### ScheduledTransactionWorker

Expected startup sequence:

```text
TransactionSchedulerWorker INICIADO
STARTUP EXECUTION: Processando recorrencias vencidas imediatamente...
Processamento finalizado em ...
STARTUP EXECUTION: Concluida com sucesso. Aguardando proximo horario agendado.
```

### InvoiceClosureWorker

Expected scheduled sequence:

```text
InvoiceClosureWorker INICIADO
TRIGGER: Executando fechamento de faturas (hora agendada atingida)
Fechamento de faturas finalizado em ...
```

## Operational Checklist

1. Worker service is healthy in Railway.
2. MongoDB connection variables are configured.
3. `Schedule__*` variables are configured for recurring processing.
4. `InvoiceClosureSchedule__*` variables are configured for invoice closure.
5. `Worker__ExecutionTimeoutMinutes` is configured.
6. A redeploy was executed after configuration changes.
7. Logs show both worker startup and scheduled activity.

## Common Problems

### Worker does not start

Check:

1. Railway service health status.
2. Missing environment variables.
3. MongoDB connectivity.
4. invalid option values causing options validation to fail.

### Worker starts but recurring processing does not run

Check:

1. current time zone configuration.
2. schedule values.
3. startup execution logs.
4. whether overdue recurring data actually exists.

### Invoice closure does not run

Check:

1. `InvoiceClosureSchedule` values.
2. logs from `InvoiceClosureWorker`.
3. application data and business preconditions for invoice closure.

## Safe Change Procedure

When changing worker behavior:

1. update schedule or options in code if needed
2. update Railway environment variables
3. redeploy worker
4. verify logs
5. update [../../guides/worker-development-guide.md](../../guides/worker-development-guide.md) if runtime behavior changed
