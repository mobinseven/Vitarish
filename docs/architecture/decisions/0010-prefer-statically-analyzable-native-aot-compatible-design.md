# ADR-0010: Prefer Statically Analyzable and Native AOT–Compatible Design for the .NET Backend

Date: 2026-07-11
Status: Accepted
Scope: Platform .NET backend
Owner: Architecture
Supersedes:
Superseded by:
Related: [Static Analyzability & Native AOT Engineering Standard](../conventions/adr-0010-static-analyzability-native-aot.md)

## Context and Problem Statement

The backend is a set of ASP.NET Core Minimal API hosts behind a React frontend. Minimal APIs are structurally compatible with Native AOT, but the current persistence implementation relies on EF Core, whose Native AOT support does not yet meet our production requirements.

Because EF Core already blocks full Native AOT publication, there is a risk that the rest of the codebase treats AOT compatibility as irrelevant by default and accumulates unrelated runtime dynamism — broad assembly scanning, reflection-based mapping, service-locator patterns, and similar — none of which is actually required by EF Core. That dynamism has costs independent of AOT: it hides behavior from the compiler and reviewers, makes startup and runtime behavior harder to predict, and makes trimming or a future partial persistence change harder than it needs to be.

We need a standing decision on how much static-analysis discipline to apply outside the persistence layer, and how to keep that discipline evidence-based (verified in CI) rather than aspirational.

## Decision Drivers

- **Compile-time visibility** — behavior should be checkable by the compiler, analyzers, and reviewers wherever the cost of doing so is reasonable.
- **Runtime predictability** — reduce sensitivity to assembly load order, reflection-discovered types, and naming-convention-based behavior.
- **Deployment optionality** — keep the door open to (a) publishing with Native AOT once EF Core's support matures, or (b) swapping persistence in specific hosts, without a large, unrelated migration.
- **Engineering pragmatism** — avoid turning AOT-friendliness into a rule that produces speculative abstraction, boilerplate, or degraded maintainability.
- **Evidence over convention** — any compatibility claim must be backed by CI validation, not asserted by coding style alone.

## Alternatives Considered

1. **Adopt full Native AOT immediately, including persistence.** Replace or heavily constrain EF Core now to unblock a complete AOT build.
2. **No AOT-related constraint.** Let each module use whatever is most convenient (reflection, scanning, dynamic proxies) and revisit only if/when a full AOT migration is undertaken later.
3. **Apply AOT-friendly, statically analyzable patterns as the default everywhere except persistence; isolate EF Core as a single, documented, continuously reassessed exception.** *(chosen)*

## Decision Outcome

**Chosen option: 3 — statically analyzable design by default, with EF Core isolated as an accepted exception.**

Option 1 was rejected: EF Core's current Native AOT support does not satisfy our production requirements around change tracking, migrations, and provider behavior. Forcing a persistence rewrite purely to unblock AOT would be a technology change made for the wrong reason.

Option 2 was rejected: treating the EF Core gap as license for unconstrained runtime dynamism elsewhere would let hidden coupling spread through application and domain code that has no actual dependency on EF Core, with no offsetting benefit.

Option 3 captures most of the engineering value of AOT-style design — compile-time visibility, predictable startup, deterministic serialization, trimming-safety — without requiring a persistence migration, and treats the EF Core gap as a scoped, owned, revisitable exception rather than grounds for abandoning the discipline elsewhere.

Native AOT compatibility is treated as a **design input and quality signal**, not an unconditional optimization target that can override correctness, domain integrity, or maintainability.

### What this means in practice

- New and modified application, domain, API, serialization, DI, validation, configuration, and messaging code defaults to compile-time-generated or explicitly registered patterns over runtime discovery, reflection, or dynamic code generation.
- EF Core stays inside the persistence infrastructure layer. It must not appear in API contracts, domain models, or application-facing interfaces — no leaked `IQueryable<T>`, `DbContext`, entities, or lazy-loading proxies.
- Persistence is abstracted around use cases and aggregates, not a generic `IRepository<T>`. The goal is isolating EF Core, not building a lowest-common-denominator abstraction over it.
- Compatibility is verified continuously — trimming analysis plus a Native AOT compatibility probe in CI — rather than asserted by convention.
- The concrete rules, code patterns, CI checks, and review checklist that operationalize this decision live in the companion **Engineering Standard** (linked above), so they can evolve with the .NET SDK without re-opening this ADR.

## Consequences

### Positive

- More backend behavior (DI registration, endpoint mapping, JSON contracts, polymorphism) becomes visible at compile time and in code review, rather than discovered at runtime.
- Runtime configuration errors surface earlier, via startup validation and source-generated JSON contexts.
- Application and domain layers stay independent of EF Core, keeping a future persistence change — full or partial — contained to infrastructure.
- Trimming and AOT issues are caught by CI rather than discovered at deployment time.
- New dependencies are evaluated deliberately against known criteria instead of adopted for convenience.

### Negative / Trade-offs

- Some registration and mapping code becomes more explicit and verbose than an equivalent reflection-based shortcut.
- Source generators, analyzers, and a Native AOT compatibility probe add build and CI time.
- Some otherwise-convenient third-party libraries (reflection-heavy mappers, dynamic-proxy helpers) become harder to justify adopting.
- The team needs enough familiarity with trimming and AOT metadata preservation to interpret warnings correctly rather than suppress them reflexively.
- Even with this discipline, replacing EF Core later will still require real work, because persistence semantics — change tracking, concurrency, transactions — are not automatically portable. This decision reduces *unrelated* migration work; it does not make persistence migration itself easy.

### Known Exception

EF Core is an accepted, isolated Native AOT blocker for the current production persistence implementation. It is confined to persistence infrastructure and migration tooling; domain and application projects do not reference it directly. This exception is tracked in the Engineering Standard's exception registry and re-evaluated on the triggers below.

## Validation

This decision only holds if it's checked, not just followed by convention. At minimum, CI should:

- Run trimming analysis on the applicable host(s).
- Publish and smoke-test a Native AOT compatibility probe covering all production code except the known EF Core blocker.
- Audit backend package references against an approved compatibility baseline.
- Run architecture tests confirming domain/application projects don't reference EF Core or ASP.NET Core inappropriately, and that `IQueryable<T>` / `DbContext` don't cross into application-facing APIs.

The concrete implementation of these checks — scripts, CI jobs, specific IL warning codes — is specified in the Engineering Standard, not here, so SDK-version-specific detail can change without re-opening this ADR.

## Non-Goals

This decision does not require:

- Immediate Native AOT publication of the production API.
- Removal of EF Core.
- Prohibition of all reflection, or of all libraries that aren't Native AOT-compatible.
- A single persistence technology across every host.
- Generic abstraction over every infrastructure dependency for its own sake.

## Review Trigger

Revisit this ADR on: a major .NET release, a material change in EF Core's Native AOT support, a persistence technology change, or the introduction of a new runtime-dynamic framework into the backend.
