# Module operating contract

Applies to business modules under this directory. The repository and scoped
`AGENTS.md` instructions own backend boundary guidance. React and Web-host
routing rules do not apply to module-only work.

The root Cohesion, Narrow knowledge, Cohesive edits, and Current requirements
rules are **[MUST]** rules in this subtree, specialized below. Apply them to
behavior introduced or materially worsened by the current change; report
unrelated legacy violations without expanding the change into cleanup.

## Module architecture

- **[MUST] Layer ownership** — Changed production artifacts must conform to the
  ownership and forbidden-dependency matrix in
  [`docs/architecture.md`](../../docs/architecture.md), including its forbidden
  architectural patterns. Business rules and state transitions belong to
  Domain; use-case orchestration and ports to Application; serialization-safe
  wire shapes to Contracts; transport mapping to Api; technology adapters to
  Infrastructure; and registration/wiring to composition roots.
- **[MUST] Cohesion** — Give each artifact one primary reason to change. Split
  independently owned business, transport, persistence, and composition
  concerns; do not split cohesive behavior merely to add indirection.
- **[MUST] Narrow knowledge** — Cross-layer and cross-module code must depend on
  the narrowest stable capability owned by the providing boundary. Do not reach
  through internals or leak persistence, provider, framework, transport, or
  generated implementation types across their owning boundary.
- **[MUST] Cohesive extension** — Reuse a natural extension point or edit the
  current owner. Do not add a handler, strategy, plug-in, wrapper, or parallel
  path solely to avoid changing stable code.
- **[MUST] Present need** — A new port, project, layer, option, abstraction, or
  extension point requires a current boundary, nondeterminism source, alternate
  implementation, test seam, or demonstrated reuse need. Do not add one for
  symmetry or a hypothetical future requirement.
- **[MUST] Composition only** — `Module` and `Hosting` projects compose services,
  endpoints, and infrastructure. They must not own commands, queries, business
  rules, DTO definitions, endpoint bodies, or persistence behavior.

## Native AOT readiness

- **[MUST]** Follow
  [ADR-0010](../../docs/architecture/decisions/0010-prefer-statically-analyzable-native-aot-compatible-design.md)
  and its
  [engineering standard](../../docs/architecture/conventions/adr-0010-static-analyzability-native-aot.md).
  Keep EF Core, provider types, proxies, tracking state, and `IQueryable<T>`
  inside persistence infrastructure; never expose them through
  Domain/Application contracts.

## Scoped persistence concurrency

- **[MUST]** Never run concurrent operations against the same scoped EF Core
  `DbContext`, including through repositories or services backed by that
  context. Do not use `Task.WhenAll` or start multiple unawaited queries over
  shared scoped persistence dependencies. Await those operations sequentially.
  Parallel persistence work requires explicitly independent context instances
  with separately defined transaction, consistency, cancellation, and failure
  behavior.

## Validation

- **[MUST] Agents ownership** — An optional `<Module>.Agents` project may reference
  only its own Application, `Intelligence.Agents`, and approved serialization-safe
  shared libraries. It must not own authorization, tenant/subject binding,
  approvals, mutation policy, persistence, infrastructure, provider SDKs, or
  discovery. Cross-module composition belongs in an app-owned Agents project.

- **[MUST]** For changed module endpoints, run
  `dotnet run --project tools/architecture -- openapi-endpoints`.
- **[MUST]** For changed module Client code or its registration, run
  `dotnet run --project tools/architecture -- internal-module-clients`.
- **[MUST]** For project or layer-boundary changes, inspect the changed project
  references against `docs/architecture.md`; no current static validator proves
  the complete module dependency matrix. Run the focused module tests and the
  applicable merge-level build from `docs/codebase-map.md`, or report exactly
  what was not run and why.
