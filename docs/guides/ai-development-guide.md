# AI Development Guide

## Purpose

This guide defines how AI assistants and GitHub Copilot should work with this repository.

## Source Of Truth Rule

AI-generated changes must follow the current codebase, not assumptions from old documents.

Always inspect:

1. [MoneyManager.sln](../../MoneyManager.sln)
2. [src/MoneyManager.Presentation/Program.cs](../../src/MoneyManager.Presentation/Program.cs)
3. [src/MoneyManager.Web/Program.cs](../../src/MoneyManager.Web/Program.cs)
4. [src/MoneyManager.Worker/WorkerHost/DependencyInjection/ServiceCollectionExtensions.cs](../../src/MoneyManager.Worker/WorkerHost/DependencyInjection/ServiceCollectionExtensions.cs)

## Copilot Working Rules

1. Do not invent projects, layers or services that are not present in the solution.
2. Distinguish explicitly between current behavior and future recommendation.
3. Prefer minimal changes that fit the current architecture.
4. Update documentation whenever behavior, configuration or public API changes.
5. Treat `docs/archive/` as historical context, not active guidance.

## Required Workflow For Changes

1. Inspect relevant code before proposing architecture decisions.
2. Identify affected layer: Web, API, Worker, Application, Domain or Infrastructure.
3. Implement the smallest coherent change.
4. Validate with build or tests when possible.
5. Reflect important decisions in active docs.

## Prompts That Work Well

Good prompt example:

`Inspect the current Worker architecture, modify only the existing scheduling flow, and update the worker guide if the runtime behavior changes.`

Bad prompt example:

`Refactor the whole backend to clean architecture with queues and event sourcing.`

The bad example is risky because it assumes architectural intent that does not exist in the repository.

## Documentation Update Rule

When a change affects runtime behavior, update one of these first-class documents:

1. `architecture-overview.md`
2. the relevant development guide in `docs/guides/`
3. an operational guide in `docs/operations/`
4. a troubleshooting guide when the change addresses a recurring incident

## Constraints AI Should Respect

1. Keep files UTF-8.
2. Avoid reintroducing removed investment-related modules.
3. Do not rely on corrupted legacy text as authoritative.
4. Prefer archive over deletion when cleaning documentation.
