# Frontend Modernization Summary

## Purpose

This document consolidates the active takeaways from prior frontend planning and fix cycles.

## Current State

1. The current active frontend implementation is in [src/MoneyManager.Frontend](../../src/MoneyManager.Frontend).
2. The migration plan was executed incrementally and tracked in [../NEXTJS_EXECUTION_PLAN.md](../NEXTJS_EXECUTION_PLAN.md).
3. Credit card and invoice semantics were aligned with backend rules for committed credit, available credit, invoice totals, and reconciliation.

## Key Decisions Preserved

1. Keep API contracts explicit and aligned between frontend and backend, especially for payment and installment flows.
2. Prefer backend-owned financial rules (invoice assignment, installment reservation, reconciliation) over distributed frontend logic.
3. Keep UI language explicit to avoid ambiguity between card debt, current invoice, committed credit, and available credit.
4. Keep recurring technical fixes in backend/frontend tests to prevent regression.

## Where To Update Going Forward

1. Feature progress and open steps: [../NEXTJS_EXECUTION_PLAN.md](../NEXTJS_EXECUTION_PLAN.md)
2. Developer workflow for frontend changes: [../guides/web-development-guide.md](../guides/web-development-guide.md)
3. Architecture boundary changes: [../architecture/architecture-overview.md](../architecture/architecture-overview.md)
4. Incident patterns and known issues: [../troubleshooting/recurring-issues.md](../troubleshooting/recurring-issues.md)

## Archived Source Documents

The following files were superseded and moved to archive during cleanup:

1. `FRONTEND_FIXES_ANALYSIS.md`
2. `IMPROVEMENT_CYCLES.md`
3. `NEXTJS_FRONTEND_SPEC.md`
4. `FRONTEND_DOCUMENTATION.md`
