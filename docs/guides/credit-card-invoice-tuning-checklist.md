# Credit Card Invoice Tuning Checklist

This checklist applies the requested tuning in compatible phases, without changing how existing data is consumed.

## Phase 1 - Short Month Closing Day

- [x] Use effective closing day (`min(configuredDay, daysInMonth)`) in monthly automatic closure.
- [x] Keep existing `InvoiceClosingDay` semantics unchanged.
- [x] Add logs when configured day is adjusted for short months.
- [x] Add test coverage for effective closing day behavior.

## Phase 2 - Automatic Overdue Transition

- [x] Add service operation to mark overdue invoices automatically.
- [x] Transition only invoices in `Closed` or `PartiallyPaid` with due date before reference date.
- [x] Keep `Paid`, `Open`, and already `Overdue` states unchanged.
- [x] Add worker processor and daily schedule for overdue transition.
- [x] Add test coverage for overdue transition filtering.

## Phase 3 - Installment Double Count Safeguards

- [x] Harden installment recurrence creation to force skip flags for balance/credit effects.
- [x] Normalize legacy/malformed installment recurrences at processing time.
- [x] Preserve current recurrence schema and field meanings.

## Validation Checklist

- [x] Run service and worker build checks.
- [x] Run focused unit tests for `CreditCardInvoiceServiceTests`.
- [x] Run full solution build.
- [x] Verify no breaking contract changes in API DTOs/endpoints.

## Compatibility Notes

- No destructive migration is required.
- Existing collections and records remain valid.
- Added behavior is additive and defensive.
