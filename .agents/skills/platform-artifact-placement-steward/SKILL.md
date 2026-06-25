---
name: platform-artifact-placement-steward
description: Use before creating, moving, or reviewing repository artifacts to classify ownership, location, dependencies, tests, documentation, ADR impact, and ADR-0003 frontend/backend isolation impact.
---

# Platform Artifact Placement Steward

## Purpose

Classify every new or changed artifact before implementation or review.

This skill is a placement, ownership, and architecture-conformance steward. It is not a code generator, roadmap planner, ADR writer, localization reviewer, or test-design skill.

## Use When

Use this skill before creating, moving, or reviewing any source file, test, document, route, module, app artifact, identity artifact, interface package, contract, client, API, infrastructure adapter, UI component, command, query, DTO, repository, DbContext, migration, DI registration, or background job.

Use it especially when the task mentions artifact placement, module boundaries, frontend/backend isolation, ADR-0003, `Contracts`, `Client`, `Api`, `Interface`, dependency direction, test placement, or docs/ADR impact.

## Relationship to Other Skills

Use this skill before implementation artifacts are created.

- Journey Curator identifies user workflows and design objectives.
- Roadmap Curator derives features and implementation tasks.
- ADR Steward records new or changed architecture decisions.
- Frontend/Backend Isolation checks Interface, Contracts, Api, Application, and backend dependency boundaries.
- Localization Maintenance checks UI text, localization resource ownership, and localizer usage.
- Test Curator derives required unit and hot-test scenarios.

Do not duplicate those responsibilities here. Route to them when the task needs deeper review.

## Sources of Truth

Follow accepted ADRs and existing repository conventions first. When this skill conflicts with an accepted ADR, follow the ADR and report that this skill should be updated.

Primary sources:

- `AGENTS.md`
- `docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md`
- `docs/platform/modules/platform/conventions/adr-0003-implementation-conventions.md`
- `docs/platform/modules/platform/validation/adr-0003-interface-boundary-validation.md`
- Existing conventions in the owning module, app, host, identity context, interface package, library, test project, or docs tree

## Core Rule

Classify first. Place second. Implement third.

Every recommendation must answer:

1. What artifact type is this?
2. Which app, module, host, identity context, interface package, library, or platform scope owns it?
3. What is the preferred location?
4. Why does it belong there?
5. What dependencies are allowed?
6. What dependencies are forbidden?
7. Where should related tests live?
8. Is documentation required?
9. Is an ADR required or recommended?
10. Does ADR-0003 apply?
11. Which validation command should be run?

## Operating Procedure

1. Read the artifact name, purpose, behavior, and intended consumers.
2. Classify whether it owns business state, changes state, reads data, adapts technology, transports data, calls HTTP, registers endpoints, renders UI, composes routes, registers services, localizes UI, documents behavior, or verifies behavior.
3. Identify the owning scope.
4. Determine whether ADR-0003 applies.
5. Prefer the existing convention inside that scope before inventing folders.
6. Apply the placement table and dependency rules.
7. Check for ownership, CQRS, frontend/backend isolation, and source-of-truth conflicts.
8. Identify related tests, docs, ADR impact, localization impact, and validation impact.
9. Return the smallest correct placement recommendation.

## Required References

Read only the reference files needed for the task:

- `references/scope-classification.md` for ownership scope decisions.
- `references/artifact-placement-table.md` for artifact-to-location mapping.
- `references/adr-0003-boundary-rules.md` for Contracts, Client, Api, Interface, Application, Domain, Infrastructure, and Module boundaries.
- `references/dependency-and-conflict-rules.md` for dependency direction and conflict detection.
- `references/route-endpoint-rules.md` for route, endpoint, command, and query placement.
- `references/test-doc-adr-routing.md` for test placement, documentation placement, and ADR impact.
- `references/validation.md` for ADR-0003 static validation commands.
- `references/output-formats.md` for exact response formats.

## Guardrails

- Keep recommendations simple.
- Prefer the narrowest owning scope.
- Do not invent folders when an existing convention is clear.
- Do not move artifacts only for symmetry.
- Do not create shared abstractions before there are at least two real consumers or a clear platform requirement.
- Do not create ADR-0003 `Contracts`, `Client`, or `Api` projects only for symmetry.
- Do not place reusable module behavior in a host page.
- Do not place app-specific behavior in shared modules.
- Do not place backend implementation details in `src/Interfaces/Interfaces.*` or `src/Modules/<Module>/Interface`.
- Do not place technology adapters in Domain or Application.
- Do not place public API contract DTOs in Domain for ADR-0003 migrated module boundaries.
- Do not place business rules in controllers, repositories, DTOs, migrations, adapters, UI components, clients, contracts, or localization resources.
- If placement is ambiguous and ownership affects architecture, ask for clarification unless a safe narrow assumption is obvious.

## Evidence of Use

When applying this skill, include this line in the plan or final response:

```txt
Applied skill: platform-artifact-placement-steward
```

Also include any validation command that should be run.

## Style

Be direct and practical. Prefer tables for multiple artifacts. Explain reasons using ownership, dependency direction, state change, reuse, host composition, persistence, interface boundary, transport contract, source of truth, or platform convention.
