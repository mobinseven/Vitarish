# App operating contract

Applies to .NET app aggregators and app-owned backend slices under this
directory. This directory is not the React Web workspace. The repository and
nearer scoped `AGENTS.md` instructions own backend boundary guidance; Web-host
routing rules do not apply to app-only work.

The root Cohesion, Narrow knowledge, Cohesive edits, and Current requirements
rules are **[MUST]** rules in this subtree, specialized below. Apply them to
behavior introduced or materially worsened by the current change; report
unrelated legacy violations without expanding the change into cleanup.

## App architecture

- **[MUST] Layer ownership** — Changed production artifacts must conform to the
  ownership and forbidden-dependency matrix in
  [`docs/architecture.md`](../../docs/architecture.md), including its forbidden
  architectural patterns. App-owned invariants and state transitions belong to
  Domain; app use-case orchestration, ports, cross-module workflows, and
  application-owned policy belong to Application; serialization-safe wire
  shapes belong to Contracts; transport mapping belongs to Api; technology
  adapters belong to Infrastructure; and service registration and startup
  wiring belong to composition roots.
- **[MUST] App/module ownership** — Modules own reusable domain capabilities,
  invariants, and module-owned mutation behavior. Apps own product-specific
  workflows that coordinate modules, including app-specific sequencing and
  policy. App code may consume module-owned application capabilities and
  contracts through their stable boundaries, but must not reference module
  Infrastructure, persistence, provider SDK, or transport implementation types.
  Modules must not depend on app projects.
- **[MUST] Cohesion** — Give each artifact one primary reason to change. Keep
  app workflow orchestration, transport mapping, persistence, infrastructure,
  and composition in their owning layers; do not split cohesive behavior merely
  to add indirection.
- **[MUST] Narrow knowledge** — Cross-layer and cross-module code must depend on
  the narrowest stable capability owned by the providing boundary. Do not reach
  through internals or leak persistence, provider, framework, transport, or
  generated implementation types across their owning boundary.
- **[MUST] Cohesive extension** — Reuse a natural extension point or edit the
  current owner. Do not add a handler, strategy, plug-in, wrapper, or parallel
  path solely to avoid changing stable app or module code.
- **[MUST] Present need** — A new port, project, layer, option, abstraction, or
  extension point requires a current boundary, nondeterminism source, alternate
  implementation, test seam, or demonstrated reuse need. Do not add one for
  symmetry or a hypothetical future app.
- **[MUST] Composition only** — `App`, `Aggregator`, and `Base` projects
  compose services, app endpoints, module registrations, and infrastructure.
  They must not own commands, queries, business rules, DTO definitions,
  endpoint bodies, or persistence behavior. `Aggregator` must compose explicit
  app instances; it must not discover or activate apps through broad runtime
  scanning.

## Cross-module workflows and boundaries

- **[MUST]** Keep app-specific orchestration in the app Application layer.
  Module rules remain in the module that owns them; do not copy a module rule
  into an app handler, endpoint, persistence adapter, agent, or client.
- **[MUST]** Validate HTTP and other transport shapes at Api, validate use-case
  preconditions and app policy at Application, enforce invariants at their
  owner, and validate external or generated responses before trusting them.
  Define cancellation, timeout, retry, duplicate-call, idempotency,
  concurrency, partial-failure, and dependency-unavailable behavior at the
  initiating app boundary. Never swallow failures.
- **[MUST]** App Clients are frontend-safe transport boundaries. Keep generated
  clients generated-only, propagate cancellation, and map transport failures
  explicitly. Do not make Client or Interface/UI code depend on app
  Application, Domain, Infrastructure, Api, persistence, or server-only types.
- **[MUST]** Commands must not become query APIs, and queries must not cause
  observable state changes. App endpoints remain thin: bind and validate
  transport input, invoke the owning use case, and map its outcome to the
  contract response.
- **[MUST] Named app values** — Keep app-owned routes, operation IDs,
  authorization policy and claim names, event/message types, serialization
  discriminators, configuration keys, and agent action or compatibility keys
  at their authoritative owner. Reuse those values from Contracts or the
  existing owning boundary across Api, Application, Client, and composition
  code; do not create a second source by repeating literals in production code.

## Native AOT readiness

- **[MUST]** Follow
  [ADR-0010](../../docs/architecture/decisions/0010-prefer-statically-analyzable-native-aot-compatible-design.md)
  and its
  [engineering standard](../../docs/architecture/conventions/adr-0010-static-analyzability-native-aot.md).
  Keep app registration, endpoint mapping, validation, configuration, and JSON
  metadata explicit or generated. Keep EF Core, provider types, proxies,
  tracking state, and `IQueryable<T>` inside persistence infrastructure; never
  expose them through Domain, Application, Contracts, or Client boundaries.

## App-owned Agents

- **[MUST]** An optional `<App>.Agents` project is provider-neutral. It may
  reference its own Application, explicitly selected module `Agents` projects,
  `Intelligence.Agents`, and approved serialization-safe shared libraries. It
  may own declarative profiles, capability-pack composition, the final agent
  identity, and the final result shape.
- **[MUST]** App Agents must not own authorization, tenant or subject binding,
  approval tiers, mutation policy, command execution, provider selection,
  credentials, retries, idempotency, concurrency, persistence, infrastructure,
  provider SDKs, HTTP, reflection discovery, or service-provider construction.
  Those concerns remain in the authenticated Api, app Application, existing
  mutation workflows, and Infrastructure boundaries.
- **[MUST]** Capability exposure must be explicit and recomputed at the
  composition and execution boundaries. Do not use assembly scanning or
  dynamic plugin discovery to build an app agent surface.

## Scoped persistence concurrency

- **[MUST]** Never run concurrent operations against the same scoped EF Core
  `DbContext`, including through repositories or services backed by that
  context. Do not use `Task.WhenAll` or start multiple unawaited queries over
  shared scoped persistence dependencies. Await those operations sequentially.
  Parallel persistence work requires explicitly independent context instances
  with separately defined transaction, consistency, cancellation, and failure
  behavior.

## Validation

- **[MUST]** For changed app Agents projects or their registration, run
  `dotnet run --project tools/architecture -- agents`.
- **[MUST]** For app endpoint, contract, application, or infrastructure changes,
  run the focused `tests/Apps/<App>.Tests` project when present and the
  applicable merge-level build from `docs/codebase-map.md`. If a focused test
  project or validator does not exist, report that fact and the checks that
  were run instead.
- **[MUST]** For project or layer-boundary changes, inspect the changed project
  references against `docs/architecture.md`; no current static validator proves
  the complete app dependency matrix. Never edit generated clients, DTOs,
  hooks, mocks, migration designer files, or snapshots manually; change their
  authoritative source and run the canonical generator.
