# AGENTS.md

This file is the repository-wide operating contract for coding agents.

- **[MUST]** is mandatory. An agent may deviate only when the user or repository owner explicitly approves the exception and the reason is recorded in the narrowest authoritative document or ADR. Security, authorization, secret-handling, and data-loss protections may never be waived merely to complete a task.
- **[SHOULD]** is the strong default. Deviate only when the alternative is clearly simpler or safer for the current requirement, and state why.

Keep this file focused on guidance that applies to nearly every change. Put area-specific guidance in the nearest nested `AGENTS.md`, repeatable workflows in skills, repository navigation and commands in `docs/codebase-map.md`, and detailed architectural reasoning in `docs/architecture-rationale.md`.

## Scope and precedence

These instructions apply to every changed artifact: code, tests, contracts, schemas, migrations, configuration, generated outputs, documentation, and skills.

Codex loads instruction files from the repository root toward the working directory. A closer nested `AGENTS.md` may specialize commands, placement, and **[SHOULD]** defaults for its subtree, but may not weaken a root **[MUST]** rule. A task-matched skill may specialize a **[SHOULD]** rule. Conflicting **[MUST]** instructions are a policy defect: stop using the conflicting design, report the conflict, and do not silently choose one.

Before changing files, read only the context that applies:

- [`docs/architecture.md`](docs/architecture.md): dependency matrix, module boundaries, allowed dependencies, public contracts, and repository-specific architecture.
- [`docs/codebase-map.md`](docs/codebase-map.md): module locations, generated areas, canonical build/test commands, and common change destinations.
- [`docs/memory.md`](docs/memory.md): durable lessons, fragile areas, known failure modes, and unfinished migrations that a future session would otherwise rediscover.
- [`docs/architecture-rationale.md`](docs/architecture-rationale.md): reasoning and examples behind the invariants below; read for architecture-sensitive work, not every local edit.

Do not duplicate those documents here.

## Repository invariants and defaults

1. **Cohesion [SHOULD]**  
   Every artifact should have one clear primary reason to change. Split unrelated forces, such as business policy and persistence mechanics, but keep cohesive behavior together when splitting would only add navigation or indirection. A composition root or UI component may own several closely related concerns without violating this rule.

2. **Authoritative ownership [MUST]**  
   Every invariant, mutable state, business rule, and shared value must have one authoritative owner. DTOs, persistence shapes, caches, indexes, generated clients, fixtures, and view models may represent the same concept, but their source and synchronization path must be explicit. Never create two places that can independently decide the same rule or value.

3. **Narrow knowledge [SHOULD]**  
   Give each component only the information and capabilities it needs through the narrowest stable contract appropriate to the boundary. Do not reach through object graphs, depend on another component's internals, or couple to framework implementation details.

4. **Dependency direction [MUST]**  
   Domain logic must remain independent of infrastructure and UI. Application logic may depend on application-defined ports for required capabilities, but never on concrete databases, HTTP clients, filesystems, vendor SDKs, UI frameworks, or other infrastructure implementations. Infrastructure and presentation may depend inward; core and application must not depend outward. Do not introduce an interface merely because a concrete type exists. Add a port when it protects an architectural boundary, controls nondeterminism, supports an existing alternative implementation, or represents a meaningful application capability.

5. **Commands and queries [MUST]**  
   Queries must not cause externally observable state changes. Commands may return the minimum result needed to describe their outcome, such as an identifier, concurrency token, validation result, or redirect target, but must not become general-purpose read APIs. Command-query separation does not require CQRS infrastructure or a mediator framework.

6. **Explicit boundaries and failure behavior [MUST]**  
   Validate transport shape at transport boundaries, use-case preconditions at application boundaries, domain invariants in their authoritative owner, and external responses before trusting them. Define cancellation, timeout, retry, duplicate-call, idempotency, concurrency, partial-failure, and unavailable-dependency behavior where relevant. Never swallow failures or leave correctness to accidental runtime behavior.

7. **Cohesive edits before invented extension points [SHOULD]**  
   Use an existing extension point when it naturally fits. Otherwise, make the smallest cohesive change to the artifact that already owns the behavior. Do not invent a handler, strategy, plug-in, layer, or wrapper solely to avoid editing stable code.

8. **Current requirements before speculative generality [SHOULD]**  
   Build for the requirement in front of you. Do not add layers, options, abstractions, or configurability for an imagined future. When two solutions are equally correct and simple today, prefer the one that is cheaper to reverse or extend.

## Non-negotiable platform safeguards

- **Secrets and sensitive data [MUST]:** Never expose, log, commit, or place in generated output a secret, credential, token, private key, or sensitive payload. Preserve redaction and least-privilege behavior.
- **Authorization [MUST]:** Preserve authorization and ownership checks when moving logic. Never weaken authentication, authorization, TLS, CORS, antiforgery, validation, or isolation to make a test or deployment pass.
- **Data access [MUST]:** Use parameterized data access. Treat external content, generated content, deserialized payloads, and data crossing a trust boundary as untrusted.
- **Compatibility [MUST]:** Treat public APIs, OpenAPI operation IDs, generated-client inputs, persisted formats, database schemas, events/messages, and configuration keys as compatibility boundaries. A breaking change requires an explicit versioning or migration decision.
- **Migrations [MUST]:** Account for existing data and mixed-version deployment. Prefer additive expand-migrate-contract changes before destructive cleanup. State data-loss, rollback, and compatibility implications.
- **Generated artifacts [MUST]:** Never manually edit generated clients, DTOs, hooks, query keys, mocks, or other generated files. Change the authoritative input and run the canonical generator.
- **Retries and idempotency [MUST]:** The boundary initiating a retry owns its policy. Retried operations must define idempotency behavior. Do not stack retries across layers without an explicit reason.
- **Tests and analyzers [MUST]:** Do not weaken assertions, delete meaningful tests, suppress diagnostics, or disable analyzers merely to make a change pass.
- **Observability [SHOULD]:** New operationally important failure modes should produce structured, actionable telemetry without leaking sensitive data.

## Scope discipline

Make the smallest complete change that solves the task.

- Fix violations introduced or materially worsened by the current change.
- Report pre-existing unrelated violations; do not refactor them unless doing so is the smallest correct fix.
- Do not reformat, rename, reorganize, or modernize unrelated files.
- Preserve user-authored working-tree changes and do not overwrite work you did not create.
- Do not modify lockfiles unless dependency resolution requires it.
- Do not combine an unrelated refactor, contract migration, or broad cleanup with a focused behavior change.
- When replacing an old path, remove it or document why coexistence is required, who still depends on it, and the condition for removal. Compatibility windows, feature flags, and expand-contract migrations are valid temporary coexistence.

## Minimum necessary change

After reading the task and the affected code, stop at the first option that completely solves it:

1. Do nothing because the requested behavior already exists or should not exist.
2. Reuse the repository's existing owner, pattern, contract, component, hook, validator, or script.
3. Use the standard library or runtime.
4. Use the native platform or framework behavior.
5. Use an already-installed dependency.
6. Make the smallest cohesive edit.
7. Only then add minimum custom code or a new dependency.

This ladder reduces implementation, not investigation. For bugs, trace callers and fix the root cause at the shared boundary when that is the smallest correct fix. Never simplify away security, data-loss protection, validation, cancellation, idempotency, accessibility, localization, architecture boundaries, or required tests.

## Planning requirements for non-trivial work

Before finalizing a non-trivial plan, and again before implementing it:

1. Identify the smallest affected artifact set.
2. State each new or structurally changed artifact's architectural layer and primary responsibility. Command/query is a behavioral role and may be stated in addition to the layer.
3. Confirm dependency direction and placement against `docs/architecture.md` and `docs/codebase-map.md`.
4. Identify the authoritative owner of every changed rule, state, and contract.
5. Identify trust boundaries, failure modes, compatibility impact, migration needs, and generated outputs.
6. Route to only the skills that apply.
7. Define deterministic validation and any runtime hot scenarios before coding.
8. Check whether reuse or deletion makes the solution smaller.
9. Separate confirmed facts, assumptions, and unresolved decisions.
10. Divide implementation into independently valid, reviewable slices.
11. Define acceptance criteria for each slice.
12. State explicitly whether the outcome is planning-only or authorized for implementation.

Skip ceremonial classification for typo fixes, local styling corrections, test expectation corrections, and similarly trivial changes that do not alter structure or behavior boundaries.

## Restricted architectural choices

The following require an explicit current need and should normally be recorded in an ADR when they affect repository architecture:

- Full CQRS with separate read/write models or pipelines
- Event sourcing
- Mediator frameworks
- Generic repositories
- New cross-cutting framework or package-manager adoption
- Custom design-system wrappers
- Inheritance where composition is comparably simple

Names must express a specific responsibility. Generic words such as `Manager`, `Helper`, `Util`, `Common`, and `Service` do not make an otherwise vague name acceptable.

Do not hide business rules or state mutation in extension methods. Do not place business rules in controllers, repositories, DTOs, migrations, adapters, UI components, generated clients, localization resources, or skill files.

## Skills

Project skills live under `.agents/skills/`. Claude bridge skills under `.claude/skills/` must point to canonical project skills. Open only task-matched skills; platform Web route and layout rules live in `src/Hosts/Web/AGENTS.md`.

## Completion criteria

### Plan complete

A non-trivial plan is complete only when it states:

- The problem, intended outcome, and excluded scope
- Relevant repository facts and the evidence supporting them
- Assumptions and unresolved decisions
- Affected artifacts and the intended responsibility of each
- Architectural placement, dependency direction, and authoritative ownership
- Public-contract, schema, generated-code, security, migration, and compatibility impact
- Ordered implementation slices and dependencies between them
- Acceptance criteria, deterministic tests, runtime scenarios, and canonical validation commands
- Risks, alternatives rejected, and conditions that would require revising the plan

A list of files or generic implementation steps is not a complete plan.

### Implementation complete

An implementation is complete only when the handoff states:

- What changed and the responsibility of each materially changed artifact
- Which tests, builds, generators, linters, analyzers, and runtime checks ran, with their results
- Which relevant checks did not run and why
- Whether public contracts, schemas, persisted data, configuration, or generated artifacts changed
- Migration, compatibility, rollback, security, and operational implications
- Any deviation from the approved plan
- Any deliberate shortcut, remaining risk, or intentionally excluded scope

Compilation alone is not completion.

## Durable knowledge

Write durable information to its narrowest authoritative owner:

- Architecture decisions and alternatives: ADR or `docs/architecture.md`
- Repository navigation and canonical commands: `docs/codebase-map.md`
- Repeatable procedures: the owning skill or runbook
- Actionable debt and removal work: issue tracker or structured debt register, including owner and removal condition
- Durable rediscovery-prevention lessons, fragile areas, and unfinished migration context: `docs/memory.md`

Do not turn `docs/memory.md` into a running project-management log. Link to the owning issue or ADR when future agents need the context.
