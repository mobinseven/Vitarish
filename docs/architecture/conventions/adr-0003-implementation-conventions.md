# ADR-0003 implementation conventions

Date: 2026-06-25
Status: Active
App: Platform
Module: Platform
Capability: Module frontend/backend isolation implementation conventions
Owner: Architecture
Related ADR: `docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md`
Related validation: `docs/architecture/validation/adr-0003-interface-boundary-validation.md`

## Scope and authority

This document is the scoped source of truth for ADR-0003 implementation conventions proven by the Feed and Requests pilot migrations. It is platform architecture convention documentation, ADR implementation guidance, and validation operation documentation.

This document does not change ADR-0003. If a future migration needs a new architecture decision, update or supersede the ADR through the repository ADR process before changing these conventions.

## Artifact classification

| Artifact | Classification | Responsibility |
| --- | --- | --- |
| This document | Platform architecture convention documentation | Record the implementation conventions future module migrations must follow. |
| Project layout sections | ADR implementation guidance | Explain project ownership, dependency direction, and content boundaries for migrated modules. |
| Route, endpoint, contract, client, and UI sections | ADR implementation guidance | Explain how code implements the ADR-0003 frontend/backend boundary without adding new decisions. |
| Boundary validation section | Validation operation documentation | Explain how maintainers run, interpret, and promote static boundary enforcement. |
| Deferred items section | ADR implementation guidance | Preserve KISS/YAGNI boundaries by naming work that must not be added during ordinary ADR-0003 migrations. |

## Standard per-module project layout

Migrated modules use the following project layout where the module needs each concern:

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

Do not create a project only for symmetry. Existing Application, Domain, Infrastructure, and Module projects remain as applicable to the module's current behavior.

### Contracts project

| Rule | Convention |
| --- | --- |
| Responsibility | Own serialization-safe public API shapes for the module. |
| Allowed dependencies | BCL/runtime libraries, approved serialization-safe shared contract libraries, and `Entities.Contracts` where the shared entity transport primitive is justified. |
| Forbidden dependencies | Domain, Application, Infrastructure, Api, Interface, Server projects, EF packages/attributes, vendor SDK types unless explicitly approved as contract-safe. |
| Ownership boundary | The Contracts project is the source of truth for request, response, result, error, enum, route, and transport-only value shapes exposed by the module API. |
| Belongs there | Request DTOs, response DTOs, result DTOs, error DTOs or error codes, public API enums, route constants, transport-only value shapes, and justified references to `Entities.Contracts`. |
| Does not belong there | Domain entities, persistence models, application commands/handlers, business rules, EF/database behavior, server service abstractions, UI state, infrastructure options, or unapproved vendor SDK types. |

Naming guidance:

- Names should reflect transport/API meaning.
- Do not blindly copy persistence model names.
- Avoid names that imply domain ownership when the type is only a DTO.

### Client project

| Rule | Convention |
| --- | --- |
| Responsibility | Provide frontend-safe HTTP adapter methods for module use cases. |
| Allowed dependencies | `<Module>.Contracts`, BCL/runtime libraries, HTTP client abstractions, and approved frontend-safe libraries. |
| Forbidden dependencies | Application, Domain, Infrastructure, Api, Server projects, backend-only cross-module projects, persistence libraries, and UI component projects unless explicitly approved as frontend-safe composition. |
| Ownership boundary | The Client project centralizes module API route usage and failure mapping for frontend callers. |
| Belongs there | Use-case-oriented HTTP methods, request/response serialization, structured client result mapping, cancellation propagation, and intentional failure surfacing. |
| Does not belong there | Business rules, UI loading state, Razor components, backend command/query handlers, persistence logic, server service abstractions, or raw domain/entity exposure. |

Client requirements:

- Client projects are frontend-safe HTTP adapters.
- Client methods are use-case-oriented, not copies of backend handler names.
- Every async method accepts `CancellationToken`.
- Clients consume and return contract types or structured client results.
- Clients centralize route usage.
- Clients surface failures intentionally.

Every client migration must account for these failure categories:

- Validation failure
- Unauthorized
- Forbidden
- Not found
- Conflict/concurrency failure
- Timeout
- Cancellation
- Network/transport failure
- Deserialization failure
- Unexpected server failure

### Api project

| Rule | Convention |
| --- | --- |
| Responsibility | Own HTTP endpoint registration and map contract DTOs to Application commands/queries. |
| Allowed dependencies | `<Module>.Contracts`, `<Module>.Application`, approved host/API abstractions, and minimal framework packages needed for endpoint mapping. |
| Forbidden dependencies | Interface, Client, UI projects, Infrastructure implementation details unless only used through composition-approved registration, persistence models in responses, and domain/entity leakage in responses. |
| Ownership boundary | The Api project is a thin transport adapter between HTTP contracts and Application use cases. |
| Belongs there | Route groups, endpoint mappings, DTO-to-command/query mapping, cancellation-token propagation, structured response/failure mapping. |
| Does not belong there | Business rules, UI/client logic, persistence behavior, reusable domain state transitions, or direct response exposure of domain entities. |

### Interface project

| Rule | Convention |
| --- | --- |
| Responsibility | Own reusable frontend UI components and UI-facing presentation behavior for the module. |
| Allowed dependencies | `<Module>.Contracts`, `<Module>.Client` when the UI calls the migrated HTTP boundary, approved frontend-safe UI/base libraries, and explicitly approved frontend-safe cross-module interfaces/contracts. |
| Forbidden dependencies | Same-module or cross-module Application, Domain, Infrastructure, Api, server-only libraries including `Entities.Server`, persistence packages, and backend service implementations. |
| Ownership boundary | The Interface project is frontend-safe and must be referenceable by frontend hosts without pulling backend code. |
| Belongs there | Razor components, UI-facing view models, UI event handling, loading/success/empty/error presentation states, and calls to client use cases. |
| Does not belong there | Raw backend routes for migrated workflows, duplicated business rules, domain state transitions, command/query handlers, repositories, or server composition. |

UI interaction expectations:

- Razor/UI code calls client methods for migrated workflows.
- UI code handles loading, success, empty, and error states.
- UI code does not construct raw backend routes for migrated workflows.
- UI code does not duplicate business rules.

### Application project

| Rule | Convention |
| --- | --- |
| Responsibility | Orchestrate module use cases through commands and queries. |
| Allowed dependencies | Domain and application-owned abstractions/contracts. |
| Forbidden dependencies | Interface, Client, Api, Infrastructure implementations, HTTP, UI, EF/database details, vendor SDKs, filesystems, clocks, or network dependencies except through abstractions. |
| Ownership boundary | Application owns use-case orchestration, validation coordination, authorization/tenant checks where already assigned, and command/query boundaries. |
| Belongs there | Commands, queries, handlers, application result models, ports/abstractions, and use-case-level failure handling. |
| Does not belong there | Endpoint mapping, contract DTO serialization concerns, UI state, EF mappings, HTTP clients, or framework-specific persistence behavior. |

### Domain project

| Rule | Convention |
| --- | --- |
| Responsibility | Own module business state, invariants, and pure behavior. |
| Allowed dependencies | BCL/runtime libraries and domain-owned value types. |
| Forbidden dependencies | Application, Contracts, Client, Api, Interface, Infrastructure, EF, HTTP, UI, vendor SDKs, filesystems, clocks, network, or host dependencies. |
| Ownership boundary | Domain is the source of truth for business rules and state transitions. |
| Belongs there | Entities with behavior, value objects, domain enums, invariants, pure policies, and state transitions. |
| Does not belong there | DTOs as public API contracts, persistence mapping, endpoint handlers, client calls, UI state, or infrastructure behavior. |

### Infrastructure project

| Rule | Convention |
| --- | --- |
| Responsibility | Implement Application abstractions using concrete technology. |
| Allowed dependencies | Application, Domain, persistence/framework packages, external SDK packages, and configuration shapes needed by adapters. |
| Forbidden dependencies | Interface and Client projects; business rules that belong in Domain/Application; contract DTOs as persistence models unless explicitly mapped and justified. |
| Ownership boundary | Infrastructure owns technology adapters, not business decisions. |
| Belongs there | Repositories, DbContexts, EF mappings, external service adapters, background jobs, cache adapters, and concrete options. |
| Does not belong there | Public API DTO ownership, UI behavior, endpoint registration, or domain invariants. |

### Module and composition concerns

| Rule | Convention |
| --- | --- |
| Responsibility | Compose the module's API, Application, Domain, and Infrastructure registrations for host startup. |
| Allowed dependencies | Module-owned Api, Application, Domain, Infrastructure, and approved platform composition libraries. |
| Forbidden dependencies | Interface-only UI concerns unless a host explicitly composes UI separately; direct business rules; persistence behavior outside adapter registration. |
| Ownership boundary | Composition roots wire artifacts together but do not own behavior. |
| Belongs there | Dependency injection registration and explicit endpoint mapping calls such as `Map<Module>Api`. |
| Does not belong there | Command/query logic, endpoint handler bodies, DTO definitions, UI state, or persistence implementation details. |

## Route and endpoint conventions

Module API routes use this prefix:

```txt
/api/modules/<module>
```

The `<module>` segment is lowercase kebab-case and identifies the module route ownership. Route constants belong in `<Module>.Contracts` when both client and API need the same route source.

Endpoint mapping expectations:

- Use an explicit method such as `Map<Module>Api` for endpoint registration.
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

## Contract DTO conventions

Contracts are the serialization source of truth for the module API. Contracts do not own business logic.

What belongs in `<Module>.Contracts`:

- Request DTOs
- Response DTOs
- Result DTOs
- Error DTOs or error codes
- Public API enums
- Route constants
- Transport-only value shapes
- References to `Entities.Contracts` where justified

What must not be in `<Module>.Contracts`:

- Domain entities
- Persistence models
- Application commands/handlers
- Business rules
- EF attributes or database behavior
- Server service abstractions
- UI state
- Infrastructure options
- Vendor SDK types unless explicitly approved as contract-safe

## Client boundary conventions

Client projects adapt frontend callers to HTTP. Blazor/Razor Interface projects are retired by ADR-0009; React Web is the only supported production UI surface.

The boundary is:

```txt
React Web -> generated/OpenAPI-safe clients or approved feature hooks -> HTTP -> <Module>.Api -> Application -> Domain
<Module>.Client -> <Module>.Contracts
<Module>.Api -> <Module>.Contracts
```

Do not bypass the client with raw UI route construction for migrated workflows. Do not move business validation into UI or client code; UI may perform presentation-friendly checks, but the authoritative rule remains in Domain/Application.

## Static boundary enforcement conventions

ADR-0009 retirement of Blazor/Razor Interface projects is enforced by `tools/architecture/validate_no_blazor.py`. Surviving protected module API boundary checks are enforced by `tools/architecture/validate_module_api_boundaries.py`.


### Forbidden Interface artifacts

Interface project reintroduction is forbidden unless a future accepted ADR supersedes ADR-0009.

### Local command

Run:

```bash
python3 tools/architecture/validate_no_blazor.py
python3 tools/architecture/validate_module_api_boundaries.py
```

### CI/build validation integration point

CI runs the same validator in `.github/workflows/architecture-validation.yml`. Local and CI interpretation should match: report-only violations remain visible, and fail-fast violations fail validation.

### Enforcement policy

- New modules should start fail-fast.
- Migrated modules must remain fail-fast.
- Non-migrated modules may remain report-only only until their migration stage.
- Exceptions require architecture approval and a removal condition.

## Intentionally deferred or rejected items

The following are not part of stages 1-4 and must not be introduced during ordinary module migrations without a new current requirement and, where architectural, ADR approval:

| Item | Status | Reason |
| --- | --- | --- |
| OpenAPI client generation | Deferred | Route/client conventions should be proven across multiple modules before adding generation tooling. |
| API versioning | Deferred | No concrete versioning requirement exists yet. |
| SignalR as primary CRUD/query boundary | Deferred | Realtime can be justified by a module-specific ADR, but HTTP remains the proven CRUD/query boundary. |
| Generic repositories | Rejected by default | They are allowed only if a current module proves they reduce complexity. |
| Mediator frameworks | Rejected by default | They are allowed only if existing code already uses them and migration requires consistency. |
| Event sourcing | Out of scope | ADR-0003 does not require or approve an event-sourced model. |
| Big-bang migration | Rejected | Module coupling varies significantly; migration remains module-by-module. |
| High-risk module migration | Deferred | High-risk modules need pilot conventions and module-specific plans first. |
| Large file, resumable upload, range request, and progress conventions | Deferred | These belong to `Files` or another module only when explicitly required. |

## Per-artifact rule audit

Artifact: `docs/architecture/conventions/adr-0003-implementation-conventions.md`
Type: Documentation
Responsibility: Record pilot-proven ADR-0003 implementation conventions for future migrations.

SRP:                    Pass — the document only records implementation conventions for ADR-0003.
Single state model:     Pass — ADR-0003 remains the decision source of truth; this document is the implementation convention source of truth.
Low coupling:           Pass — it links to the ADR and validator without embedding implementation code.
Abstraction:            Pass — it documents boundaries rather than depending on implementation artifacts.
Command-query:          N/A — documentation does not mutate or query runtime state.
Failure safety:         Pass — validation, client, API, and deferred exception handling expectations are explicit.
Open/Closed:            Pass — future new decisions require ADR updates instead of silently changing this guidance.
Core protection:        N/A — documentation contains no core logic.
KISS/YAGNI:             Pass — deferred/rejected items prevent speculative architecture.
Single source of truth: Pass — this file is scoped as the source of truth for ADR-0003 implementation conventions.

## Trade-offs

- The conventions are centralized in one platform module document instead of duplicated into global indexes, reducing navigation convenience but preserving a single source of truth.
- The document names proven layout and validation rules without requiring every legacy module to be migrated immediately.
- The document keeps large-file and realtime details deferred, so future specialized modules must do their own focused design before adopting those patterns.

## Rejected alternatives

- Updating ADR-0003 with pilot details was rejected because the ADR records the decision, while this file records implementation conventions without changing the decision.
- Creating global architecture convention pages was rejected because the platform module scope is the existing owner for ADR-0003 and its validation documentation.
- Documenting generated-client or versioning patterns now was rejected because the pilot did not prove those conventions.
