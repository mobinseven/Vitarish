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
- **[MUST] Named module values** — Keep module-owned routes, operation IDs,
  authorization policy and claim names, event/message types, serialization
  discriminators, configuration keys, and public compatibility keys at their
  authoritative owner. Reuse those values across Domain, Application, Api,
  Client, Infrastructure, and composition code; do not repeat contract or
  business literals in production code or introduce a second source of truth.

## Security-control participation

- **[MUST] Endpoint disposition** — Every changed endpoint must have an
  explicit authentication profile, written anonymous justification when
  anonymous, audit disposition, and user-data transfer disposition. Do not
  infer protection from route grouping, UI reachability, or conventions alone.
- **[MUST] Resource authority** — The owning Application use case authorizes
  tenant, subject, resource, purpose, and capability before accessing user
  data. Hosting filters, endpoint metadata, Files verification, audit success,
  and UI visibility never grant resource access.
- **[MUST] Transfer classification** — Changed import, export, upload,
  download, multipart, binary, or provider-transfer paths declare owner,
  direction, data category/sensitivity, size ceiling, permitted media
  type/format, applicable rate/frequency rule, audit operation, and
  dependency-failure behavior. Enforce transport ceilings before activating
  expensive providers, then enforce owner authorization in Application.
- **[MUST] Integrity ownership** — A module owning sensitive non-file data also
  owns its canonical integrity projection and version. Shared infrastructure
  may persist/verify a keyed manifest but must not discover fields through
  reflection or define the business projection. Integrity failure blocks
  sensitive mutation/export and produces authorized warning/remediation
  evidence.
- **[MUST] External trust** — Infrastructure adapters validate external
  responses and use the shared safe TLS/HTTP boundary. Never add an
  invalid-certificate callback, hostname-validation bypass, per-provider TLS
  downgrade, or retry that stacks with the initiating boundary.
- **[MUST] Sensitive resource reuse** — Clear owned sensitive byte buffers
  before reuse, remove sensitive temporary files on every completion/failure
  path, avoid storage reuse that can reveal prior content, and test the actual
  cleanup path. Do not make an unverifiable claim that the managed runtime
  erases immutable strings.

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

## EF migration safety

- **[MUST] Generated migration ownership** — EF migration classes, designer
  files, and model snapshots are generated output. Never hand-edit, restore,
  reconstruct, copy, or patch them, including to establish a temporary
  baseline. Change the authoritative model and use the canonical EF command to
  generate the migration and snapshot.
- **[MUST] Destructive migration gate** — Before `dotnet ef migrations remove`,
  any `--force` migration command, a rollback to an earlier migration, or a
  direct edit to `__EFMigrationsHistory`, identify the exact target database,
  inspect its applied-migration ledger, and state the schema/data effects.
  Do not execute an operation that can remove schema, data, or migration
  history without the owner's explicit approval for that exact database.
- **[MUST] Applied migration compatibility** — Do not rewrite or remove a
  migration that may be applied outside an explicitly disposable database. Use
  a forward-only corrective migration, or stop and obtain an approved
  compatibility and recovery plan. Never infer that a local development
  database is disposable.

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
