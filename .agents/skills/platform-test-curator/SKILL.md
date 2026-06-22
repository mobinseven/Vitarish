---
name: platform-test-curator
description: Curate scoped required unit tests and environment-dependent hot test scenarios for features, artifacts, modules, and architecture changes. Use when identifying test coverage, runtime scenarios, failure/latency/DB/UI/network test needs, or test implementation tasks for an app/module capability.
---

# Platform Test Curator

## Purpose

Create and maintain test curation documents for platform features, artifacts, modules, and design objectives.

This skill decides what should be tested, why it matters, which tests are required, which tests are suggested, and which tests are intentionally deferred.

It does not blindly generate test code. It first analyzes the feature objective, journey, roadmap, ADRs, existing implementation, dependencies, runtime environment, data flow, previous defects, and failure risks. Then it proposes the smallest useful set of unit tests and hot test scenarios.

## Important Definition

`Hot test` does not mean a normal unit/integration test that can be fully proven by a single `dotnet test` command.

A hot test is a scenario that needs a real or simulated runtime condition to be meaningful.

Hot tests may require:

- a real database or containerized database
- seeded data and migrations
- browser/UI execution
- authentication/session state
- file storage
- message broker or background worker
- external API dependency
- network failure, timeout, latency, or bandwidth limitation
- concurrent users or race conditions
- deployment-like configuration
- observability/log verification
- manual or semi-automated execution steps

A hot test may still be automated, but the important output of this skill is the scenario design: environment, trigger, failure mode, expected behavior, observability, cleanup, and release relevance.

## Use When

Use this skill when the user asks to:

- suggest tests for a feature or artifact
- document required unit tests
- document required hot test scenarios
- analyze code and identify scenario-level test needs
- review test coverage gaps
- derive test tasks from a roadmap or implementation plan
- decide which tests are required before merge or release
- create a module test plan
- maintain test indexes
- classify tests by app/module scope

## Relationship to Other Skills

Use the User Journey Curator when test scenarios depend on user workflow stages.

Use the Feature Roadmap Curator when tests are derived from approved roadmap phases or delivery tickets.

Use the ADR Steward when a test protects an architectural decision or enforcement rule.

This skill connects tests back to:

- user journeys
- roadmaps
- implementation tasks
- ADRs
- defects/regressions
- risk areas
- app/module ownership
- runtime dependencies

## Scope-First Repository Contract

Before creating or updating a test plan, classify its owning scope:

```txt
App:
Module:
Capability:
Related apps:
Related modules:
```

App-owned test documents:

```txt
docs/apps/{app-slug}/modules/{module-slug}/tests/
```

Shared platform test documents:

```txt
docs/platform/modules/{module-slug}/tests/
```

Examples:

```txt
docs/apps/clinic-owner/modules/patient-procedures/tests/0001-patient-procedure-test-plan.md
docs/platform/modules/search/tests/0002-semantic-retrieval-test-plan.md
```

Each test plan has exactly one home folder. If it affects multiple apps/modules, place it under the primary owner and list affected scopes in metadata.

Use `references/scope-location-rules.md` for placement rules.

## Test Taxonomy

Use these test categories:

- `Unit`: deterministic tests for one behavior, rule, method, handler, state transition, mapper, validator, or pure domain/application logic.
- `Component`: UI/component tests for rendering, state, validation, events, and accessibility-relevant behavior without requiring full deployment.
- `Integration`: tests that verify components working together, such as API + DI + database + authorization + serialization.
- `Contract`: tests for public APIs, DTOs, events, provider abstractions, module boundaries, and external expectations.
- `Hot`: scenario tests that require a real or simulated runtime condition such as DB, browser, background worker, external service, latency, network failure, concurrency, or deployment-like configuration.
- `Smoke`: quick deployment/runtime checks that verify the app or service starts and critical endpoints/pages are reachable.
- `Regression`: tests added because a bug, incident, or risky behavior must not recur.

Prefer the smallest test level that proves the risk.

Do not convert every requirement into a hot test.

## Hot Test Scenario Definition

A hot test is required when code correctness depends on runtime behavior that cannot be trusted from isolated unit tests alone.

A hot test is usually required when the behavior involves:

- real database constraints, migrations, transactions, concurrency, locking, or generated values
- UI workflow state, browser behavior, navigation, file upload/download, camera/media, or auth redirects
- network calls, retries, timeouts, latency, outage, partial failure, or external provider behavior
- background jobs, indexing, queues, SignalR, streaming, or eventual consistency
- tenant isolation, authorization boundaries, permissions, or audit history
- destructive, irreversible, financial, clinical, security, or compliance-sensitive actions
- multi-step user journeys where the bug only appears across screens or requests
- operational resilience: restart, dependency failure, configuration mismatch, or degraded mode

Hot tests are not just “important tests”. They are environment-dependent scenarios.

## Unit Test Definition

A unit test is required when the code has a local rule that should be proven without infrastructure.

Unit tests are usually required for:

- domain rules
- validation and error handling
- state transitions
- mapping and normalization
- authorization policy decisions that can be isolated
- command/query handler branching
- retry/backoff decision rules without real network calls
- edge cases and regressions that do not require infrastructure

## Code Analysis Heuristics

When analyzing code, actively look for hot-test triggers:

- `DbContext`, SQL, migrations, transactions, concurrency tokens, unique indexes
- file system, blob storage, streams, upload/download, image/media processing
- `HttpClient`, external APIs, retries, circuit breakers, timeouts
- SignalR, WebSocket, streaming, background services, hosted workers
- authentication, cookies, tokens, OIDC, tenant resolution, role checks
- UI components with async loading, wizard flows, navigation, dialogs, validation, JS interop
- caching, search indexes, eventual consistency, queues, outbox/inbox
- time, scheduling, reminders, cron/background jobs
- deletion/cancel/commit actions, audit trail, irreversible state changes

For every trigger, decide whether a unit test is enough or whether a hot scenario is needed.

## Test Requirement Levels

Classify each proposed test as:

- `Required`: must exist before merge/release.
- `Suggested`: useful but not blocking.
- `Deferred`: valuable later but not needed now.
- `Rejected`: considered but intentionally not worth adding.

A test should be `Required` only when it protects a critical behavior, known risk, accepted ADR, security boundary, data rule, runtime failure mode, or regression-prone path.

## Discovery Questions

Ask only the questions needed for the current test plan.

Default questions:

1. What feature, artifact, or change are we testing?
2. Which app and module own it?
3. What code, PR, files, journey, roadmap, ADR, or bug triggered this test need?
4. What behavior must never break?
5. What runtime dependencies are involved: DB, UI, auth, file storage, network, workers, external services, search, queues, or SignalR?
6. What failure modes matter: latency, timeout, outage, partial write, duplicate request, concurrency, permission leak, stale index, or restart?
7. What should be required before merge/release?
8. What can be suggested or deferred?
9. What test framework/project conventions should be used?
10. Can hot scenarios use containers, browsers, local compose files, test environments, or manual scripts?

If the user asks for best effort, proceed with assumptions and mark them clearly.

## Test Plan Template

Use `assets/test-plan-template.md` for test plans.

Required metadata:

- Date
- Status
- App
- Module
- Capability
- Owner
- Related journey
- Related roadmap
- Related ADRs
- Related tasks
- Source code/files
- Confidence

Required sections:

- Objective
- Scope
- Code/Risk Analysis
- Required Unit Tests
- Suggested Unit Tests
- Required Hot Test Scenarios
- Suggested Hot Test Scenarios
- Deferred or Rejected Tests
- Test Task Derivation
- Existing Coverage / Gaps
- CI / Execution Guidance
- Traceability

## Hot Scenario Quality Rules

Every hot test scenario must explain:

- scenario name
- risk protected
- why unit tests are insufficient
- environment required
- data/setup required
- trigger/action
- fault or runtime condition, if any
- expected behavior
- observability evidence: logs, metrics, audit record, DB state, UI state, event, trace, or screenshot
- cleanup/reset
- automation level: manual, semi-automated, local automated, CI, nightly, pre-release, or chaos/lab
- requirement level
- source journey/roadmap/ADR/task/code when available

Do not call a test “hot” unless the scenario genuinely needs runtime conditions.

## Unit Test Quality Rules

Every unit test proposal must explain:

- behavior protected
- input/edge case
- expected result
- target class/function/handler/component
- reason it should be unit-level
- requirement level

Good unit tests are deterministic, readable, focused, and independent of infrastructure.

## Test Task Derivation Rules

When asked to generate implementation tickets/tasks, derive them from the approved or explicitly accepted test plan.

Each test task must include:

- source test plan section
- test category
- requirement level
- target test project or suite
- environment and fixtures
- acceptance criteria
- validation command or execution procedure
- dependencies and cleanup
- out-of-scope notes

Do not generate final test tasks from an unapproved test plan unless the user explicitly asks for provisional tasks.

## Output Modes

### Mode: Ask Clarifying Questions

Use when essential context is missing.

```md
## Test Curation Clarification

I need a little context before drafting the test plan:

1.
2.
3.
4.
5.
```

### Mode: Draft Test Plan

Use when enough information exists.

```md
## Proposed File

<scoped path>/tests/NNNN-title-test-plan.md

## Test Plan Draft

<test plan content>

## Index Update

<index row>

## Follow-up Items

<open questions or recommended next steps>
```

### Mode: Code Test Scenario Review

Use when the user provides code and asks what should be tested.

```md
## Test Scenario Review

Code area:

Risk triggers found:

Required unit tests:

Required hot test scenarios:

Suggested tests:

Deferred tests:

Recommended test tasks:
```

### Mode: Test Task Derivation

Use when turning an approved test plan into implementation tasks.

```md
## Test Task Derivation

Source test plan:

Required tasks:

Suggested tasks:

Hot scenario execution tasks:

Validation commands/procedures:

CI or lab requirements:
```

## Guardrails

Do not treat hot tests as ordinary `dotnet test` tests.

Do not require a hot scenario when a focused unit test proves the risk.

Do not propose brittle UI tests for logic that can be tested below the UI.

Do not propose infrastructure-heavy tests without naming the risk they protect.

Do not hide manual/lab requirements behind a fake automated command.

Do not invent existing test infrastructure.

If the scenario needs DB/UI/network/latency/failure injection, document the environment honestly.

If the test protects an architecture rule or ADR, link it.

## Style

Be concise.

Be concrete about scenario setup and expected evidence.

Use tables for test matrices.

Keep required tests small and high-signal.

Prefer traceability over volume.
