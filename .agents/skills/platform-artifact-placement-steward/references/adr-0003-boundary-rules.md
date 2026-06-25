# ADR-0003 Boundary Rules

For migrated modules, ADR-0003 introduces explicit frontend/backend boundary projects.

## Project Layout

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

Do not create projects only for symmetry. Use these projects only where the module has the corresponding current concern.

## Target Boundary

```txt
Interface/Razor UI -> <Module>.Client -> HTTP -> <Module>.Api -> Application -> Domain
Interface/Razor UI -> <Module>.Contracts
<Module>.Client -> <Module>.Contracts
<Module>.Api -> <Module>.Contracts
<Module>.Infrastructure -> <Module>.Application/Domain
<Module>.Module -> <Module>.Api/Application/Domain/Infrastructure
```

Frontend hosts must be able to reference `Interface`, `Client`, and `Contracts` projects without pulling backend projects.

## Contracts Project

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

## Client Project

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

## Api Project

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

## Interface Project

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

## Application Project

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

## Domain Project

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

## Infrastructure Project

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

## Module and Composition Concerns

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
