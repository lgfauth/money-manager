# Coding Standards

## Purpose

This document defines the baseline coding rules for the current solution.

## General Rules

1. Keep responsibilities aligned with the current project boundaries.
2. Prefer small, explicit changes over large speculative refactors.
3. Keep public behavior predictable and documented.
4. Preserve user isolation: all business data operations must be scoped by `userId`.
5. Prefer soft delete for user-facing financial entities unless a hard-delete case is explicitly required.

## Layer Rules

### Domain

- Keep business entities and contracts here.
- Do not add UI or infrastructure-specific behavior.

### Application

- Put business orchestration in services.
- Keep validation logic close to the application boundary.

### Infrastructure

- Put persistence, repository and security helpers here.
- Keep MongoDB-specific implementation out of `Domain`.

### Presentation

- Keep controllers thin.
- Delegate business logic to `Application` services.

### Web

- Keep pages focused on UI state and user interaction.
- Keep HTTP access in service classes.

### Worker

- Keep scheduling and orchestration in hosted services.
- Keep domain behavior in processor or application services.

## Do And Do Not

1. Do keep business logic in `Application`; do not move it into controllers or pages.
2. Do keep controllers thin and explicit; do not hide business rules in HTTP layers.
3. Do keep Mongo-specific behavior in `Infrastructure`; do not leak persistence details into `Domain`.
4. Do use structured logging placeholders; do not rely on ad-hoc `Console.WriteLine` output.
5. Do keep workers idempotent and cancellation-aware; do not implement long-running work without cancellation support.
6. Do keep HTTP access encapsulated in service/hook boundaries on the frontend; do not scatter HTTP calls in multiple UI components.

## Naming Rules

1. Use descriptive names for services, methods and DTOs.
2. Keep file names aligned with the main type they contain.
3. Use English technical naming in code when continuing existing patterns.

## Logging Rules

1. Log important transitions: startup, trigger, completion, timeout and failure.
2. Avoid logs that only restate obvious assignments.

## Testing Rules

1. Add or update tests when behavior changes in business logic.
2. Prefer testing application services and critical flows over superficial coverage.
3. Cover success, not-found, rule violation and user isolation scenarios for service-level behavior.

## Documentation Rules

1. If a change affects architecture, update `architecture-overview.md`.
2. If a change affects Web, API or Worker developer workflow, update the relevant guide.
3. If a change fixes a recurring incident, record it in `troubleshooting/` or `history/`.

## Pull Request Checklist

1. Layering respected: domain, business, infrastructure, and transport concerns remain separated.
2. Any changed business behavior has updated tests.
3. Logging and error messages are actionable and non-sensitive.
4. Relevant docs were updated or intentionally archived.
