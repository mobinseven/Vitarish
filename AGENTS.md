# AGENTS.md

Repository-wide operating contract for coding agents.

- **[MUST]** is mandatory. Deviate only with explicit owner approval recorded in the narrowest authoritative document or ADR. Never waive security, authorization, secret handling, or data-loss protection merely to finish work.
- **[SHOULD]** is the strong default. Deviate only for a clearly simpler or safer solution and state why.

## Scope and precedence

Applies to every changed artifact. A nearer `AGENTS.md` may specialize commands, placement, and **[SHOULD]** rules, but cannot weaken a root **[MUST]**. A matched skill may specialize **[SHOULD]** guidance. Conflicting **[MUST]** rules are a policy defect: stop, report them, and do not choose silently.

Read only relevant sources:

- `docs/architecture.md`: boundaries, dependency matrix, public contracts.
- `docs/codebase-map.md`: locations, generated areas, canonical commands.
- `docs/memory.md`: durable lessons, fragile areas, unfinished migrations.
- `docs/architecture-rationale.md`: reasoning for architecture-sensitive work.

Do not duplicate those documents here.

## Core rules

1. **Cohesion [SHOULD]** — Give each artifact one primary reason to change. Split unrelated forces; do not split cohesive behavior merely to add indirection.
2. **Authoritative ownership [MUST]** — Every rule, invariant, mutable state, and shared value has one owner. Derived DTOs, persistence shapes, caches, indexes, generated clients, fixtures, and view models must have an explicit source and synchronization path.
3. **Narrow knowledge [SHOULD]** — Expose only the information and capabilities required through the narrowest stable boundary. Do not reach through internals or framework details.
4. **Dependency direction [MUST]** — Domain is independent of Infrastructure and UI. Application may depend only on Application-owned ports, never concrete databases, HTTP/filesystem clients, vendor SDKs, UI frameworks, or other outward implementations. Add a port only for a real boundary, nondeterminism, alternative implementation, or meaningful capability.
5. **Commands and queries [MUST]** — Queries cause no observable state change. Commands return only the outcome data required; they are not read APIs. CQRS infrastructure is optional.
6. **Explicit boundaries [MUST]** — Validate transport shape at Transport, use-case preconditions at Application, invariants at their owner, and external responses before trust. Define relevant cancellation, timeout, retry, duplicate-call, idempotency, concurrency, partial-failure, and dependency-unavailable behavior. Never swallow failures.
7. **Cohesive edits [SHOULD]** — Reuse a natural extension point; otherwise edit the current owner. Do not invent handlers, strategies, plug-ins, layers, or wrappers solely to avoid changing stable code.
8. **Current requirements [SHOULD]** — Avoid speculative layers, options, abstractions, and configuration. When solutions are equally correct, prefer the simpler and more reversible one.

## Safeguards

- **Secrets/data [MUST]:** Never expose, log, commit, or generate secrets, credentials, private keys, or sensitive payloads. Preserve redaction and least privilege.
- **Trust [MUST]:** Never weaken authentication, authorization, ownership/tenant checks, TLS, CORS, antiforgery, validation, or isolation to pass tests or deployment.
- **Data access [MUST]:** Parameterize data access. Treat external, generated, deserialized, and cross-boundary content as untrusted.
- **Compatibility [MUST]:** Public APIs, operation IDs, generated-client inputs, persisted formats, schemas, events/messages, and configuration keys are compatibility boundaries. Breaking changes require explicit versioning or migration.
- **Migrations [MUST]:** Account for existing data and mixed versions. Prefer expand-migrate-contract; state data-loss, rollback, and compatibility effects.
- **Generated output [MUST]:** Never edit generated clients, DTOs, hooks, keys, mocks, or equivalent output. Change the source and run the canonical generator.
- **Retries [MUST]:** The initiating boundary owns retry policy. Retried mutations require idempotency. Do not stack retries without explicit reason.
- **Quality [MUST]:** Do not weaken assertions, tests, analyzers, validators, or diagnostics to obtain a pass.
- **.NET Native AOT readiness [MUST]:** Backend changes follow [ADR-0010](docs/architecture/decisions/0010-prefer-statically-analyzable-native-aot-compatible-design.md) and its [engineering standard](docs/architecture/conventions/adr-0010-static-analyzability-native-aot.md). Prefer generated or explicit behavior over runtime discovery; isolate unavoidable dynamism and the documented EF Core persistence exception. Evaluate runtime dependencies, treat warnings as actionable, and report missing required validation. Readiness does not require publishing the current production host with Native AOT and never overrides correctness, security, or maintainability.
- **Observability [SHOULD]:** Important failures produce structured, actionable, redacted telemetry.

## Scope and minimum change

Make the smallest complete change. Fix violations introduced or materially worsened by it; report unrelated pre-existing issues. Do not reformat, rename, reorganize, modernize, or refactor unrelated files. Preserve user changes. Modify lockfiles only when dependency resolution requires it.

When replacing a path, remove it or document the compatibility window, remaining dependents, owner, and removal condition.

Stop at the first complete option:

1. No change needed.
2. Reuse an existing owner, pattern, contract, component, hook, validator, or script.
3. Use the standard library/runtime.
4. Use native platform/framework behavior.
5. Use an installed dependency.
6. Make the smallest cohesive edit.
7. Add only the minimum custom code or dependency.

For bugs, trace callers and fix the shared root cause when that is the smallest correct fix. Never simplify away security, data safety, validation, cancellation, idempotency, accessibility, localization, boundaries, or required tests.

## Planning and delivery

For non-trivial work, record:

- outcome, non-goals, repository evidence, assumptions, and unresolved decisions;
- smallest artifact set, layer, primary responsibility, owner, and dependency direction;
- trust, failure, contract, compatibility, migration, configuration, and generated-output effects;
- ordered independently valid slices, acceptance criteria, deterministic validation, and runtime hot scenarios;
- applicable skills and whether work is planning-only or implementation-authorized.

Skip this ceremony for trivial edits that do not alter behavior or structural boundaries.

A plan is complete only when it provides the above plus risks, rejected alternatives, and revision triggers. A file list or generic steps are insufficient.

An implementation handoff must state material changes, validation commands/results, unrun checks and reasons, contract/data/configuration/generated-output effects, migration/rollback/security/operational implications, plan deviations, residual risk, and excluded scope. Compilation alone is insufficient.

## Restricted choices

Require a current need and normally an ADR when repository architecture changes: full CQRS with separate models/pipelines, event sourcing, mediator frameworks, generic repositories, new cross-cutting frameworks or package managers, custom design-system wrappers, and inheritance where composition is comparably simple.

Names must express a specific responsibility; `Manager`, `Helper`, `Util`, `Common`, or `Service` cannot rescue a vague name. Do not hide business rules or mutation in extension methods or place business rules in controllers, repositories, DTOs, migrations, adapters, UI, generated clients, localization resources, or skills.

## Skills

Skills live in `.agents/skills/`. Load only those owning current decisions. The
nearest scoped `AGENTS.md` owns boundary-skill routing for its subtree; do not
load a backend, Web, transport, persistence, integration, or trust skill merely
because it exists in the repository-wide catalog.

| Trigger | Skill |
| --- | --- |
| Plan scope, assumptions, slices, acceptance | `platform-change-planning` |
| Authorized implementation or final handoff | `platform-delivery-loop` |
| Web route shells, viewport, main/scroll ownership | `apply-web-layout-policy` |
| Tests, validators, builds, generators, hot scenarios | `platform-test-validation` |
| Requested simplification or notable new layers | `platform-simplification-audit` |
| Skill-system maintenance | `platform-skill-governance` |

Planning uses `platform-change-planning`; implementation uses
`platform-delivery-loop`. Add only boundary skills allowed by every scoped
`AGENTS.md` covering the changed artifact. Use `platform-test-validation` after
boundaries are known. Planning-only work stops before edits unless
implementation is explicitly authorized. See `.agents/skills/README.md` only
for mode and layout-routing edge cases.

## Durable knowledge

Write information to its owner: architecture decisions to ADRs or `docs/architecture.md`; navigation/commands to `docs/codebase-map.md`; repeatable procedures to skills/runbooks; actionable debt to the issue/debt tracker with owner and removal condition; rediscovery-prevention lessons and unfinished migration context to `docs/memory.md`. Do not turn memory into a project log.
