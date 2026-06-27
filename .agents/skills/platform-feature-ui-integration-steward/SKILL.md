---
name: platform-feature-ui-integration-steward
description: Guide React/HeroUI feature integration over generated OpenAPI clients; keep screens workflow-shaped, feature hooks authoritative, React Query invalidation centralized, and UI free of transport leakage.
---

# Platform Feature UI Integration Steward

## Purpose

Use this skill when connecting HeroUI/React pages, forms, tables, dialogs, and feature components to generated API clients.

The goal is feature-shaped UI. Screens must follow user intent and workflow, not raw HTTP endpoints.

## Use When

- Building or reviewing HeroUI pages, forms, tables, dialogs, or feature components backed by generated clients.
- Creating feature hooks that wrap generated React Query hooks.
- Defining query invalidation/update behavior after mutations.
- Mapping ProblemDetails or validation errors into UI state.
- Scaffolding UI from feature intent.

## Do Not Use For

- OpenAPI contract design; use `platform-openapi-contract-steward`.
- Orval/client generation; use `platform-generated-react-client-steward`.
- Pure visual-only styling with no data or workflow boundary.

## UI Boundary Rules

1. Pages/components consume feature hooks, not generated hooks.
2. Feature hooks expose use-case names and UI-ready data, not endpoint names.
3. Feature hooks own query invalidation, cache updates, optimistic updates, and mutation side effects.
4. HeroUI components receive presentation state, validation messages, loading state, and event handlers.
5. UI does not know auth headers, base URLs, transport errors, DTO quirks, or generated client structure.
6. ProblemDetails is mapped once into feature/UI error state.
7. Forms are workflow-shaped; do not mirror request DTOs blindly.
8. Tables/lists are task-shaped; do not expose backend pagination/filtering accidents directly.
9. Feature adapters may map generated DTOs to view models when it reduces UI coupling.
10. Keep local component state separate from server state; do not create parallel truth sources.

## Review Procedure

1. Identify the user workflow and feature intent.
2. Identify feature hooks and the generated hooks they wrap.
3. Verify pages/forms/tables/dialogs import only feature-level contracts.
4. Check mutation success behavior: invalidation, cache update, navigation, toast, or no-op.
5. Check failure behavior: ProblemDetails, validation errors, retry/cancel, disabled states.
6. Remove endpoint-shaped UI artifacts unless the task explicitly needs a technical/admin surface.
7. Confirm the smallest useful UI was implemented without speculative generic wrappers.

## Required Output

```txt
Feature: <name>
User intent: <one sentence>
Feature hooks: <list>
Generated imports in UI: <none / list>
Server-state owner: <hook/query key owner>
Decision: <accept / fix / reject>

Fixes:
- <minimal required changes>

Left out:
- <intentional omissions>
```

## Hard Stops

Stop and correct if:

- A page, form, table, or dialog imports generated hooks directly.
- Mutation invalidation is scattered in UI components.
- UI logic depends on transport details, auth, base URLs, or raw ProblemDetails.
- Request DTO shape dictates form layout without workflow justification.
- The same server state is copied into uncontrolled local truth without a synchronization rule.
- Generic UI scaffolding adds complexity not needed by the current feature.

## Handoff

Use this skill after OpenAPI contract validation and client generation are complete.
