---
name: platform-artifact-placement-steward
description: Classifies new or changed platform artifacts and recommends their correct repository location, ownership boundary, dependencies, related tests, documentation impact, ADR impact, and ADR-0003 frontend/backend isolation impact. Use before creating, moving, or reviewing source files, tests, docs, routes, modules, app-specific artifacts, identity artifacts, interface packages, contracts, clients, APIs, infrastructure adapters, UI components, commands, queries, DTOs, repositories, DbContexts, or background jobs.
---

# Platform Artifact Placement Steward

## Purpose

Classify every new or changed artifact before adding, moving, or reviewing it.

This skill protects the repository architecture by deciding where an artifact belongs, what owns it, what dependencies are allowed, and what supporting tests or documentation should be created.

It is not a code generator. It is a placement, ownership, and architecture-conformance steward.

## Use When

Use this skill when the user asks to:

- decide where a new file, class, component, page, DTO, command, query, port, adapter, repository, DbContext, background job, test, or document should live
- review whether existing files are placed correctly
- move artifacts to the correct module, app, host, identity, interface package, platform, library, or test location
- classify implementation artifacts generated from a roadmap or task plan
- check whether a PR violates repository structure rules
- derive related test and documentation placement for a production artifact
- detect whether an artifact placement implies an ADR
- classify or review ADR-0003 `Contracts`, `Client`, `Api`, `Interface`, or frontend/backend isolation artifacts

## Relationship to Other Skills

Use this skill before implementation artifacts are created.

- Journey Curator identifies the user workflow and design objective.
- Roadmap Curator derives features and implementation tasks.
- ADR Steward records new or changed architecture decisions.
- Artifact Placement Steward classifies where each artifact belongs.
- Frontend/Backend Isolation checks Interface, Contracts, Api, Application, and backend dependency boundaries.
- Localization Maintenance checks UI text, localization resource ownership, and localizer usage.
- Test Curator derives required unit and hot-test scenarios for placed artifacts.

Do not duplicate roadmap, journey, ADR, localization, frontend/backend isolation, or test planning responsibilities here.

## Architecture Sources of Truth

Use these repository sources when they apply:

- `AGENTS.md` for global architecture rules.
- `docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md` for ADR-0003 frontend/backend isolation rules.
- `docs/platform/modules/platform/conventions/adr-0003-implementation-conventions.md` for ADR-0003 implementation conventions.
- `docs/platform/modules/platform/validation/adr-0003-interface-boundary-validation.md` for ADR-0003 static boundary validation.
- Existing folder/project conventions in the owning module, app, host, identity context, interface package, library, test project, or docs tree.

When this skill conflicts with an accepted ADR, follow the ADR and report that the skill guidance must be updated.

## Core Rule

Classify first. Place second. Implement third.

Every placement recommendation must answer:

1. What artifact type is this?
2. Which app, module, host, identity context, interface package, library, or platform scope owns it?
3. What is the preferred location?
4. Why does it belong there?
5. What dependencies are allowed?
6. What dependencies are not allowed?
7. Where should related tests live?
8. Is documentation required?
9. Is an ADR required or recommended?
10. Does ADR-0003 apply?

## Scope Classification

Before choosing a path, classify scope:

- App-specific: owned by `src/Apps/<App>`.
- Module-specific: owned by `src/Modules/<Module>`.
- Host-specific: owned by `src/Hosts/<Host>`.
- Identity-specific: owned by `src/Identity/<BoundedContext>`.
- Interface package: owned by `src/Interfaces/Interfaces.<Name>`.
- Shared platform primitive: owned by `src/Modules/Base`, `src/Libraries/<Library>`, or an established platform module.
- Library capability: owned by `src/Libraries/<Library>`.
- Documentation: owned by the existing scoped `docs/...` source of truth.
- Test artifact: owned by `tests/...` near the production scope it verifies.

If the scope is unclear, ask for the owning app, module, host, identity context, interface package, library, or documentation scope unless a safe assumption is obvious.

## ADR-0003 Classification

For migrated modules, ADR-0003 introduces explicit frontend/backend boundary projects:

```txt
src/Modules/<Module>/Contracts/<Module>.Contracts.csproj
src/Modules/<Module>/Client/<Module>.Client.csproj
src/Modules/<Module>/Api/<Module>.Api.csproj
src/Modules/<Module>/Interface/<Module>.Interface.csproj
src/Modules/<Module>/Application/<Module>.Application.csproj
src/Modules/<Module>/Domain/<Module>.Domain.csproj
src/Modules/<Module>/Infrastructure/<Module>.Infrastructure.csproj
src/Modules/<Module>/Module/<Module>.Module.csproj
```

Do not create a project only for symmetry. Use these projects where the module has the corresponding current concern.

The ADR-0003 target boundary is:

```txt
Interface/Razor UI -> <Module>.Client -> HTTP -> <Module>.Api -> Application -> Domain
Interface/Razor UI -> <Module>.Contracts
<Module>.Client -> <Module>.Contracts
<Module>.Api -> <Module>.Contracts
<Module>.Infrastructure -> <Module>.Application/Domain
<Module>.Module -> <Module>.Api/Application/Domain/Infrastructure
```

Frontend hosts must be able to reference `Interface`, `Client`, and `Contracts` projects without pulling backend projects.

## Artifact Placement Rules

| Artifact type | Preferred location | Responsibility |
| --- | --- | --- |
| Domain entity, enum, value object, pure model, pure rule | `src/Modules/<Module>/Domain/...`, `src/Apps/<App>/Domain/...`, or `src/Identity/<BoundedContext>/Domain/...` | Own state and pure business behavior for the owning boundary |
| Domain event | `Domain/Events` under the owning module, app, or identity context | Represent a business fact inside the owning boundary |
| Domain mapper | Existing `Domain/Mappers` convention only when mapping is pure and technology-free | Translate between domain-owned shapes without infrastructure, transport, or UI coupling |
| Internal domain DTO/model | Existing `Domain/Dto` or `Domain/Models` convention only when already established and not public API contract state | Carry domain-owned internal data without transport behavior |
| Public API request DTO | `src/Modules/<Module>/Contracts` for migrated ADR-0003 modules | Represent serialization-safe request input for the module API |
| Public API response DTO | `src/Modules/<Module>/Contracts` for migrated ADR-0003 modules | Represent serialization-safe response output for the module API |
| Public API result/error DTO | `src/Modules/<Module>/Contracts` for migrated ADR-0003 modules | Represent transport-safe success and failure shapes |
| Public API enum | `src/Modules/<Module>/Contracts` for migrated ADR-0003 modules | Represent public transport enum values, not domain behavior |
| Module API route constants | `src/Modules/<Module>/Contracts` when shared by Client and Api | Provide one route source for the module HTTP boundary |
| Contract transport-only value shape | `src/Modules/<Module>/Contracts` | Carry serialization-safe values without business behavior |
| Application command | `Application/Commands` under the owning boundary | Change state through one use case |
| Application query | `Application/Queries` under the owning boundary | Return data without changing observable state |
| Application contract/port | `Application/Abstractions` or existing `Application/Contracts` convention | Define stable dependencies needed by application logic |
| Application event | `Application/Events` | Represent use-case-level notifications or integration points |
| Client HTTP adapter | `src/Modules/<Module>/Client` | Provide frontend-safe HTTP methods for module use cases |
| Client result/failure mapping | `src/Modules/<Module>/Client` unless shared as transport contract | Surface validation, auth, conflict, timeout, cancellation, transport, deserialization, and unexpected failures intentionally |
| Api endpoint mapping | `src/Modules/<Module>/Api` | Register Minimal API endpoints and map HTTP contracts to Application commands/queries |
| Api response/failure mapping | `src/Modules/<Module>/Api` | Convert Application outcomes to structured HTTP responses without leaking internals |
| Infrastructure adapter | `Infrastructure/...` | Implement application contracts with concrete technology |
| Repository | `Infrastructure/Repositories` | Persist and retrieve one persistence concern only |
| DbContext/data mapping/migration | `Infrastructure/Data` or existing `Infrastructure/Migrations` convention | Own EF and persistence mapping details |
| Infrastructure validator | Existing `Infrastructure/Validators` convention when validation depends on infrastructure or external state | Validate infrastructure-backed constraints outside pure domain rules |
| Background job | `Infrastructure/BackgroundJobs`, `src/Modules/Base/BackgroundJobs`, or host infrastructure when host-owned | Run asynchronous work with explicit ownership and dependency boundaries |
| Module interface component | `src/Modules/<Module>/Interface/Components` | Render reusable frontend-safe UI owned by that module |
| Module interface plugin | Existing `src/Modules/<Module>/Interface/Plugins` convention | Expose module-owned UI integration points |
| Module interface model/service | `src/Modules/<Module>/Interface/Models` or `Interface/Services` | Support reusable module UI without backend implementation coupling |
| Interface package component | `src/Interfaces/Interfaces.<Name>/Components` | Provide reusable cross-app UI in an interface package |
| Interface package abstraction | `src/Interfaces/Interfaces.<Name>/Abstractions` | Define UI-facing abstractions for the interface package |
| Interface package model | `src/Interfaces/Interfaces.<Name>/Models` | Carry UI-facing state for the interface package |
| Interface package service | `src/Interfaces/Interfaces.<Name>/Services` | Support interface package behavior without backend implementation coupling |
| Interface localization resource | Existing `src/Interfaces/Interfaces.<Name>/Resources` or `Localization` convention | Own UI text/resources for the interface package |
| Module DI | Existing `Interface/DependencyInjection`, `Infrastructure/DependencyInjection`, or module composition convention | Register the owning layer’s services |
| Module composition | `src/Modules/<Module>/Module` or existing aggregate project convention | Package module API, Application, Domain, and Infrastructure registrations for host startup |
| Routable host page | `src/Hosts/<Host>/Pages/...` | Compose route-level UI and route-specific state |
| Host route constants/builders | `src/Hosts/<Host>/Common/Routes` | Centralize host-owned UI navigation paths |
| Host authorization/pipeline/platform integration | Existing `src/Hosts/<Host>/Authorization`, `Infrastructure/Pipeline`, or `Infrastructure/Platform` convention | Compose host-specific runtime behavior |
| App-specific artifact | `src/Apps/<App>/...` | Own behavior specific to that app boundary |
| Identity artifact | `src/Identity/<BoundedContext>/...` | Own identity-specific behavior |
| Shared entity transport primitive | `src/Libraries/Entities/Entities.Contracts` when justified | Provide shared serialization-safe entity transport primitives |
| Entity server artifact | `src/Libraries/Entities/Entities.Server` | Own EF, repositories, caching, backend services, server commands/queries, and server notification infrastructure |
| Shared platform primitive | `src/Modules/Base/...` or existing platform module convention | Provide cross-module primitives without module-specific business behavior |
| Library capability | `src/Libraries/<Library>/...` | Provide reusable library behavior with clear ownership and no app/module leakage |
| Source generator | Existing `src/Libraries/<Library>/<Library>.Generators` convention | Generate code for the owning library capability |
| Test | Existing `tests/...` convention nearest the production scope | Verify the corresponding production artifact |
| Documentation | Existing scoped `docs/...` source of truth | Record product, platform, architecture, operational, or developer guidance |

## ADR-0003 Project Rules

### Contracts Project

`<Module>.Contracts` owns transport-safe artifacts only when they are part of the public client/server contract.

Allowed:

- request DTOs
- response DTOs
- result DTOs
- error DTOs or error codes
- public API enums
- route constants
- transport-only value shapes
- justified references to `Entities.Contracts`

Forbidden:

- EF entities
- domain entities with behavior
- repositories
- application services
- application commands or handlers
- domain services
- backend command/query handlers
- infrastructure adapters
- UI components
- UI state
- business-rule implementations
- persistence models
- EF attributes or database behavior
- server service abstractions
- infrastructure options
- vendor SDK types unless explicitly approved as contract-safe

Contracts are serialization shapes, not the source of business behavior.

### Client Project

`<Module>.Client` owns frontend-safe HTTP access to the module API.

Allowed:

- `<Module>.Contracts`
- BCL/runtime libraries
- HTTP client abstractions
- approved frontend-safe libraries
- use-case-oriented HTTP methods
- request/response serialization
- structured client result mapping
- cancellation propagation
- intentional failure surfacing

Forbidden:

- Application
- Domain
- Infrastructure
- Api
- server projects
- backend-only cross-module projects
- persistence libraries
- UI components unless explicitly approved as frontend-safe composition
- business rules
- UI loading state
- Razor components
- backend command/query handlers
- persistence logic
- raw domain/entity exposure

Every async client method must accept `CancellationToken`.

Client code must explicitly handle or surface:

- validation failure
- unauthorized
- forbidden
- not found
- conflict/concurrency failure
- timeout
- cancellation
- network/transport failure
- deserialization failure
- unexpected server failure

### Api Project

`<Module>.Api` owns Minimal API endpoint registration and maps HTTP contracts to Application commands/queries.

Allowed:

- `<Module>.Contracts`
- `<Module>.Application`
- approved host/API abstractions
- minimal framework packages needed for endpoint mapping
- route groups
- endpoint mappings
- DTO-to-command/query mapping
- cancellation-token propagation
- structured response/failure mapping

Forbidden:

- Interface
- Client
- UI projects
- Infrastructure implementation details unless only used through composition-approved registration
- persistence models in responses
- direct response exposure of domain entities
- business rules
- UI/client logic
- persistence behavior
- reusable domain state transitions

Endpoints must be thin adapters. They must not contain business rules.

### Interface Project

`<Module>.Interface` owns reusable frontend UI components and UI-facing presentation behavior for the module.

Allowed:

- `<Module>.Contracts`
- `<Module>.Client` when the UI calls the migrated HTTP boundary
- approved frontend-safe UI/base libraries
- explicitly approved frontend-safe cross-module interfaces/contracts
- Razor components
- UI-facing view models
- UI event handling
- loading, success, empty, and error presentation states
- calls to client use cases

Forbidden:

- same-module or cross-module Application
- Domain
- Infrastructure
- Api
- server-only libraries, including `Entities.Server`
- persistence packages
- backend service implementations
- raw backend routes for migrated workflows
- duplicated business rules
- domain state transitions
- command/query handlers
- repositories
- server composition

Razor/UI code calls client methods for migrated workflows. It must not construct raw backend routes for migrated workflows when a module client exists.

### Application Project

Application owns use-case orchestration through commands and queries.

Allowed:

- Domain
- application-owned abstractions/contracts

Forbidden:

- Interface
- Client
- Api
- Infrastructure implementations
- HTTP
- UI
- EF/database details
- vendor SDKs
- filesystems
- clocks
- network dependencies except through abstractions

### Domain Project

Domain owns module business state, invariants, and pure behavior.

Allowed:

- BCL/runtime libraries
- domain-owned value types

Forbidden:

- Application
- Contracts
- Client
- Api
- Interface
- Infrastructure
- EF
- HTTP
- UI
- vendor SDKs
- filesystems
- clocks
- network
- host dependencies

Domain is the source of truth for business rules and state transitions.

### Infrastructure Project

Infrastructure implements Application abstractions using concrete technology.

Allowed:

- Application
- Domain
- persistence/framework packages
- external SDK packages
- configuration shapes needed by adapters

Forbidden:

- Interface
- Client
- business rules that belong in Domain/Application
- contract DTOs as persistence models unless explicitly mapped and justified

### Module and Composition Concerns

Module/composition artifacts wire the module together but do not own behavior.

Allowed:

- module-owned Api
- Application
- Domain
- Infrastructure
- approved platform composition libraries

Forbidden:

- Interface-only UI concerns unless a host explicitly composes UI separately
- direct business rules
- persistence behavior outside adapter registration
- command/query logic
- endpoint handler bodies
- DTO definitions
- UI state

## General Dependency Rules

Prefer inward, stable dependencies:

- Domain must stay pure and should not depend on Application, Contracts, Client, Api, Infrastructure, Interface, Host, App runtime composition, or concrete technology packages.
- Application may depend on Domain and application-owned abstractions/contracts.
- Contracts must stay serialization-safe and must not contain behavior or backend implementation dependencies.
- Client may depend on Contracts and frontend-safe HTTP abstractions.
- Api may depend on Contracts and Application and must remain a thin HTTP adapter.
- Infrastructure may depend on Application contracts and Domain to implement concrete adapters.
- Module Interface artifacts may depend on Contracts, Client, UI-facing models/services, and approved frontend-safe libraries according to ADR-0003 and module convention.
- `src/Interfaces/Interfaces.*` packages must remain UI-facing and must not depend on backend implementation details, persistence, host pages, or infrastructure adapters.
- Host pages compose route-level UI and should not own reusable module or interface package behavior.
- Apps own app-specific behavior and should not become a dumping ground for reusable module logic.
- Identity artifacts stay under Identity bounded contexts.
- Shared libraries and `src/Modules/Base` must not contain module-specific business behavior.
- Tests mirror or reference the production scope they verify.
- Documentation follows the existing scoped source of truth and should not duplicate rules.

## Route and Endpoint Placement Rules

Module API routes use this prefix:

```txt
/api/modules/<module>
```

The `<module>` segment is lowercase kebab-case and identifies the module route ownership.

Place route artifacts as follows:

- Host UI navigation routes: `src/Hosts/<Host>/Common/Routes`
- Module API route constants shared by Client and Api: `src/Modules/<Module>/Contracts`
- Endpoint registration methods such as `Map<Module>Api`: `src/Modules/<Module>/Api`
- Host startup calls to endpoint registration: host or module composition root according to existing convention

Endpoint mapping expectations:

- Use explicit endpoint registration methods such as `Map<Module>Api`.
- Keep endpoint handlers thin.
- Map contract DTOs to Application commands or queries at the API boundary.
- Propagate `CancellationToken` from the HTTP request through Application calls.
- Return structured failures.
- Avoid domain/entity leakage in responses.
- Avoid UI/client dependencies in API projects.

Command/query expectations:

- Command endpoints mutate state and must not act as data query APIs.
- Query endpoints return data and must not mutate observable state.
- Combined command/query behavior requires redesign before implementation.

## Classification Procedure

When classifying an artifact:

1. Read the requested artifact name, purpose, behavior, and intended consumers.
2. Identify whether it owns business state, changes state, reads data, adapts technology, transports data, calls HTTP, registers endpoints, renders UI, composes routes, registers services, localizes UI, documents behavior, or verifies behavior.
3. Identify the owning scope: app, module, host, identity context, interface package, library, platform, test, or docs.
4. Determine whether ADR-0003 applies because the artifact is part of a migrated module frontend/backend boundary.
5. Prefer the existing convention inside that scope before inventing a new folder.
6. Apply the placement table and ADR-0003 rules.
7. Check whether the artifact violates layer, ownership, CQRS, frontend/backend isolation, or source-of-truth rules.
8. Identify required related artifacts, tests, docs, localization resources, boundary validation, or ADRs.
9. Return the smallest correct placement recommendation.

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

ADR-0003 impact:

Validation impact:

Notes:
```

Use this format for multiple artifacts:

```md
## Artifact Placement Plan

| Artifact | Classification | Owning scope | Preferred location | Related tests | ADR impact | ADR-0003 impact |
|---|---|---|---|---|---|---|

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

ADR-0003 boundary risks:

Suggested moves:

Tests/docs/ADR impact:

Validation impact:
```

Include ADR-0003 boundary validation impact when reviewing changes under:

- `src/Modules/*/Interface/*.csproj`
- `src/Modules/*/Interface/**/*.cs`
- `src/Modules/*/Interface/**/*.razor`
- `src/Modules/*/Contracts`
- `src/Modules/*/Client`
- `src/Modules/*/Api`

## ADR Impact Rules

Recommend an ADR when placement introduces or changes a stable architecture rule, such as:

- a new module boundary
- a new app boundary
- a new interface package boundary
- a new shared platform primitive
- a new library capability boundary
- a new cross-module dependency direction
- a new frontend/backend isolation pattern
- a new persistence strategy
- a new host composition pattern
- a new identity boundary
- a new rule that future implementations must follow

Do not recommend an ADR for ordinary placement that already follows the table, an accepted ADR, and existing repository convention.

ADR-0003 already decides the module `Contracts` / `Client` / `Api` / `Interface` isolation pattern. Do not request a new ADR merely to apply ADR-0003 to a migrated module. Recommend ADR review only when a module needs to deviate from ADR-0003 or add a new stable platform rule.

## ADR-0003 Validation Rules

The ADR-0003 validator is:

```bash
python3 tools/architecture/validate_interface_boundaries.py
```

The validator configuration is:

```txt
tools/architecture/interface_boundary_config.json
```

It scans:

- `src/Modules/*/Interface/*.csproj`
- `src/Modules/*/Interface/**/*.cs`
- `src/Modules/*/Interface/**/*.razor`

It reports forbidden backend dependencies from module Interface projects and source files, including:

- `ProjectReference` violations in `.csproj` files
- `CSharpNamespaceImport` violations in `.cs` files
- `CSharpFullyQualifiedUsage` violations in `.cs` files
- `RazorNamespaceImport` violations in `.razor` files
- `RazorFullyQualifiedUsage` violations in `.razor` files

When reviewing migrated module Interface changes, recommend this validation command unless the user explicitly asked for static discussion only.

Rollout modes:

- `ReportOnly`: violations are actionable but do not fail the command.
- `FailFast`: violations cause a non-zero exit code.

Temporary exceptions must be recorded in the validator configuration with the path, forbidden dependency or namespace, reason, and planned removal condition.

## Test Placement Rules

For production artifact placement, suggest corresponding test placement using the existing test project convention first:

- Module tests: `tests/Modules/<Module>.Tests/...`
- Module layer subfolders, when useful inside the test project: `Application`, `Domain`, `Infrastructure`, `Interface`, `Contracts`, `Client`, `Api`, `TestHelpers`
- Host tests: `tests/Hosts/<Host>.Tests/...`
- Interface package tests: `tests/Interfaces/Interfaces.<Name>.Tests/...`
- App-specific tests: `tests/Apps/<App>/...` when an app test convention exists, otherwise call out the missing convention
- Identity tests: `tests/Identity/<BoundedContext>/<Layer>/...` or the existing bounded-context test layout
- Library tests: `tests/Libraries/<Library>/<Library>.<Project>.Tests/...` or the established library convention
- Platform/base tests: `tests/Modules/Base.Tests/...` or the established platform convention
- ADR-0003 boundary validation: `tools/architecture/validate_interface_boundaries.py`
- Hot-test scenario documentation: use the Test Curator when verification depends on runtime infrastructure, browser flow, external services, DB behavior, queues, files, clocks, or network timing

Do not create a new test project layout when an existing test project for the scope already exists.

## Documentation Placement Rules

Documentation should follow scoped ownership and existing sources of truth:

- App/module docs: `docs/apps/<app>/modules/<module>/...`
- Platform module docs: `docs/platform/modules/<module>/...`
- Platform conventions: `docs/platform/modules/platform/conventions/...`
- Architecture decisions: existing ADR convention, including `docs/architecture/decisions` or scoped `docs/platform/modules/<module>/decisions`
- ADR-0003 decision: `docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md`
- ADR-0003 implementation conventions: `docs/platform/modules/platform/conventions/adr-0003-implementation-conventions.md`
- ADR-0003 validation guidance: `docs/platform/modules/platform/validation/adr-0003-interface-boundary-validation.md`
- Developer guidance: `docs/development/...` or existing `docs/platform/developer-guides/...` convention
- Operations/runbooks: `docs/runbooks/...` or existing `docs/platform/operations/...` convention
- Journey, roadmap, test, and decision docs: place under the owning scoped docs tree when one exists

Do not duplicate the same rule in multiple places. Use indexes and links for navigation.

If an established documentation source of truth already exists, update or reference it instead of creating a competing location.

## Conflict Detection

Report a possible placement conflict when:

- domain code depends on infrastructure, UI, host, app-specific behavior, vendor SDKs, clocks, filesystems, network clients, or persistence packages
- domain code depends on Contracts, Client, or Api
- application code depends on infrastructure implementation rather than application-owned abstractions
- application code depends on Interface, Client, Api, HTTP, UI, EF/database details, vendor SDKs, filesystems, clocks, or network dependencies outside abstractions
- Contracts contain domain entities, persistence models, business rules, EF attributes, application handlers, repositories, infrastructure adapters, UI state, or server abstractions
- Client code depends on Application, Domain, Infrastructure, Api, server projects, persistence libraries, or backend-only cross-module projects
- Client methods hide failures, omit cancellation tokens, or expose raw domain/entity state
- Api endpoint handlers contain business rules, return domain entities directly, depend on Interface/Client/UI, or leak persistence details
- module Interface code depends on Application, Domain, Infrastructure, Api, backend implementations, persistence packages, or `Entities.Server`
- Razor/UI code constructs raw backend routes for migrated workflows when a module client exists
- host pages contain reusable module or interface package UI logic
- `src/Interfaces/Interfaces.*` packages depend on backend implementation, persistence, host pages, or infrastructure adapters
- shared libraries or `src/Modules/Base` contain module-specific business behavior
- commands return query models while also changing state without an explicit, narrow reason
- queries change observable state
- repositories own multiple unrelated persistence concerns
- app-specific artifacts leak into reusable modules
- identity-specific behavior is placed under ordinary modules
- tests are detached from the production scope they verify
- documentation creates a competing source of truth
- localization resources are split across multiple owners for the same UI text
- an artifact named `Manager`, `Helper`, `Util`, `Common`, or vague `Service` hides multiple responsibilities

Use “possible conflict” when evidence is incomplete.

## Guardrails

Keep recommendations simple.

Do not invent new folders when an existing module, app, interface, identity, host, library, test, or docs convention is clear.

Do not move artifacts just for symmetry if the existing convention is stable and understandable.

Do not create shared abstractions before there are at least two real consumers or a clear platform requirement.

Do not create ADR-0003 `Contracts`, `Client`, or `Api` projects only for symmetry. Create them when the module has the corresponding current concern.

Do not place reusable module behavior in a host page.

Do not place app-specific behavior in shared modules.

Do not place backend implementation details in `src/Interfaces/Interfaces.*`.

Do not place backend implementation details in `src/Modules/<Module>/Interface`.

Do not place technology adapters in Domain or Application.

Do not place public API contract DTOs in Domain for ADR-0003 migrated module boundaries.

Do not place business rules in controllers, repositories, DTOs, migrations, adapters, UI components, clients, contracts, or localization resources.

If placement is ambiguous, prefer the narrowest owning scope and ask for clarification when ownership affects architecture.

## Style

Be direct and practical.

Prefer tables for multiple artifacts.

Explain the reason in architecture terms: ownership, dependency direction, state change, reuse, host composition, persistence, interface boundary, transport contract, source of truth, or platform convention.