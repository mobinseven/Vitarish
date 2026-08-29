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

## Core Architectural Rules

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

## Cross-boundary proof

- **[MUST]** When a change crosses browser/API, authentication, tenant,
  host-composition, persistence, or shared-transport boundaries, use the
  applicable scoped `AGENTS.md` E2E feedback workflow before reporting
  completion.


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

## Durable knowledge

Write information to its owner: architecture decisions to ADRs or `docs/architecture.md`; navigation/commands to `docs/codebase-map.md`; repeatable procedures to skills/runbooks; actionable debt to the issue/debt tracker with owner and removal condition; rediscovery-prevention lessons and unfinished migration context to `docs/memory.md`. Do not turn memory into a project log.

## Needless to say

* **Cleaning-up [MUST]** — Always clean up after yourself. When you abandon a folder, you are the one who must delete the folder. When a file (log, screenshot, etc.) is no longer needed, you the creator are the one who must delete it.
* **Named values [MUST]** — Do not duplicate string literals that represent a route, operation ID, configuration key, authorization claim or policy, event/message type, serialization discriminator, localization key, feature flag, cache key, or other contract or business value. Define each value at its authoritative owner and reuse it through an existing typed constant, enum, options, contract, or resource mechanism. An inline literal is acceptable only when it is local, self-explanatory, non-contractual, and does not encode a rule or compatibility boundary; exact-value tests may use literals when the literal itself is what the test asserts. Do not create broad `Constants`, `Common`, or wrapper abstractions solely to hide a literal.
* **Plan-agnostic names [MUST]** — Durable source files, types, commands, diagnostics, documentation, and configuration values must express their responsibility or current contract, not a temporary workstream, plan, stage, or phase label such as `Stage1` or `Phase2`. Use a plan/phase label only for an explicitly temporary artifact whose removal condition is recorded in the same authoritative plan or ADR.