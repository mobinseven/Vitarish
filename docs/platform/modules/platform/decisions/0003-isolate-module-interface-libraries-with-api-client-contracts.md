# ADR-0003: Isolate module Interface libraries with API, Client, and Contracts projects

Date: 2026-06-24
Status: Proposed
App: Platform
Module: Platform
Capability: Module frontend/backend isolation
Owner: Architecture
Related apps: All frontend hosts
Related modules: All migrated modules; Entities
Supersedes:
Superseded by:
Related: docs/platform/modules/platform/decisions/0001-record-architecture-decisions.md; docs/platform/modules/platform/decisions/0002-persist-instants-as-utc-datetime.md

## Context

Current `src/Modules/<Module>/Interface` projects reference backend `Application`, `Domain`, `Infrastructure`, cross-module backend projects, and `src/Libraries/Entities/Entities.Server`.

Those references prevent frontend/backend isolation because UI-facing Interface libraries can pull server-only code into frontend hosts. They also cause Razor components and other UI artifacts to depend, directly or transitively, on server-only libraries such as persistence, backend orchestration, domain behavior, and infrastructure adapters.

The platform needs module Interface libraries that can be referenced by frontend hosts without importing backend projects. Module public APIs, HTTP clients, and transport contracts need explicit ownership so UI code depends on frontend-safe libraries only.

## Decision

Every migrated module gets these projects:

- `src/Modules/<Module>/Contracts/<Module>.Contracts.csproj`
- `src/Modules/<Module>/Client/<Module>.Client.csproj`
- `src/Modules/<Module>/Api/<Module>.Api.csproj`

`<Module>.Interface` may depend on:

- `<Module>.Client`
- `<Module>.Contracts`
- approved frontend-safe UI/base libraries
- explicitly approved frontend-safe cross-module interfaces/contracts

`<Module>.Interface` must not depend on:

- `<Module>.Application`
- `<Module>.Domain`
- `<Module>.Infrastructure`
- `<Module>.Api`
- cross-module backend libraries
- `src/Libraries/Entities/Entities.Server`

### Dependency direction

Target frontend dependency direction:

```txt
<Module>.Interface -> <Module>.Client -> <Module>.Contracts
```

Target backend/API dependency direction:

```txt
<Module>.Api -> <Module>.Application -> <Module>.Domain
<Module>.Api -> <Module>.Contracts
<Module>.Infrastructure -> <Module>.Application/Domain
<Module>.Module -> <Module>.Api
<Module>.Module -> <Module>.Infrastructure
<Module>.Module -> <Module>.Application
<Module>.Module -> <Module>.Domain
```

Frontend hosts must be able to reference Interface, Client, and Contracts projects without pulling backend projects.

### Contract library rules

`<Module>.Contracts` owns transport-safe artifacts only when they are part of the public client/server contract:

- request DTOs
- response DTOs
- result DTOs
- public API enums
- route constants

`<Module>.Contracts` must not contain:

- EF entities
- domain entities with behavior
- repositories
- application services
- domain services
- backend command/query handlers
- infrastructure adapters
- UI components
- business-rule implementations

Business validation and state transitions remain in Domain/Application. Contracts are serialization shapes, not the source of business behavior.

### Client library rules

`<Module>.Client` owns frontend-safe HTTP client access to the module API.

Client APIs must be shaped around UI/API use cases, not copied backend service names. Every async client method must accept a `CancellationToken`.

Client code must handle these outcomes explicitly:

- HTTP errors
- validation failures
- authorization failures
- conflicts
- timeouts
- cancellation
- deserialization failures

Razor components must not construct API routes or perform raw HTTP calls for module use cases when a module client exists.

### API library rules

`<Module>.Api` owns Minimal API endpoint definitions.

Endpoints must be thin adapters from HTTP contracts to Application commands/queries. Endpoints must not contain business rules. Endpoints must not return domain entities directly. Endpoints must not reference Interface or Client projects.

`<Module>.Module` or host initialization registers endpoint mappings through explicit methods such as `Map<Module>Api`.

### API conventions

Module API routes use this prefix convention:

```txt
/api/modules/<module>
```

The `<module>` route segment is lowercase kebab-case and matches the module identity used by host composition.

API versioning is intentionally deferred. Versioning must be added through a follow-up ADR or explicit architecture approval when the platform has a current compatibility requirement that cannot be met by additive contract evolution.

Standard response/error strategy:

- Validation failures return a structured validation error response with field or rule-level details where available.
- Not found returns a structured not-found error response and does not expose persistence details.
- Unauthorized returns the host-standard unauthorized response.
- Forbidden returns the host-standard forbidden response.
- Conflict/concurrency failures return a structured conflict response that identifies the conflicting operation or resource where safe.
- Unexpected failures return the platform-standard unexpected failure response and must not leak internals.

Tenant context propagation rules:

- Tenant context is resolved by host/API middleware from authenticated user, route, header, or other approved host context.
- Interface and Client code must not invent tenant authority or accept arbitrary tenant input as trusted.
- API endpoints pass resolved tenant context to Application commands/queries through approved application abstractions.
- Contracts may include tenant identifiers only when tenant identity is part of the public API contract for the use case.

Cancellation-token requirements:

- Every async Client method accepts `CancellationToken`.
- Every Minimal API endpoint that performs asynchronous work accepts or obtains `CancellationToken` from the HTTP request.
- Application calls made by API endpoints propagate the token.
- Cancellation is handled as an explicit non-success outcome and is not converted into a misleading domain failure.

File/binary transfer conventions for modules such as Files, Intelligence, Knowledge, and Sea:

- Metadata and authorization decisions use contract DTOs.
- Binary upload/download endpoints use HTTP streaming primitives owned by the API layer.
- Contracts describe metadata, requested operations, content type, size, checksums, and result/error shapes; Contracts do not contain stream implementations.
- Clients expose use-case-specific upload/download methods that accept cancellation tokens and surface transfer failures explicitly.
- Large file, resumable upload, range request, and realtime progress conventions require module-specific design before migration.

### Entities library boundary

Introduce `src/Libraries/Entities/Entities.Contracts/Entities.Contracts.csproj` as the source of truth for shared entity transport primitives.

Move transport-safe entity DTO/result/error primitives there where still needed, including:

- `EntityReadDto`
- `EntityWriteDto`
- `EntityDescriptorDto`
- `EntityResult`
- `EntityResult<T>`
- `EntityError`
- `EntityErrorCode`
- `EntityOperation`
- `PartedResult`

Keep these in `Entities.Server`:

- EF Core artifacts
- repositories
- caching
- DbContext factories
- command/query handlers
- backend entity services
- server notification infrastructure

Refactor `Entities.Client` to use HTTP as the primary CRUD/query transport and reference `Entities.Contracts`.

Existing SignalR support is optional realtime notification infrastructure, not the primary CRUD/query boundary, unless a future ADR explicitly justifies a different boundary.

### Cross-module frontend composition rules

`Interface -> Interface` references are allowed only for intentionally reusable frontend-safe UI components.

`Interface -> other module Application/Domain/Infrastructure` references are forbidden.

`Client -> other module Client` references should be avoided unless a clear use case is documented. Prefer host composition or backend orchestration for multi-module workflows.

Shared cross-module DTOs belong to the owning module's Contracts project, or to a true platform contract library only when the DTO is not module-specific.

### Migration strategy

Migration is module-by-module. Start with a low-coupling pilot module before high-coupling modules.

Rank modules by:

- number of backend project references in `<Module>.Interface.csproj`
- number of `Application`, `Domain`, and `Infrastructure` namespace usages under `src/Modules/<Module>/Interface`
- cross-module backend dependencies
- file/streaming/realtime complexity
- `Entities` dependency complexity

Suggested low-risk candidates to investigate first:

- `Feed`
- `Requests`
- `Platform`

Suggested high-risk modules to defer until conventions are proven:

- `Sea`
- `Medical`
- `Appointments`
- `Knowledge`
- `Communications`
- `Intelligence`

### Done criteria for each migrated module

A module migration is done only when:

- `<Module>.Interface` references only `<Module>.Client`, `<Module>.Contracts`, approved frontend-safe UI/base libraries, and explicitly approved frontend-safe cross-module interfaces/contracts.
- No `Vitarish.*.Application`, `Vitarish.*.Domain`, `Vitarish.*.Infrastructure`, or `Entities.Server` usages remain under `src/Modules/<Module>/Interface`.
- `<Module>.Client` uses HTTP and contract DTOs.
- `<Module>.Api` maps endpoints to Application commands/queries.
- Contracts are the serialization source of truth.
- Client and API failure handling is explicit.
- Tests or static checks validate the boundary.

## Consequences

Positive consequences:

- Real frontend/backend isolation.
- Explicit public module APIs.
- Reduced accidental backend coupling in UI.
- HTTP-compatible deployment boundary.
- Clearer ownership of contracts and clients.

Negative consequences:

- Migration cost.
- DTO mapping cost.
- Temporary duplication risk during migration.
- Need for route/error convention enforcement.
- More projects per module.

## Alternatives Considered

### Keep current in-process backend references

Rejected. This preserves the current coupling problem and prevents frontend hosts from referencing Interface libraries without pulling backend projects.

### Move domain models wholesale into Contracts

Rejected. Domain models can contain behavior, invariants, and state transitions. Moving them into Contracts would make transport shapes the business-rule source of truth and would leak domain behavior into frontend-safe libraries.

### Use SignalR as the primary API boundary

Rejected for the general module boundary. SignalR remains useful for realtime notifications, but CRUD/query interactions need an HTTP-compatible request/response boundary unless a module-specific ADR justifies otherwise.

### Generate clients from OpenAPI immediately

Deferred. Generated clients may be useful later, but adopting generation now would add tooling and convention complexity before the platform has proven route, error, and contract patterns through pilot migrations.

### Big-bang migrate every module at once

Rejected. Coupling, Entities usage, file transfer, streaming, and realtime needs vary by module. A module-by-module migration lowers risk and lets conventions be validated before high-risk modules move.

## Validation / Enforcement

Implement a static architecture check that scans:

- `src/Modules/*/Interface/*.csproj`
- `src/Modules/*/Interface/**/*.cs`
- `src/Modules/*/Interface/**/*.razor`

The check must fail on references/imports to:

- same-module `Application`, `Domain`, or `Infrastructure`
- cross-module `Application`, `Domain`, or `Infrastructure`
- `<Module>.Api`
- `Entities.Server`

The check must allow only approved frontend-safe dependencies. Include this check in CI/build validation once implemented.

## Notes

This ADR proposes the target isolation boundary. It does not approve a big-bang migration, does not accept the ADR, and does not by itself create new production projects.
