# Architecture

## What architecture style are we following?

We follow a modular, layered architecture with clean dependency boundaries. Each module or app slice keeps business behavior in the core, exposes use cases through application commands and queries, adapts transport through API/client contracts, and composes infrastructure only at the outer edges.

## What are the layer/module responsibilities?

- **Domain** owns business state, invariants, pure policies, value objects, entities, domain enums, and state transitions.
- **Application** owns use-case orchestration, command/query boundaries, application-owned ports, validation coordination, authorization or tenant checks where assigned, and explicit failure handling.
- **Contracts** own serialization-safe public API shapes: requests, responses, results, errors, public API enums, route constants, and transport-only values.
- **API** owns endpoint registration and maps HTTP contracts to application commands or queries. API endpoints stay thin.
- **Client** owns frontend-safe HTTP access to module APIs, route usage, cancellation propagation, serialization, and explicit failure mapping.
- **Interface/UI** owns presentation components, UI state, UI events, loading/success/empty/error presentation, accessibility, and calls to client use cases.
- **Infrastructure** owns concrete adapters for persistence, external services, background jobs, caches, files, network, SDKs, and other technology dependencies.
- **Module/composition root** owns dependency injection and host startup wiring. It does not own behavior.
- **Documentation** records the accepted rules, decisions, journeys, tests, runbooks, and implementation conventions in their owning scope.

## Which dependencies are allowed or forbidden?

- **Domain may depend on** the runtime and domain-owned value types. **Domain must not depend on** Application, Contracts, Client, API, Interface/UI, Infrastructure, EF, HTTP, filesystems, clocks, networks, vendors, or host frameworks.
- **Application may depend on** Domain and application-owned abstractions. **Application must not depend on** Interface/UI, Client, API, Infrastructure implementations, HTTP, EF/database details, filesystems, clocks, network, vendor SDKs, or host framework details except through ports.
- **Contracts may depend on** runtime libraries and approved serialization-safe shared contract libraries. **Contracts must not depend on** Domain, Application, Infrastructure, API, Interface/UI, server projects, persistence packages, UI state, or business behavior.
- **API may depend on** Contracts, Application, and approved host/API abstractions. **API must not depend on** Interface/UI, Client, persistence implementation details, or return domain entities directly.
- **Client may depend on** Contracts, HTTP client abstractions, runtime libraries, and approved frontend-safe libraries. **Client must not depend on** Application, Domain, Infrastructure, API, server projects, persistence libraries, UI components, or backend-only cross-module projects.
- **Interface/UI may depend on** Contracts, Client, approved frontend-safe UI/base libraries, and explicitly approved frontend-safe cross-module interfaces/contracts. **Interface/UI must not depend on** Application, Domain, Infrastructure, API, server-only libraries, persistence packages, backend services, or raw backend routes when a client exists.
- **Infrastructure may depend on** Application, Domain, persistence/framework packages, external SDKs, and concrete adapter configuration. **Infrastructure must not depend on** Interface/UI or Client and must not own business rules.
- **Composition roots may depend on** the projects they wire together. **Composition roots must not contain** command/query logic, business rules, endpoint bodies, DTO definitions, UI state, or persistence behavior.

## Where must business rules live?

Business rules, invariants, state transitions, and pure policies must live in Domain. Use-case sequencing, authorization checks assigned to the use case, validation coordination, idempotency, cancellation, concurrency, and failure handling must live in Application. A rule must have one source of truth; other layers may map, display, persist, or transport outcomes but must not reimplement the rule.

## Where must UI, persistence, infrastructure, and external service logic live?

- UI logic lives in Interface/UI or approved Web UI packages and stays presentation-only.
- Persistence logic lives in Infrastructure: repositories, DbContexts, migrations, mappings, and database-specific queries.
- Infrastructure logic lives in Infrastructure: background jobs, cache adapters, filesystem adapters, network adapters, SDK adapters, and concrete options.
- External service logic lives behind Application-owned ports and is implemented by Infrastructure adapters.
- Host startup and dependency wiring live in Module or app composition roots.
- API transport logic lives in API, and frontend HTTP transport logic lives in Client.

## What patterns should be copied?

- Domain -> Application -> API/Infrastructure -> composition dependency direction.
- Contracts and Clients as frontend-safe boundaries for module APIs.
- Thin API endpoints that map contract DTOs to commands or queries and return structured failures.
- Use-case-oriented client methods that accept `CancellationToken` and surface validation, authorization, not-found, conflict, timeout, cancellation, transport, deserialization, and unexpected failures intentionally.
- UI components that call client/use-case boundaries and handle loading, success, empty, and error states without duplicating business rules.
- Route constants in Contracts when both API and Client need the same route source.
- App documentation traceability from decision tree to journey to test document.
- Shared React Web presentation reuse through approved Web host packages without backend or feature workflow dependencies.

## What patterns are forbidden?

- Business rules in controllers, API endpoints, repositories, DTOs, migrations, UI components, generated clients, localization resources, or skill files.
- Domain or Application imports of infrastructure, UI, transport, persistence, framework, filesystem, network, clock, or vendor SDK details.
- Interface/UI references to backend Application, Domain, Infrastructure, API, server-only libraries, persistence packages, or raw backend routes for migrated workflows.
- Contracts that contain domain behavior, persistence models, repositories, handlers, UI state, infrastructure options, or unapproved vendor SDK types.
- Methods that both mutate state and serve as query APIs.
- Duplicate sources of truth for rules, state, validation, routes, contracts, generated clients, or user-facing copy.
- Manual edits to generated clients, generated DTOs, generated hooks, generated query keys, or generated mocks.
- Vague artifacts named `Manager`, `Helper`, `Util`, `Common`, or generic `Service` unless the single responsibility is precise and explicit.
- Speculative CQRS, event sourcing, mediator frameworks, generic repositories, custom design-system wrappers, inheritance, or new package managers when they do not reduce current complexity.
- Silent failure swallowing or accidental runtime failure instead of explicit validation and failure handling.

## When is it acceptable to keep things simple instead of abstracting?

Keep things simple whenever the current requirement is satisfied by an existing local pattern, the runtime, the native platform, an installed dependency, one line, or direct use of the framework without violating boundaries. Do not add projects, layers, wrappers, extension points, generic abstractions, shared packages, or design-system components for symmetry or imagined future needs. Abstract only when a present dependency boundary, test seam, external technology adapter, reuse requirement, or rule ownership problem requires it now.
