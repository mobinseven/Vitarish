---
name: platform-artifact-placement-steward
description: Use before creating, moving, or reviewing repository artifacts to classify ownership, location, dependencies, tests, documentation, ADR impact, ADR-0003 frontend/backend isolation impact, and modular UI/page/component placement across modules, submodules, hosts, and React Web host features.
---

# Platform Artifact Placement Steward

## Purpose

Classify every new or changed artifact before implementation or review.

This skill is a placement, ownership, and architecture-conformance steward. It is not a code generator, roadmap planner, ADR writer, localization reviewer, or test-design skill.

## Use When

Use this skill before creating, moving, or reviewing any source file, test, document, route, module, app artifact, host artifact, identity artifact, interface package, contract, client, API, infrastructure adapter, UI component, Razor component, React component, page, route module, feature hook, generated web client, command, query, DTO, repository, DbContext, migration, DI registration, or background job.

Use it especially when the task mentions artifact placement, module boundaries, frontend/backend isolation, ADR-0003, `Contracts`, `Client`, `Api`, `Interface`, Web host, React, Razor, page placement, component placement, interface component placement, module feature placement, submodule placement, capability placement, dependency direction, test placement, or docs/ADR impact.

## Relationship to Other Skills

Use this skill before implementation artifacts are created.

- Journey Curator identifies user workflows and design objectives.
- Roadmap Curator derives features and implementation tasks.
- ADR Steward records new or changed architecture decisions.
- Frontend/Backend Isolation checks Interface, Contracts, Api, Application, Web host, and backend dependency boundaries.
- Localization Maintenance checks UI text, localization resource ownership, and localizer usage.
- Test Curator derives required unit and hot-test scenarios.

Do not duplicate those responsibilities here. Route to them when the task needs deeper review.

## Sources of Truth

Follow accepted ADRs and existing repository conventions first. When this skill conflicts with an accepted ADR, follow the ADR and report that this skill should be updated.

Primary sources:

- `AGENTS.md`
- `docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md`
- `docs/architecture/conventions/adr-0003-implementation-conventions.md`
- `docs/architecture/validation/adr-0003-interface-boundary-validation.md`
- Existing conventions in the owning module, submodule/capability, app, host, React Web host, identity context, interface package, library, test project, or docs tree

## Core Rule

Classify first. Place second. Implement third.

Every recommendation must answer:

1. What artifact type is this?
2. Which app, module, submodule/capability, host, Web host, identity context, interface package, library, or platform scope owns it?
3. What is the preferred location?
4. Why does it belong there?
5. What dependencies are allowed?
6. What dependencies are forbidden?
7. Where should related tests live?
8. Is documentation required?
9. Is an ADR required or recommended?
10. Does ADR-0003 apply?
11. Does modular UI/page/component placement apply?
12. Does React Web host placement apply?
13. Which validation command should be run?

## Operating Procedure

1. Read the artifact name, purpose, behavior, intended consumers, and runtime host.
2. Classify whether it owns business state, changes state, reads data, adapts technology, transports data, calls HTTP, registers endpoints, renders Razor UI, renders React UI, composes a route/page, owns module feature state, owns submodule/capability state, wraps generated clients/hooks, registers services, localizes UI, documents behavior, or verifies behavior.
3. Identify the owning scope.
4. If the artifact is UI, classify it as host page, route composition, module Interface component, submodule/capability Interface component, host-only component, React feature component/hook, generated API artifact, or shared interface package artifact.
5. If the artifact is module or Web host UI, identify the owning module and, when applicable, the owning submodule/capability before choosing `Interface`, `Pages`, `features/...`, or shared interface package placement.
6. Determine whether ADR-0003 applies.
7. Determine whether modular UI/page/component placement applies.
8. Determine whether React Web host placement applies.
9. Prefer the existing convention inside that scope before inventing folders.
10. Apply the placement table, modular UI rules, Web host rules, and dependency rules.
11. Check for ownership, CQRS, frontend/backend isolation, module/submodule UI boundary, React host/module/submodule boundary, and source-of-truth conflicts.
12. Identify related tests, docs, ADR impact, localization impact, and validation impact.
13. Return the smallest correct placement recommendation.

## Required References

Read only the reference files needed for the task:

- `references/scope-classification.md` for ownership scope decisions.
- `references/artifact-placement-table.md` for artifact-to-location mapping.
- `references/modular-ui-placement.md` for module/submodule Interface components, host pages, React pages, feature components, hooks, and capability UI placement.
- `references/web-host-react-placement.md` for React Web host page, module feature, submodule/capability, component, hook, generated client, asset, and route placement.
- `references/adr-0003-boundary-rules.md` for Contracts, Client, Api, Interface, Application, Domain, Infrastructure, and Module boundaries.
- `references/dependency-and-conflict-rules.md` for dependency direction and conflict detection.
- `references/route-endpoint-rules.md` for route, endpoint, command, and query placement.
- `references/test-doc-adr-routing.md` for test placement, documentation placement, and ADR impact.
- `references/validation.md` for ADR-0003 and Web host validation commands.
- `references/output-formats.md` for exact response formats.

## Guardrails

- Keep recommendations simple.
- Prefer the narrowest owning scope.
- Do not invent folders when an existing convention is clear.
- Do not move artifacts only for symmetry.
- Do not create shared abstractions before there are at least two real consumers or a clear platform requirement.
- Do not create ADR-0003 `Contracts`, `Client`, or `Api` projects only for symmetry.
- Do not place reusable module behavior in a host page or React page.
- Do not place app-specific behavior in shared modules.
- Do not place backend implementation details in `src/Interfaces/Interfaces.*`, `src/Modules/<Module>/Interface`, or a React Web host.
- Do not place technology adapters in Domain or Application.
- Do not place public API contract DTOs in Domain for ADR-0003 migrated module boundaries.
- Do not place business rules in controllers, repositories, DTOs, migrations, adapters, UI components, React components, pages, clients, contracts, or localization resources.
- Do not place reusable React UI in a routable page when a feature, host component, module Interface, or interface package scope owns it.
- Do not place reusable Razor/module UI in host `Pages` when `src/Modules/<Module>/Interface/Components` owns it.
- Do not mix unrelated module UI in the same feature, Interface, or page folder; keep `Medical`, `Knowledge`, and other module-owned UI artifacts apart.
- Do not mix submodule/capability UI into a parent module’s generic UI folder when it has its own workflow, state, API wrapper, route group, or future independent evolution; use a capability folder such as `Interface/Components/<Capability>` or `features/<module>/<capability>`.
- Do not edit generated Web host API/client/hook code manually.
- Do not let a React Web host import backend Application, Domain, Infrastructure, Api, server projects, persistence libraries, or server-only modules.
- If placement is ambiguous and ownership affects architecture, ask for clarification unless a safe narrow assumption is obvious.

## Evidence of Use

When applying this skill, include this line in the plan or final response:

```txt
Applied skill: platform-artifact-placement-steward
```

Also include any validation command that should be run.

## Style

Be direct and practical. Prefer tables for multiple artifacts. Explain reasons using ownership, dependency direction, state change, reuse, host composition, route composition, persistence, interface boundary, transport contract, modular UI boundary, React/Web host module/submodule boundary, source of truth, or platform convention.
