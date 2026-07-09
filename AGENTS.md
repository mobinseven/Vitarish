# AGENTS.md - Architecture Rules

These rules are non-negotiable. Every artifact you produce must comply. No exceptions, no shortcuts, no "it is just a small thing."

---

## What Artifact Means

Every unit you touch: solution, project, namespace, class, struct, record, interface, enum, method, function, service, command, query, DTO, entity, event, migration, config, test, adapter, port, controller, handler, repository, document, skill, or integration boundary.

---

## The Ten Rules

**1. One responsibility per artifact.**
One reason to change. If it mixes business logic, persistence, validation, orchestration, transport, or UI, split it.

**2. One active state model.**
No duplicated domain concepts, no parallel truth sources. If two models exist, define their boundary and mapping explicitly.

**3. Minimize inter-component knowledge.**
Components know only what they directly need. No reaching through object graphs. No leaking internals, persistence details, or framework specifics.

**4. Depend on abstractions, not implementations.**
Core logic depends on stable contracts only. Never on databases, HTTP clients, filesystems, vendor SDKs, clocks, or UI details.

**5. Commands change state. Queries return data. Never both.**
A method that changes state must not return data as a query API. A method that returns data must not change observable state.

**6. Validate all inputs. Handle all failures explicitly.**
Invalid input, unavailable dependencies, partial failure, retries, idempotency, cancellation, and concurrency must be handled intentionally. Silent swallowing is a defect.

**7. Extend through new code, not edits to stable code.**
New behavior goes in new implementations, strategies, handlers, or adapters when an extension point already exists.

**8. Protect core logic from infrastructure.**
Domain and Application logic must never import or reference UI, database, transport, framework, filesystem, network, clock, or third-party SDKs.

**9. Do not add what is not needed now.**
No speculative layers, fashionable patterns, or generic frameworks for imagined futures. If it does not solve a present problem, remove it.

**10. One source of truth for every rule and value.**
Business rules, validation logic, state transitions, config values, shared definitions, user-facing copy, and generated artifacts exist in exactly one owner.

---

## Minimum Necessary Change

After reading the task and the code it touches, stop at the first rung that holds:

1. Does this need to exist? If not, skip it and say why.
2. Does this already exist in this codebase? Reuse the local pattern, helper, contract, component, hook, or validator.
3. Does the standard library/runtime already do it? Use it.
4. Does the native platform already do it? Use it.
5. Does an already-installed dependency solve it? Use it before adding a new dependency.
6. Can one line or one existing pattern cover it? Use that.
7. Only then write the minimum custom code that works.

This ladder shortens solutions, not understanding. For bug fixes, trace callers and fix the root cause at the shared boundary when that is the smallest correct fix.

Never simplify away trust-boundary validation, explicit failure handling, cancellation, idempotency, data-loss protection, security, accessibility, localization, architecture boundaries, or required tests.

---

## Before Writing Anything

Classify each artifact:

- Domain / core logic
- Application / use-case orchestration
- Command / write
- Query / read
- Port / contract
- Adapter / infrastructure
- Presentation / API / UI
- Data transfer shape
- Persistence model
- Test
- Configuration / composition root
- Documentation / skill

If an artifact fits more than one category, split it.

---

## Mandatory Steps

1. Identify the smallest set of artifacts required.
2. Assign exactly one responsibility to each.
3. Define boundaries: core -> application -> infrastructure -> presentation.
4. Name the single source of truth for every piece of state and rule.
5. Separate commands from queries.
6. Define abstractions before writing any infrastructure dependency.
7. Ask whether extensibility is needed now, not speculatively.
8. Remove every pattern, layer, and abstraction that serves no current requirement.
9. Only then write code.

---

## Hard Stops

Stop and correct immediately if:

- A class, method, module, skill, or document has more than one responsibility.
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

## Required Audit

For every changed artifact in a code submission, include or be able to produce this audit:

```txt
Artifact: <name>
Type: <class / method / module / document / skill / etc.>
Responsibility: <one sentence>

SRP:                    Pass / Fail - reason
Single state model:     Pass / Fail - reason
Low coupling:           Pass / Fail - reason
Abstraction:            Pass / Fail - reason
Command-query:          Pass / Fail / N/A - reason
Failure safety:         Pass / Fail - reason
Open/Closed:            Pass / Fail - reason
Core protection:        Pass / Fail / N/A - reason
KISS/YAGNI:             Pass / Fail - reason
Single source of truth: Pass / Fail - reason
```

Any Fail means the artifact is not done.

---

## Standing Prohibitions

- No `Manager`, `Helper`, `Util`, `Common`, or vague `Service` names unless the single responsibility is precise and explicit.
- No extension methods that hide business rules, mutate state, or couple unrelated layers.
- No business rules in controllers, repositories, DTOs, migrations, adapters, UI components, generated clients, localization resources, or skill files.
- No CQRS, event sourcing, mediator frameworks, generic repositories, custom design-system wrappers, or new package managers unless they provably reduce complexity for the current problem.
- No inheritance unless it is clearly simpler and safer than composition.
- No manual edits to generated clients, generated DTOs, generated hooks, generated query keys, or generated MSW mocks.

---

## Skills

Use skills as task-specific operating manuals.

Project skills live under `.agents/skills/`. Claude bridge skills live under `.claude/skills/` and only point back to the canonical skill files.

When a task matches a skill, open and follow that skill's `SKILL.md`. Do not load every skill by default.

### Current Skill Set

```txt
.agents/skills/
  platform-scope-planning/
  platform-architecture-governance/
  platform-api-client-boundary/
  platform-react-ui-boundary/
  platform-test-strategy/
  platform-delivery-loop/
  platform-simplification-audit/
  platform-skill-governance/
```

### Skill Routing

| Situation | Use this skill | Read |
| --- | --- | --- |
| Product intent, user workflow, journey/roadmap need, implementation task derivation, PR slicing, ambiguous feature scope | Platform Scope Planning | `.agents/skills/platform-scope-planning/SKILL.md` |
| Artifact classification, file placement, dependency direction, ADR impact, frontend/backend isolation, architecture audit | Platform Architecture Governance | `.agents/skills/platform-architecture-governance/SKILL.md` |
| OpenAPI endpoint metadata, operation IDs, contract DTOs, generated React clients, apiFetch, generated hooks, API-to-React handoff | Platform API Client Boundary | `.agents/skills/platform-api-client-boundary/SKILL.md` |
| React Web pages/components/hooks, HeroUI, forms/tables/dialogs, localization, formatting, accessibility, shared Web UI | Platform React UI Boundary | `.agents/skills/platform-react-ui-boundary/SKILL.md` |
| Required tests, regression coverage, test placement, validation commands, hot test scenarios | Platform Test Strategy | `.agents/skills/platform-test-strategy/SKILL.md` |
| One focused delivery loop, approved plan execution, auto-implementation loop, review verdicts, feedback triage, handoff | Platform Delivery Loop | `.agents/skills/platform-delivery-loop/SKILL.md` |
| Simplification review, over-engineering audit, deletion opportunities, standard-library/native replacement, bloat criticism | Platform Simplification Audit | `.agents/skills/platform-simplification-audit/SKILL.md` |
| Skill creation, deletion, consolidation, metadata, bridges, `AGENTS.md` harmony, stale skill routing | Platform Skill Governance | `.agents/skills/platform-skill-governance/SKILL.md` |

### Default Chains

Use only the steps that apply:

```txt
New feature:
Scope Planning -> Architecture Governance -> Test Strategy -> Delivery Loop

OpenAPI to React:
Architecture Governance -> API Client Boundary -> React UI Boundary -> Test Strategy -> Delivery Loop

React UI:
React UI Boundary -> Test Strategy -> Delivery Loop

Architecture-sensitive change:
Architecture Governance -> Test Strategy -> Delivery Loop

Simplification:
Simplification Audit -> Architecture Governance if boundaries are affected

Skill maintenance:
Skill Governance -> Architecture Governance if repository policy changes
```

### Mandatory Checks Before Implementation

For non-trivial changes, answer:

```txt
1. Is a journey, roadmap, or task derivation needed?
2. Is an ADR or architecture-governance decision needed?
3. Does the change affect OpenAPI, generated clients, API/client boundaries, React UI, HeroUI, localization, or shared Web UI?
4. Where should each artifact live?
5. What deterministic tests or validators are required?
6. What hot scenarios require runtime validation?
7. Can the solution be smaller by reuse, deletion, stdlib/runtime, native platform behavior, or installed dependencies?
```

Skip irrelevant checks for small local bug fixes, styling changes, simple refactors, and test-only corrections.

