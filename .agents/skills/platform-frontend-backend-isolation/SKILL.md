---
name: platform-frontend-backend-isolation
description: Enforce Vitarish frontend/backend isolation by ensuring Interface clients do not depend on backend implementation, clients speak HTTP through Contracts, Api adapts Contracts to Application, Contracts contain serialization shapes only, and Domain/Application retain behavior.
---

# Platform Frontend/Backend Isolation

## Purpose

Use this skill to design, implement, or review boundaries between frontend Interface artifacts, HTTP clients, API endpoints, Contracts, Application use cases, and Domain behavior.

This skill protects the platform boundary:

* Interface cannot depend on backend implementation artifacts.
* Client speaks HTTP using Contracts.
* Api adapts Contracts to Application.
* Contracts contain serialization shapes only.
* Domain and Application keep behavior.

## Use When

Use this skill when creating, moving, or reviewing artifacts in or between these layers:

* `src/Modules/**/Interface/**`
* `src/Modules/**/Client/**`
* `src/Modules/**/Contracts/**`
* `src/Modules/**/Api/**`
* `src/Modules/**/Application/**`
* `src/Modules/**/Domain/**`
* host or composition-root code that wires frontend clients to backend APIs
* tests that verify frontend/backend boundaries

Apply it especially when adding a module endpoint, generated/typed API client, request/response DTO, frontend data access path, or contract mapping.

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
* Persistence, vendor SDK, or UI concerns.
* Returning Domain entities directly as wire responses.

### Application and Domain

Application owns use-case orchestration. Domain owns behavior and invariants.

Allowed:

* Application commands change state and return command results only.
* Application queries return data and do not mutate observable state.
* Domain entities/value objects enforce invariants and behavior.

Not allowed:

* Depending on Interface, Client, Api, Contracts, HTTP, serialization, UI, persistence implementations, or framework transport details.
* Moving behavior into Contracts to make frontend or Api mapping easier.

## Required Review Checks

For each affected artifact, verify:

1. **Dependency direction:** Interface → Client → Contracts over HTTP; Api → Contracts and Application; Application → Domain/ports; Domain → no outer layer.
2. **No backend dependency from Interface:** UI does not reference Api, Application handlers, Domain entities, Infrastructure, repositories, or DbContexts.
3. **Contracts are inert:** Contract types carry data only and contain no business behavior.
4. **Api mapping is explicit:** Api maps Contracts to Application input and Application output to Contracts.
5. **Behavior location:** Domain/Application contain rules, invariants, authorization decisions, and use-case flow.
6. **CQRS split:** Commands and queries remain separate across Api, Client, Contracts, and Application.
7. **Failure handling:** Client handles HTTP failures/cancellation; Api handles malformed input/status mapping; Application handles use-case failures explicitly.
8. **Single state source:** Backend state remains owned by Application/Domain/persistence boundaries, while UI state remains presentation-only.

## Common Violations

Stop and correct immediately if you find:

* Interface importing `*.Api`, `*.Application`, `*.Domain`, `*.Infrastructure`, repository, or DbContext namespaces.
* Client constructing Application commands by referencing Application types instead of Contract request shapes.
* Contracts with methods that enforce business rules, mutate state, call services, or format UI text.
* Api returning Domain entities or persistence models directly.
* Application depending on Contract DTOs or serialization attributes.
* Domain depending on Contract, Api, Client, Interface, HTTP, JSON, or framework transport types.
* One type used simultaneously as Domain model, Application DTO, Contract shape, and UI state.

## Preferred Slice Pattern

For a new HTTP-backed frontend capability, keep the slice explicit and small:

1. Contract request/response records for the wire format.
2. Client method that sends/receives those Contract records over HTTP.
3. Api endpoint that maps Contract records to one Application command or query.
4. Application command/query and handler if one does not already exist.
5. Domain behavior only when the capability requires new business behavior.
6. Interface component/page that uses the Client, not backend internals.
7. Tests at the lowest layer that owns the behavior, plus boundary tests when dependency direction is easy to regress.

Do not introduce generated client frameworks, mediator frameworks, shared mega-DTOs, or new abstractions unless they reduce complexity for the current change.

## Output Format

When reporting a review or plan, include:

```md
## Frontend/Backend Isolation Review

### Artifacts
| Artifact | Layer | Responsibility | Boundary verdict |
| --- | --- | --- | --- |

### Dependency Direction
- Expected: Interface → Client → Contracts over HTTP; Api → Contracts/Application; Application → Domain/ports; Domain → none.
- Actual: [summary]

### Findings
| Severity | File | Issue | Required fix |
| --- | --- | --- | --- |

### Checks
- Interface avoids backend dependencies: Pass/Fail
- Client speaks HTTP using Contracts: Pass/Fail/N/A
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

Route frontend/backend boundary, Contract, Client, Api, Interface dependency, and cross-layer isolation work to this skill from the root `AGENTS.md` skill routing table when that table is maintained.
