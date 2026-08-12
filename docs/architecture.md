# Architecture

Consult [`AGENTS.md`](../AGENTS.md) for non-negotiable repository rules. Consult this file when a change requires deciding which layer owns new code, which dependency direction is allowed, or which architectural pattern to copy or avoid. Consult the codebase map in [`docs/README.md`](README.md) when deciding where files physically live.

## 1. Architecture style

Vitarish follows a modular, layered architecture with clean dependency boundaries. Modules keep business behavior in their core, expose use cases through application commands and queries, adapt transport through API/client contracts, and compose infrastructure only at the outer edges.

The expected dependency direction is:

```txt
Domain -> Application -> API / Infrastructure -> composition roots
Contracts -> API and Client transport boundary
Client -> UI / host presentation boundary
```

Dependencies point inward for business behavior and outward only for adapters, transport, generated clients, UI, and host wiring.

## 2. Architecture goals

This architecture exists to:

- keep business rules independent from UI, persistence, and vendors
- allow modules to evolve without leaking internals
- make API/client contracts explicit and regeneratable
- make tests target the correct layer
- prevent duplicated business rules across UI, API, and database
- keep implementation simple unless a real boundary requires abstraction

## 3. Layer responsibilities and dependency rules

| Layer | Owns | May depend on | Must not depend on |
| --- | --- | --- | --- |
| Domain | Business state, invariants, pure policies, value objects, entities, domain enums, and state transitions. | Runtime and domain-owned value types. | Application, Contracts, Client, API, Interface/UI, Infrastructure, EF, HTTP, filesystems, clocks, networks, vendors, or host frameworks. |
| Application | Use-case orchestration, command/query boundaries, application-owned ports, validation coordination, assigned authorization or tenant checks, idempotency, cancellation, concurrency, and explicit failure handling. | Domain and application-owned abstractions. | Interface/UI, Client, API, Infrastructure implementations, HTTP, EF/database details, filesystems, clocks, network, vendor SDKs, or host framework details except through ports. |
| Contracts | Serialization-safe public API shapes: requests, responses, results, errors, public API enums, route constants, and transport-only values. | Runtime libraries and approved serialization-safe shared contract libraries. | Domain, Application, Infrastructure, API, Interface/UI, server projects, persistence packages, UI state, or business behavior. |
| API | Endpoint registration and mapping between HTTP contracts and application commands or queries. | Contracts, Application, and approved host/API abstractions. | Interface/UI, Client, persistence implementation details, business rules, or direct domain entity responses. |
| Client | Frontend-safe HTTP access to module APIs, route usage, cancellation propagation, serialization, generated-client usage, and explicit transport failure mapping. | Contracts, HTTP client abstractions, runtime libraries, generated client code, and approved frontend-safe libraries. | Application, Domain, Infrastructure, API, server projects, persistence libraries, UI components, or backend-only cross-module projects. |
| Interface/UI | Presentation components, UI state, UI events, loading/success/empty/error presentation, accessibility, localization usage, and calls to client use cases. | Contracts, Client, approved frontend-safe UI/base libraries, and explicitly approved frontend-safe cross-module interfaces/contracts. | Application, Domain, Infrastructure, API, server-only libraries, persistence packages, backend services, raw backend routes when a Client exists, or duplicated business rules. |
| Infrastructure | Concrete adapters for persistence, external services, background jobs, caches, files, network, SDKs, database mappings, database-specific queries, migrations, and concrete options. | Application, Domain, persistence/framework packages, external SDKs, and concrete adapter configuration. | Interface/UI, Client, or business rules. |
| Composition roots | Dependency injection and host startup wiring. | The projects they wire together. | Command/query logic, business rules, endpoint bodies, DTO definitions, UI state, or persistence behavior. |
| Documentation | Accepted rules, decisions, journeys, tests, runbooks, and implementation conventions in their owning scope. | Repository files it references. | Runtime behavior, duplicated business rules, or generated source edits. |

## 4. Single source of truth

A business rule, state transition, validation rule, route, contract, generated artifact, or user-facing copy must have one source of truth. Other layers may map, display, persist, or transport outcomes, but they must not reimplement the rule.

## 5. Transport, client, and contract boundaries

Contracts are transport shapes, not domain models. API endpoints translate HTTP input into application commands or queries and translate application outcomes back into contract responses.

Client code provides frontend-safe access to APIs. See Section 8 for the rule against editing generated artifacts by hand.

## 6. Correct implementation flow

Use this flow for non-trivial changes:

1. Classify each artifact as Domain, Application, Command, Query, Port/contract, Adapter/infrastructure, Presentation/API/UI, Data transfer shape, Persistence model, Test, Configuration/composition, Documentation, or Skill.
2. Identify the smallest required artifact set and assign one responsibility to each artifact.
3. Name the single source of truth for every state model, business rule, validation rule, route, contract, generated artifact, and user-facing copy.
4. Define boundaries before writing infrastructure or UI code.
5. Keep commands and queries separate.
6. Reuse existing local patterns, runtime features, native platform behavior, and installed dependencies before writing custom code.
7. Add only the abstractions required by a present boundary, adapter, test seam, or reuse need.
8. Validate with the narrowest deterministic checks that cover the changed layer.

## 7. Patterns to copy

- Domain -> Application -> API/Infrastructure -> composition dependency direction.
- Contracts and Clients as frontend-safe boundaries for module APIs.
- Thin API endpoints that map contract DTOs to commands or queries and return structured failures.
- Use-case-oriented client methods that accept cancellation and surface validation, authorization, not-found, conflict, timeout, cancellation, transport, deserialization, and unexpected failures intentionally.
- UI components that call client/use-case boundaries and handle loading, success, empty, and error states without duplicating business rules.
- Route constants in Contracts when both API and Client need the same route source.
- App documentation traceability from decision tree to journey to test document.
- Shared React Web presentation reuse through approved Web host packages without backend or feature workflow dependencies.

## 8. Forbidden architectural patterns

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

## 9. Simplicity and abstraction rules

Keep implementation simple whenever the current requirement is satisfied by an existing local pattern, the runtime, the native platform, an installed dependency, one line, or direct use of the framework without violating boundaries.

Do not add projects, layers, wrappers, extension points, generic abstractions, shared packages, or design-system components for symmetry or imagined future needs. Abstract only when a present dependency boundary, test seam, external technology adapter, reuse requirement, or rule ownership problem requires it now.

## 10. Exceptions and legacy code

Legacy code does not make a forbidden pattern acceptable for new work. When touching legacy code, keep the change as small as possible, avoid spreading the legacy dependency direction, and add an explicit boundary or adapter only when required by the current change.

If a rule cannot be followed without expanding scope, document the exception near the owning decision or task, state the risk, and prefer a follow-up over mixing unrelated cleanup into the current change.

## 11. Testing implications

Tests target the layer that owns the behavior in Section 3's "Owns" column — for example, Domain tests cover invariants and state transitions, Application tests cover orchestration and failure handling, and UI tests cover rendering and interaction state. Architecture validators cover dependency direction and generated-boundary rules when available.

Do not duplicate business-rule assertions in UI, API, persistence, or generated-client tests when Domain or Application owns the rule.

## 12. Links to ADRs, codebase map, AGENTS.md, and module docs

- Repository rules: [`AGENTS.md`](../AGENTS.md)
- Documentation index and codebase map: [`docs/README.md`](README.md)
- Architecture decisions index: [`docs/architecture/decisions/_index.md`](architecture/decisions/_index.md)
- ADR template: [`docs/architecture/decisions/_template.md`](architecture/decisions/_template.md)
- Interface/API/client boundary ADR: [`docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md`](architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md)
- Module-owned Agents and guarded composition: [`docs/architecture/decisions/0012-module-owned-agents-with-guarded-composition.md`](architecture/decisions/0012-module-owned-agents-with-guarded-composition.md)
- ADR-0003 implementation conventions: [`docs/architecture/conventions/adr-0003-implementation-conventions.md`](architecture/conventions/adr-0003-implementation-conventions.md)
- ADR-0003 validation: [`docs/architecture/validation/adr-0003-interface-boundary-validation.md`](architecture/validation/adr-0003-interface-boundary-validation.md)
- OpenAPI endpoint conventions: [`docs/architecture/conventions/openapi-endpoint-conventions.md`](architecture/conventions/openapi-endpoint-conventions.md)
- Platform module docs: [`docs/platform/modules/`](platform/modules/)
- App docs: [`docs/apps/`](apps/)
- Host docs: [`docs/hosts/`](hosts/)
- Development docs: [`docs/development/`](development/)
- Runbooks: [`docs/runbooks/`](runbooks/)
