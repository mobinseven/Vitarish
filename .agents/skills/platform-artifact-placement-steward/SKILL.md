---
name: platform-artifact-placement-steward
description: Classifies new or changed platform artifacts and recommends their correct repository location, ownership boundary, dependencies, related tests, documentation impact, and ADR impact. Use before creating, moving, or reviewing source files, tests, docs, routes, modules, app-specific artifacts, identity artifacts, infrastructure adapters, UI components, commands, queries, DTOs, repositories, DbContexts, or background jobs.
---

# Platform Artifact Placement Steward

## Purpose

Classify every new or changed artifact before adding, moving, or reviewing it.

This skill protects the repository architecture by deciding where an artifact belongs, what owns it, what dependencies are allowed, and what supporting tests or documentation should be created.

It is not a code generator. It is a placement, ownership, and architecture-conformance steward.

## Use When

Use this skill when the user asks to:

- decide where a new file, class, component, page, DTO, command, query, service, repository, DbContext, background job, test, or document should live
- review whether existing files are placed correctly
- move artifacts to the correct module, app, host, identity, platform, or test location
- classify implementation artifacts generated from a roadmap or task plan
- check whether a PR violates repository structure rules
- derive related test and documentation placement for a production artifact
- detect whether an artifact placement implies an ADR

## Relationship to Other Skills

Use this skill before implementation artifacts are created.

- Journey Curator identifies the user workflow and design objective.
- Roadmap Curator derives features and implementation tasks.
- Artifact Placement Steward classifies where each artifact belongs.
- Test Curator derives required unit and hot-test scenarios for placed artifacts.
- ADR Steward records new or changed architecture decisions.

Do not duplicate roadmap, journey, ADR, or test planning responsibilities here.

## Core Rule

Classify first. Place second. Implement third.

Every placement recommendation must answer:

1. What artifact type is this?
2. Which app, module, host, identity context, or platform scope owns it?
3. What is the preferred location?
4. Why does it belong there?
5. What dependencies are allowed?
6. What dependencies are not allowed?
7. Where should related tests live?
8. Is documentation required?
9. Is an ADR required or recommended?

## Scope Classification

Before choosing a path, classify scope:

- App-specific: owned by `src/Apps/<App>`.
- Module-specific: owned by `src/Modules/<Module>`.
- Host-specific: owned by `src/Hosts/<Host>`.
- Identity-specific: owned by `src/Identity/<BoundedContext>`.
- Shared platform primitive: owned by `src/Libraries` or `src/Modules/Base`.
- Documentation: owned by `docs/...`.
- Test artifact: owned by `tests/...`.

If the scope is unclear, ask for the owning app/module/host/context unless a safe assumption is obvious.

## Artifact Placement Rules

| Artifact type | Preferred location | Responsibility |
| --- | --- | --- |
| Domain entity, enum, pure model, pure rule | `src/Modules/<Module>/Domain` | Own module state and pure business behavior |
| Read/write DTO or descriptor | `src/Modules/<Module>/Domain/Dto`, `Domain/Models`, or existing module convention | Carry data across explicit boundaries without business behavior |
| Application command | `src/Modules/<Module>/Application/Commands` | Change state through a single use case |
| Application query | `src/Modules/<Module>/Application/Queries` | Return data without changing observable state |
| Application contract/port | `src/Modules/<Module>/Application/Abstractions` or `Application/Contracts` | Define stable dependencies needed by application logic |
| Application event | `src/Modules/<Module>/Application/Events` | Represent use-case-level notifications or integration points |
| Infrastructure adapter | `src/Modules/<Module>/Infrastructure` | Implement application contracts with concrete technology |
| Repository | `src/Modules/<Module>/Infrastructure/Repositories` | Persist and retrieve one persistence concern only |
| DbContext/data mapping | `src/Modules/<Module>/Infrastructure/Data` | Own EF and persistence mapping details |
| Background job | `src/Modules/<Module>/Infrastructure/BackgroundJobs` | Run infrastructure-backed asynchronous work |
| Reusable module UI | `src/Modules/<Module>/Interface/Components` | Render reusable components without owning host routes |
| UI-facing interface model/service | `src/Modules/<Module>/Interface/Models` or `Interface/Services` | Support reusable module UI only |
| Module DI | `src/Modules/<Module>/Interface/DependencyInjection`, `Infrastructure/DependencyInjection`, or `Module` | Register the owning layer’s services |
| Module composition | `src/Modules/<Module>/Module` | Package the module layer projects for aggregation |
| Routable host page | `src/Hosts/Main/Pages` | Compose route-level UI |
| Route constants/builders | `src/Hosts/Main/Common/Routes` | Centralize host-owned navigation paths |
| Shared platform primitive | `src/Libraries` or `src/Modules/Base` | Provide cross-module primitives without module-specific behavior |
| App-specific artifact | `src/Apps/<App>` | Own behavior specific to that app boundary |
| Identity artifact | `src/Identity/<BoundedContext>` | Own identity-specific behavior |
| Test | `tests/...` | Verify the corresponding production artifact |
| Documentation | `docs/...` | Record product, platform, architectural, operational, or developer guidance |

## Dependency Rules

Prefer inward, stable dependencies:

- Domain must stay pure and should not depend on Application, Infrastructure, Interface, Host, App, or concrete technology packages.
- Application may depend on Domain and application-owned abstractions/contracts.
- Infrastructure may depend on Application contracts and Domain to implement concrete adapters.
- Interface components may depend on UI-facing models/services and application-facing contracts as allowed by module convention.
- Host pages compose route-level UI and should not own reusable module UI behavior.
- Apps own app-specific behavior and should not become a dumping ground for reusable module logic.
- Identity artifacts stay under Identity bounded contexts.
- Shared libraries must not contain module-specific business behavior.
- Tests mirror or reference the production scope they verify.
- Documentation follows the scoped App/Module/Platform documentation structure.

## Classification Procedure

When classifying an artifact:

1. Read the requested artifact name, purpose, and behavior.
2. Identify whether it owns business state, changes state, reads data, adapts technology, renders UI, composes routes, registers services, or documents behavior.
3. Identify the owning scope: app, module, host, identity context, platform, test, or docs.
4. Apply the placement table.
5. Check whether the artifact violates layer or ownership rules.
6. Identify required related artifacts, tests, docs, or ADRs.
7. Return the smallest correct placement recommendation.

## Placement Output

Use this format for a single artifact:

```md
## Artifact Placement

Artifact:

Classification:

Owning scope:

Preferred location:

Reason:

Allowed dependencies:

Disallowed dependencies:

Related tests:

Documentation impact:

ADR impact:

Notes:
```

Use this format for multiple artifacts:

```md
## Artifact Placement Plan

| Artifact | Classification | Owning scope | Preferred location | Related tests | ADR impact |
|---|---|---|---|---|---|

## Placement Notes

## Risks / Conflicts

## Recommended Next Step
```

## Placement Review Output

When reviewing existing files or a PR, use:

```md
## Artifact Placement Review

Classification summary:

Correct placements:

Possible misplacements:

Ownership conflicts:

Dependency risks:

Suggested moves:

Tests/docs/ADR impact:
```

## ADR Impact Rules

Recommend an ADR when placement introduces or changes a stable architecture rule, such as:

- a new module boundary
- a new app boundary
- a new shared platform primitive
- a new cross-module dependency direction
- a new persistence strategy
- a new host composition pattern
- a new identity boundary
- a new rule that future implementations must follow

Do not recommend an ADR for ordinary placement that already follows the table.

## Test Placement Rules

For production artifact placement, suggest corresponding test placement:

- Domain rule/entity tests: `tests/Modules/<Module>/Domain.Tests/...`
- Application command/query tests: `tests/Modules/<Module>/Application.Tests/...`
- Infrastructure adapter/repository/data tests: `tests/Modules/<Module>/Infrastructure.Tests/...`
- Interface component tests: `tests/Modules/<Module>/Interface.Tests/...` or existing UI test convention
- Host page flow tests: `tests/Hosts/<Host>/...` or hot-test scenario documentation
- App-specific tests: `tests/Apps/<App>/...`
- Identity tests: `tests/Identity/<BoundedContext>/...`
- Platform library tests: `tests/Libraries/...` or existing library convention

If runtime dependencies are required, recommend the Test Curator for hot-test scenario specification.

## Documentation Placement Rules

Documentation should follow scoped ownership:

- App/module docs: `docs/apps/<app>/modules/<module>/...`
- Platform module docs: `docs/platform/modules/<module>/...`
- Global architecture docs: `docs/platform/architecture/...`
- Operational docs: `docs/platform/operations/...`
- Developer guidance: `docs/platform/developer-guides/...`

Do not duplicate the same rule in multiple places. Use indexes and links for navigation.

## Conflict Detection

Report a possible placement conflict when:

- domain code depends on infrastructure, UI, host, or app-specific behavior
- host pages contain reusable module UI logic
- shared libraries contain module-specific business behavior
- application commands return query models while also changing state without explicit reason
- queries change observable state
- repositories own multiple unrelated persistence concerns
- app-specific artifacts leak into reusable modules
- identity-specific behavior is placed under ordinary modules
- tests are detached from the production scope they verify
- documentation creates a competing source of truth

Use “possible conflict” when evidence is incomplete.

## Guardrails

Keep recommendations simple.

Do not invent new folders when an existing module convention is clear.

Do not move artifacts just for symmetry if the existing convention is stable and understandable.

Do not create shared abstractions before there are at least two real consumers or a clear platform requirement.

Do not place reusable module behavior in a host page.

Do not place app-specific behavior in shared modules.

Do not place technology adapters in Domain or Application.

If placement is ambiguous, prefer the narrowest owning scope.

## Style

Be direct and practical.

Prefer tables for multiple artifacts.

Explain the reason in architecture terms: ownership, dependency direction, state change, reuse, host composition, persistence, or boundary.
