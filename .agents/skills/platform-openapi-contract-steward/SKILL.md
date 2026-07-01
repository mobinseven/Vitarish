---
name: platform-openapi-contract-steward
description: Review and maintain module/app OpenAPI contracts that drive generated React clients; enforce stable operation IDs, schema names, response/error shapes, auth documentation, linting, and generation safety.
---

# Platform OpenAPI Contract Steward

## Purpose

Use this skill when a backend HTTP boundary will be consumed through OpenAPI-based tooling.

The goal is a stable, boring, generation-safe API contract. The OpenAPI document is a boundary contract, not a dump of controller implementation details.

## Use When

- Creating, editing, reviewing, or splitting module/app OpenAPI specs.
- Preparing specs for Orval, MSW, SDK generation, or frontend contract tests.
- Adding endpoints that affect React clients, feature hooks, or generated DTOs.
- Reviewing API naming, auth, pagination, errors, versioning, or breaking changes.

## Do Not Use For

- Internal-only methods that are not HTTP/API boundaries.
- UI composition, form layout, or React component structure.
- Business-rule design that belongs in domain/application ADRs.

## Contract Rules

1. Every operation has a stable, human-authored `operationId`.
2. `operationId` values are unique, deterministic, and use client-friendly verb/noun names.
3. Tags group by feature/module capability, not by controller class accidents.
4. Schema names are stable DTO names, not anonymous framework output.
5. Request and response DTOs are explicit; avoid leaking persistence/domain internals.
6. Success responses document status code, media type, and schema.
7. Error responses document `application/problem+json` / ProblemDetails shape.
8. Auth schemes, authorization expectations, and tenant/module boundaries are visible in the contract.
9. Pagination, sorting, filtering, idempotency, and concurrency shapes are consistent per module/app.
10. Nullable, optional, enum, date/time, and identifier semantics are explicit enough for TypeScript generation.
11. Specs are linted before generation; lint failures block client regeneration.
12. Breaking changes are deliberate and visible; do not hide them behind regenerated clients.

## Review Procedure

1. Identify the owning app/module/capability.
2. Check whether the endpoint belongs in this module boundary.
3. Verify request/response DTOs are transport contracts, not domain/persistence models.
4. Check operation naming, tags, auth, success responses, ProblemDetails errors, and version impact.
5. Confirm the spec can be consumed by generated clients without hand edits.
6. Call out any breaking change and propose either compatibility or explicit versioning.

## Required Output

For each reviewed spec or endpoint, report:

```txt
Contract: <module/app/spec>
Boundary owner: <owner>
Generation impact: <none / DTO / hook / query key / mock / breaking>
Decision: <accept / fix before generation / reject>

Findings:
- <issue or pass statement>

Required fixes:
- <minimal fix>

Deferred items:
- <only if safe to defer>
```

## Hard Stops

Stop and correct before generation if:

- `operationId` is missing, duplicated, route-derived noise, or unstable.
- A persistence/domain model leaks as an API schema.
- Error shape is undocumented or incompatible with ProblemDetails handling.
- Auth or tenant boundary is implicit.
- A breaking change is introduced without being named.
- The generated client would require manual edits to work.

## Handoff

After the contract is valid, hand off to `platform-generated-react-client-steward` for Orval/client generation work.

## Localization Boundary Addendum

OpenAPI schemas should expose stable codes, statuses, and structured fields for user-facing errors or states. They must not require translated display text from backend/application layers. React UI maps stable contract codes to localized presentation strings. Do not change a contract solely to match a visual component's copy shape.
