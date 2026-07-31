# Module agent routing

Applies to business modules under this directory. Classify the touched slice
before loading skills; a module name alone does not justify every backend skill.

| Touched decision | Load |
| --- | --- |
| Domain or Application behavior, commands, queries, workflows, or ports | `platform-backend-use-case` |
| API, Contracts, routes, DTOs, OpenAPI, Client transport, or realtime protocols | `platform-transport-contract` |
| Infrastructure persistence, persisted formats, or migrations | `platform-data-evolution` |
| Infrastructure adapter for a remote vendor/provider | `platform-external-integration` |
| Authentication, authorization, tenant ownership, privacy, abuse, or other untrusted-input boundaries | `platform-trust-boundary` |
| New/moved slices, cross-module dependencies, or an ADR decision | `platform-artifact-architecture` |

Do not load React, Web-route, or Web-layout skills for module-only work.
`Module` projects are composition roots: keep behavior in its owning inner
slice. Generated clients and DTOs must be changed through their authoritative
source and generator.

## Native AOT readiness

- **[MUST]** Apply the root Native AOT readiness rule. Keep EF Core, provider
  types, proxies, tracking state, and `IQueryable<T>` inside persistence
  infrastructure; never expose them through Domain/Application contracts.

## Scoped persistence concurrency

- **[MUST]** Never run concurrent operations against the same scoped EF Core
  `DbContext`, including through repositories or services backed by that
  context. Do not use `Task.WhenAll` or start multiple unawaited queries over
  shared scoped persistence dependencies. Await those operations sequentially.
  Parallel persistence work requires explicitly independent context instances
  with separately defined transaction, consistency, cancellation, and failure
  behavior.
