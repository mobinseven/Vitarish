# Feed ADR-003 pilot selection

Date: 2026-06-24
Status: Accepted
App: Platform
Module: Feed
Capability: ADR-003 module interface isolation pilot
Owner: Platform
Related apps: Main host
Related modules: Requests
Related ADRs: docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md

## Context

Stage 3 required exactly one low-coupling pilot module from `src/Modules/Feed` and `src/Modules/Requests` to validate ADR-003 with the least unrelated refactoring.

`Feed` has an empty Interface project, read-only query workflows, no module-owned persistence, and simple HTTP response shapes for feed items, feed sources, and configuration. `Requests` owns request identity context, EF persistence, identity claim resolution, backend options, and cross-module backend dependencies.

## Decision

Use `src/Modules/Feed` as the sole ADR-003 pilot module for Stage 3.

## Consequences

The Feed module owns new `Contracts`, `Client`, and `Api` projects. Feed Interface references only frontend-safe Feed contracts/client projects. Feed API is registered from the backend module composition root.

## Alternatives Considered

- `src/Modules/Requests`: rejected for this pilot because its identity and persistence dependencies would require broader backend refactoring unrelated to proving the ADR-003 project shape.
- Migrating both candidates: rejected because Stage 3 explicitly limits this work to one pilot unless further migration is approved after the first pilot is complete.

## Validation / Enforcement

`tools/architecture/interface_boundary_config.json` marks `Feed` as `FailFast`, so forbidden backend references from Feed Interface fail the ADR-003 boundary validation.
