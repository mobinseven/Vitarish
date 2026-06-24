# AGENTS.md — Architecture Rules

These rules are non-negotiable. Every artifact you produce must comply. No exceptions, no shortcuts, no "it's just a small thing."

---

## What "Artifact" Means

Every unit you touch: solution, project, namespace, class, struct, record, interface, enum, method, function, service, repository, controller, handler, command, query, DTO, entity, event, migration, config, test, adapter, port, or integration boundary.

---

## The Ten Rules

**1. One responsibility per artifact.**
One reason to change. If it mixes business logic, persistence, validation, orchestration, transport, or UI — split it now.

**2. One active state model.**
No duplicated domain concepts, no parallel truth sources. If two models exist, define their boundary and mapping explicitly.

**3. Minimize inter-component knowledge.**
Components know only what they directly need. No reaching through object graphs. No leaking internals, persistence details, or framework specifics.

**4. Depend on abstractions, not implementations.**
Core logic depends on stable contracts only. Never on databases, HTTP clients, filesystems, vendor SDKs, clocks, or UI details.

**5. Commands change state. Queries return data. Never both.**
A method that changes state must not return data as a query API. A method that returns data must not change observable state.

**6. Validate all inputs. Handle all failures explicitly.**
Invalid input, unavailable dependencies, partial failure, retries, idempotency, cancellation — handle them intentionally. Silent swallowing of exceptions is a defect.

**7. Extend through new code, not edits to stable code.**
New behavior goes in new implementations, strategies, handlers, or adapters. Do not repeatedly modify stable core logic to add features.

**8. Protect core logic from infrastructure.**
Domain and application logic must never import or reference UI, database, transport, framework, filesystem, network, clock, or third-party SDKs.

**9. Do not add what is not needed now.**
No speculative layers, no fashionable patterns, no generic frameworks built for imagined futures. If it doesn't solve a real present problem, remove it.

**10. One source of truth for every rule and value.**
Business rules, validation logic, state transitions, config values, and shared definitions exist in exactly one place. Duplication is a defect.

---

## Before You Write Anything

Classify each artifact:

- Domain / core logic
- Application / use-case orchestration
- Command (write)
- Query (read)
- Port / contract
- Adapter / infrastructure
- Presentation / API / UI
- Data transfer shape
- Persistence model
- Test
- Configuration / composition root

If an artifact fits more than one category, split it. No exceptions.

---

## Mandatory Steps

1. Identify the smallest set of artifacts required.
2. Assign exactly one responsibility to each.
3. Define boundaries: core → application → infrastructure → presentation.
4. Name the single source of truth for every piece of state.
5. Separate commands from queries.
6. Define abstractions before writing any infrastructure dependency.
7. Ask whether extensibility is needed *now* — not speculatively.
8. Remove every pattern, layer, and abstraction that serves no current requirement.
9. Only then write code.

---

## Hard Stops — Stop and Correct Immediately If:

- A class or method has more than one responsibility.
- A method both mutates state and serves as a query.
- Core logic imports or references infrastructure, frameworks, vendors, or UI.
- A business rule appears in more than one place.
- Two artifacts represent the same state without an explicit ownership boundary.
- A component accesses internals through another component.
- Adding a feature requires editing stable core logic when an extension point exists.
- An error is swallowed, ignored, or left to accidental runtime failure.
- A pattern was chosen because it is fashionable rather than necessary.
- The design is more complex than the requirement demands.

Do not proceed past a hard stop. Identify the violation, explain it, fix it.

---

## Required Audit for Every Artifact

```
Artifact: <name>
Type: <class / method / module / etc.>
Responsibility: <one sentence>

SRP:                    Pass / Fail — reason
Single state model:     Pass / Fail — reason
Low coupling:           Pass / Fail — reason
Abstraction:            Pass / Fail — reason
Command-query:          Pass / Fail / N/A — reason
Failure safety:         Pass / Fail — reason
Open/Closed:            Pass / Fail — reason
Core protection:        Pass / Fail / N/A — reason
KISS/YAGNI:             Pass / Fail — reason
Single source of truth: Pass / Fail — reason
```

Any Fail means the artifact is not done. Fix it before submitting.

---

## Standing Prohibitions

- No `Manager`, `Helper`, `Util`, `Common`, or `Service` names unless the single responsibility is precise and explicit.
- No extension methods that hide business rules, mutate state, or couple unrelated layers.
- No business rules in controllers, repositories, DTOs, migrations, or adapters.
- No CQRS, event sourcing, mediator frameworks, or generic repository abstractions unless they provably reduce complexity for the current problem.
- No inheritance unless it is clearly simpler and safer than composition.

---

## Output Format

**Architecture proposals must include:**
1. Artifact list with one-sentence responsibilities
2. Dependency direction
3. State ownership and source of truth
4. Command / query split
5. Failure-handling strategy
6. Extensibility points — only where justified now
7. What was intentionally left out and why

**Code submissions must include:**
1. Code
2. Per-artifact rule audit
3. Trade-offs
4. Rejected alternatives and why they were rejected

---

The goal is simple, enforceable, correct software. Every rule above is a red line. Treat it accordingly.

---

## Skills

Use skills as task-specific operating manuals.

This section is only a routing index. Do not treat it as a replacement for the skill files.

When a task matches a skill, open and follow that skill’s `SKILL.md`.

Project skills live under:

```txt
skills/
  platform-user-journey-curator/
  platform-feature-roadmap-curator/
  platform-adr-steward/
  platform-test-curator/
  platform-artifact-placement-steward/
  platform-localization-maintenance/
```

If the runtime expects native Codex skill discovery, mirror or symlink these folders under:

```txt
.agents/skills/
```

### Skill Routing

| Situation                                                                 | Use this skill                      | Read                                                  |
| ------------------------------------------------------------------------- | ----------------------------------- | ----------------------------------------------------- |
| User workflow, persona, design objective, pain points, journey stages     | Platform User Journey Curator       | `skills/platform-user-journey-curator/SKILL.md`       |
| Roadmap, phases, MVP/short-term/long-term scope, implementation tickets   | Platform Feature Roadmap Curator    | `skills/platform-feature-roadmap-curator/SKILL.md`    |
| Architecture decision, ADR need, supersession, architecture conflict      | Platform ADR Steward                | `skills/platform-adr-steward/SKILL.md`                |
| Unit tests, required tests, hot test scenarios, risk-based test planning  | Platform Test Curator               | `skills/platform-test-curator/SKILL.md`               |
| File placement, layer ownership, module boundary, artifact classification | Platform Artifact Placement Steward | `skills/platform-artifact-placement-steward/SKILL.md` |
| Localization review, hard-coded UI text, `IStringLocalizer` usage | Platform Localization Maintenance | `.agents/skills/platform-localization-maintenance/SKILL.md` |

### Default Skill Order

Use only the skills needed for the task.

For a new user-facing feature:

```txt
Journey → Roadmap → ADR if needed → Artifact Placement → Test Plan → Implementation
```

For an architecture-sensitive change:

```txt
ADR → Artifact Placement → Test Plan → Implementation
```

For a code placement or refactor task:

```txt
Artifact Placement → Test Plan → Implementation
```

For a test review task:

```txt
Test Curator → Artifact Placement if new test files are needed
```

### Mandatory Checks Before Implementation

Before creating or moving production code, answer:

```txt
1. Is a journey needed?
2. Is a roadmap or task derivation needed?
3. Is an ADR needed?
4. Where should each artifact live?
5. What unit tests are required?
6. What hot test scenarios are required?
```

Skip irrelevant checks for small local bug fixes, styling changes, simple refactors, and test-only corrections.

### Scope and Placement

Generated planning and documentation artifacts should be scoped by owner:

```txt
docs/apps/<App>/modules/<Module>/
  journeys/
  roadmaps/
  tasks/
  tests/
  decisions/

docs/platform/modules/<Module>/
  journeys/
  roadmaps/
  tasks/
  tests/
  decisions/
```

Global indexes are navigation only. The scoped artifact is the source of truth.

### Core Rules

Do not duplicate full skill instructions in `AGENTS.md`.

Do not load every skill by default.

Do not create ceremony without value.

Do not invent product goals, architectural decisions, or testing requirements.

Do not proceed from roadmap directly to implementation when artifact placement or test risk is unresolved.

Preserve the platform architecture principles: single responsibility, simple design, protected core logic, clear boundaries, CQRS separation, and one source of truth.

