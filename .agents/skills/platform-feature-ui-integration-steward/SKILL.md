---
name: platform-feature-ui-integration-steward
description: Guide modular React/HeroUI feature integration over generated OpenAPI clients; keep screens workflow-shaped, module/submodule features separated, feature hooks authoritative, React Query invalidation centralized, and UI free of transport leakage.
---

# Platform Feature UI Integration Steward

## Purpose

Use this skill when connecting HeroUI/React pages, forms, tables, dialogs, and feature components to generated API clients.

The goal is modular, feature-shaped UI. Screens must follow user intent and workflow, not raw HTTP endpoints, and feature folders must preserve module and submodule ownership.

## Use When

- Building or reviewing HeroUI pages, forms, tables, dialogs, or feature components backed by generated clients.
- Creating feature hooks that wrap generated React Query hooks.
- Defining query invalidation/update behavior after mutations.
- Mapping ProblemDetails or validation errors into UI state.
- Scaffolding UI from feature intent.
- Deciding whether React UI belongs under a module feature, submodule/capability feature, host component, or shared UI owner.

## Do Not Use For

- OpenAPI contract design; use `platform-openapi-contract-steward`.
- Orval/client generation; use `platform-generated-react-client-steward`.
- Pure visual-only styling with no data, workflow, ownership, or integration boundary.
- Repository-wide placement decisions; use `platform-artifact-placement-steward` first when location is unclear.
- Frontend/backend dependency review; use `platform-frontend-backend-isolation` when imports or layer boundaries are in question.

## Modular Feature Ownership

React feature integration must start by identifying the owning module and, when applicable, the owning submodule/capability.

Default hierarchy:

```txt
src/Hosts/<Host>/src/features/<module>/...
src/Hosts/<Host>/src/features/<module>/<submodule-or-capability>/...
```

Examples:

```txt
src/Hosts/<Host>/src/features/medical/components/...
src/Hosts/<Host>/src/features/knowledge/components/...
src/Hosts/<Host>/src/features/medical/prescription/components/...
src/Hosts/<Host>/src/features/medical/prescription/hooks/...
src/Hosts/<Host>/src/features/knowledge/corpus/api/...
```

Rules:

1. Medical UI, hooks, API wrappers, and state stay under the Medical feature owner.
2. Knowledge UI, hooks, API wrappers, and state stay under the Knowledge feature owner.
3. A submodule/capability such as Medical/Prescription gets its own nested feature folder when it has distinct workflow, state, route group, generated-client wrapper, query keys, or likely independent evolution.
4. Shared host UI must be promoted to a named shared owner, such as host `components`/`ui` or an interface package. Do not share by importing sibling feature internals.
5. A generic `features/common`, `features/shared`, or broad parent-module bucket is only acceptable when the owner and reuse rule are explicit.

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
11. Module feature hooks/components must not import sibling module internals.
12. Submodule/capability hooks/components must not leak into a parent module generic folder unless intentionally promoted to a named shared owner.

## Review Procedure

1. Identify the user workflow and feature intent.
2. Identify the owning module and submodule/capability, if any.
3. Verify the files are under the owning feature hierarchy, such as `features/medical`, `features/knowledge`, or `features/medical/prescription`.
4. Identify feature hooks and the generated hooks they wrap.
5. Verify pages/forms/tables/dialogs import only feature-level contracts.
6. Check mutation success behavior: invalidation, cache update, navigation, toast, or no-op.
7. Check failure behavior: ProblemDetails, validation errors, retry/cancel, disabled states.
8. Remove endpoint-shaped UI artifacts unless the task explicitly needs a technical/admin surface.
9. Check for cross-feature imports and sibling module/submodule coupling.
10. Confirm the smallest useful UI was implemented without speculative generic wrappers.

## Required Output

```txt
Feature: <module[/submodule-or-capability]>
User intent: <one sentence>
Placement: <feature folder / page / shared owner>
Feature hooks: <list>
Generated imports in UI: <none / list>
Server-state owner: <hook/query key owner>
Module separation: <pass / fix required>
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
- Medical and Knowledge UI/hooks/API wrappers are mixed in one feature folder.
- A submodule/capability such as Medical/Prescription is placed in a generic Medical folder despite distinct workflow, state, route group, API wrapper, query keys, or independent evolution.
- A feature imports sibling module/submodule internals instead of extracting a named shared owner.
- Generic UI scaffolding adds complexity not needed by the current feature.

## Handoff

Use this skill after OpenAPI contract validation and client generation are complete.

Use `platform-artifact-placement-steward` first when the owning module/submodule or location is unclear.

Use `platform-frontend-backend-isolation` when the integration touches backend imports, generated-client isolation, Contracts, Api, Application, Domain, or cross-layer dependency rules.
