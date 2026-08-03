# Module agent routing

Applies to business modules under this directory. The repository and scoped
`AGENTS.md` instructions own backend boundary guidance. Do not load React,
Web-route, or Web-layout skills for module-only work.
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
