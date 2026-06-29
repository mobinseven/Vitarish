---
name: platform-frontend-backend-isolation
description: Enforce Vitarish frontend/backend isolation by ensuring Interface and React Web host UI do not depend on backend implementation, clients speak HTTP through Contracts or generated contract-safe clients, Api adapts Contracts to Application, Contracts contain serialization shapes only, and Domain/Application retain behavior.
---

# Platform Frontend/Backend Isolation

## Purpose

Use this skill to design, implement, or review boundaries between frontend Interface artifacts, React Web host artifacts, generated HTTP clients, API endpoints, Contracts, Application use cases, and Domain behavior.

This skill protects the platform boundary:

* Interface and React Web host UI cannot depend on backend implementation artifacts.
* Client/generated client access speaks HTTP using Contracts or generated contract-safe DTOs.
* Api adapts Contracts to Application.
* Contracts contain serialization shapes only.
* Domain and Application keep behavior.
* React Web host module and submodule features stay modular and do not import sibling feature internals.

## Use When

Use this skill when creating, moving, or reviewing artifacts in or between these layers:

* `src/Modules/**/Interface/**`
* `src/Modules/**/Client/**`
* `src/Modules/**/Contracts/**`
* `src/Modules/**/Api/**`
* `src/Modules/**/Application/**`
* `src/Modules/**/Domain/**`
* `src/Hosts/**/src/**` React/Web host pages, features, hooks, components, routes, API wrappers, and generated clients
* host or composition-root code that wires frontend clients to backend APIs
* tests that verify frontend/backend boundaries

Apply it especially when adding a module endpoint, generated/typed API client, request/response DTO, frontend data access path, feature hook, React page/component, or contract mapping.

## Boundary Rules

### Interface

Interface artifacts own user interaction and presentation state only.

Allowed:

* Depend on Client abstractions or concrete HTTP clients intended for frontend use.
* Use Contract request and response shapes when they are the HTTP serialization boundary.
* Map Contract data to UI view state where needed.

Not allowed:

* Referencing Api, Infrastructure, persistence, server-only Application handlers, Domain entities, DbContexts, repositories, or backend composition details.
* Calling Application use cases directly from frontend UI.
* Embedding backend business behavior in UI components.

### React Web Host

React Web host artifacts own host composition, route-level UI, module feature UI, feature hooks, and presentation state only.

Allowed:

* Pages compose route params, route state, layouts, feature components, and feature hooks.
* Feature hooks wrap generated clients/hooks and expose use-case-oriented names.
* Module features live under `features/<module>` and submodule/capability features under `features/<module>/<capability>` unless an existing host convention is stronger.
* Feature wrappers map generated DTOs to UI-ready view models when that reduces UI coupling.
* Host-only reusable UI lives in host `components` or `ui` only when it is not module-owned.

Not allowed:

* Importing backend Api, Application, Domain, Infrastructure, persistence, server projects, or server-only libraries.
* Calling backend use cases directly instead of HTTP/generated clients.
* Embedding Domain/Application business behavior in React components, pages, or hooks.
* Letting pages import generated hooks directly when feature hooks exist.
* Mixing unrelated module features, such as Medical and Knowledge, in one feature folder.
* Placing submodule/capability internals, such as Medical/Prescription, in a parent module’s generic folder when the submodule has its own workflow, state, route group, API wrapper, query keys, or independent evolution.
* Importing sibling module/submodule internals instead of extracting a named shared owner.

### Client

Client artifacts own HTTP communication.

Allowed:

* Speak HTTP to Api routes.
* Serialize requests and deserialize responses using Contracts.
* Handle transport failures, cancellation, status codes, and authentication forwarding appropriate to the client boundary.

Not allowed:

* Depend on Api implementation classes, Application handlers, Domain entities, or Infrastructure.
* Contain business rules beyond transport concerns and simple response interpretation.
* Share mutable state as a source of truth for backend state.

### Generated React Client

Generated React client artifacts own generated transport access only.

Allowed:

* Generated DTOs, clients, React Query hooks, query keys, and MSW mocks.
* Transport-safe generated types based on stable OpenAPI contracts.
* Regeneration from OpenAPI and generator configuration.

Not allowed:

* Manual edits to generated output.
* Placement inside pages or feature folders.
* Business rules, UI layout, localization, feature invalidation policy, or module/submodule workflow behavior.
* Direct backend implementation references.

### Contracts

Contracts are serialization shapes only.

Allowed:

* Request/response records, enums, and primitive value shapes needed on the wire.
* Serialization attributes when required for stable wire format.
* Basic structural defaults required by serializers.

Not allowed:

* Business behavior, validation rules, authorization rules, persistence logic, HTTP execution, UI formatting, localization, or framework-specific behavior outside serialization needs.
* Domain invariants as executable behavior.
* Application orchestration or command/query handling.

### Api

Api artifacts adapt HTTP Contracts to Application use cases.

Allowed:

* Route binding and HTTP status mapping.
* Contract-to-Application command/query mapping.
* Application result-to-Contract mapping.
* Transport-specific validation of malformed requests before calling Application.

Not allowed:

* Business rules that belong in Domain/Application.
* Persistence, vendor SDK, or UI/Web host concerns.
* Returning Domain entities directly as wire responses.

### Application and Domain

Application owns use-case orchestration. Domain owns behavior and invariants.

Allowed:

* Application commands change state and return command results only.
* Application queries return data and do not mutate observable state.
* Domain entities/value objects enforce invariants and behavior.

Not allowed:

* Depending on Interface, React Web host, Client, Api, Contracts, HTTP, serialization, UI, persistence implementations, or framework transport details.
* Moving behavior into Contracts, generated clients, React hooks, or UI components to make frontend or Api mapping easier.

## Required Review Checks

For each affected artifact, verify:

1. **Dependency direction:** Interface/React Web host → Client/generated client → Contracts/OpenAPI over HTTP; Api → Contracts/Application; Application → Domain/ports; Domain → no outer layer.
2. **No backend dependency from frontend:** Interface and React Web host UI do not reference Api, Application handlers, Domain entities, Infrastructure, repositories, DbContexts, server projects, or server-only libraries.
3. **Generated client isolation:** generated React clients stay under generated API output and are wrapped by feature hooks before page/form/table/dialog use when wrappers exist.
4. **Modular frontend ownership:** React module features stay under their owning module/capability folder, such as `features/medical`, `features/knowledge`, or `features/medical/prescription`.
5. **Contracts are inert:** Contract types carry data only and contain no business behavior.
6. **Api mapping is explicit:** Api maps Contracts to Application input and Application output to Contracts.
7. **Behavior location:** Domain/Application contain rules, invariants, authorization decisions, and use-case flow.
8. **CQRS split:** Commands and queries remain separate across Api, Client, Contracts, generated clients, feature hooks, and Application.
9. **Failure handling:** Client/generated client handles HTTP failures/cancellation; feature hooks map failures for UI; Api handles malformed input/status mapping; Application handles use-case failures explicitly.
10. **Single state source:** Backend state remains owned by Application/Domain/persistence boundaries, while UI state remains presentation-only.

## Common Violations

Stop and correct immediately if you find:

* Interface importing `*.Api`, `*.Application`, `*.Domain`, `*.Infrastructure`, repository, or DbContext namespaces.
* React Web host code importing backend Api, Application, Domain, Infrastructure, server projects, persistence libraries, or server-only modules.
* React Web host pages importing generated hooks directly when feature hooks exist.
* React Web host feature folders mixing unrelated modules, such as Medical and Knowledge.
* React Web host submodule/capability artifacts placed in a parent module generic folder despite distinct workflow/state/routes/API wrappers.
* Client constructing Application commands by referencing Application types instead of Contract request shapes.
* Contracts with methods that enforce business rules, mutate state, call services, or format UI text.
* Generated client output edited manually or used as the place for feature behavior.
* Api returning Domain entities or persistence models directly.
* Application depending on Contract DTOs, generated DTOs, React types, or serialization attributes.
* Domain depending on Contract, Api, Client, Interface, React Web host, HTTP, JSON, or framework transport types.
* One type used simultaneously as Domain model, Application DTO, Contract shape, generated client shape, and UI state.

## Preferred Slice Pattern

For a new HTTP-backed frontend capability, keep the slice explicit and small:

1. Contract request/response records for the wire format.
2. Client method or generated client hook that sends/receives those Contract records over HTTP.
3. Feature hook under the owning React module/submodule folder that wraps generated access and exposes a use-case name.
4. Api endpoint that maps Contract records to one Application command or query.
5. Application command/query and handler if one does not already exist.
6. Domain behavior only when the capability requires new business behavior.
7. Interface component/page or React page/component that uses the Client/feature hook, not backend internals.
8. Tests at the lowest layer that owns the behavior, plus boundary tests when dependency direction is easy to regress.

Do not introduce generated client frameworks, mediator frameworks, shared mega-DTOs, or new abstractions unless they reduce complexity for the current change.

## Output Format

When reporting a review or plan, include:

```md
## Frontend/Backend Isolation Review

### Artifacts
| Artifact | Layer | Responsibility | Boundary verdict |
| --- | --- | --- | --- |

### Dependency Direction
- Expected: Interface/React Web host → Client/generated client → Contracts/OpenAPI over HTTP; Api → Contracts/Application; Application → Domain/ports; Domain → none.
- Actual: [summary]

### Modular UI Ownership
- Module/capability mapping: [summary]
- Cross-module or sibling-feature imports: [none / findings]

### Findings
| Severity | File | Issue | Required fix |
| --- | --- | --- | --- |

### Checks
- Interface avoids backend dependencies: Pass/Fail/N/A
- React Web host avoids backend dependencies: Pass/Fail/N/A
- React Web host preserves module/submodule ownership: Pass/Fail/N/A
- Client/generated client speaks HTTP using Contracts/OpenAPI: Pass/Fail/N/A
- Generated code remains unedited and isolated: Pass/Fail/N/A
- Api adapts Contracts to Application: Pass/Fail/N/A
- Contracts contain serialization shapes only: Pass/Fail/N/A
- Domain/Application keep behavior: Pass/Fail
- Commands and queries stay separate: Pass/Fail/N/A
- Failures handled at owning boundary: Pass/Fail
```

If there are no findings, state the reviewed paths and commands used.

## Placement and Routing

Place this skill at:

```text
.agents/skills/platform-frontend-backend-isolation/SKILL.md
```

Route frontend/backend boundary, Contract, Client, Api, Interface dependency, React Web host/backend dependency, generated client isolation, and cross-layer isolation work to this skill from the root `AGENTS.md` skill routing table when that table is maintained.
