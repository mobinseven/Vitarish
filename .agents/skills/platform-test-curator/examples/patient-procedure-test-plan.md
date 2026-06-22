# Test Plan: Patient Procedure Tracking

Date: 2026-06-22
Status: Draft
App: clinic-owner
Module: patient-procedures
Capability: procedure tracking
Owner: Product / Engineering
Related journey: patient-procedure-journey
Related roadmap: patient-procedure-roadmap
Related ADRs:
Related tasks:
Source code/files: ProcedureDetails, ProceduresGrid, PatientProcedurePage, EventDetails, EventItemsTimeline
Confidence: Medium

## 1. Objective

Protect the clinical procedure workflow where an admin opens a patient, creates or selects a procedure, records event-level progress, and preserves history.

## 2. Scope

### In Scope

Procedure creation, procedure list state, event commit/uncommit behavior, timeline notes, cancellation/status/provider gaps.

### Out of Scope

Full clinical scheduling and billing.

## 3. Code / Risk Analysis

| Code area | Risk trigger | Why it matters | Test response |
|---|---|---|---|
| ProcedureCreateDialog | copies event chains and attachments | clone errors corrupt patient procedure history | integration + hot DB scenario |
| EventDetails commit/uncommit | auditable clinical action | missing note or wrong state hides what happened | unit + hot UI scenario |
| ProceduresGrid | selection/status visibility | wrong procedure selected or active/cancelled state unclear | component + hot UI scenario |
| DeleteProcedure | destructive action | clinical work should be cancelled/closed, not silently removed | required scenario before replacement |

## 4. Required Unit Tests

| Test | Behavior protected | Target | Input / edge case | Expected result | Requirement reason |
|---|---|---|---|---|---|
| Procedure status transition rejects invalid transition | workflow safety | procedure status service/domain rule | Cancelled -> Active | rejected with safe error | protects explicit status model |
| Cancel reason is required for cancellation | audit quality | validator/handler | missing reason | validation failure | protects clinical traceability |
| Provider assignment is medical abstraction | module boundary | mapper/handler | Sea role value supplied | does not leak role semantics | protects module abstraction |

## 5. Suggested Unit Tests

| Test | Behavior protected | Target | Notes |
|---|---|---|---|
| Procedure sort orders unread/new first | list usability | grid query helper | useful if logic exists outside UI |

## 6. Required Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Trigger | Expected evidence | Execution profile |
|---|---|---|---|---|---|
| Create procedure from template with real DB | template clone integrity | DB, migrations, seeded template | create patient procedure from template | procedure, events, role assignments, attachments all created consistently | local automated / CI with containers |
| Commit event then verify timeline evidence | audit history | UI/browser, auth, DB | clinician commits event step | UI shows committed state, timeline note exists, DB state matches | pre-release browser scenario |
| Cancel procedure instead of delete | clinical history preservation | UI/browser, DB | user cancels active procedure with reason | status Cancelled, reason/timestamp preserved, procedure remains visible | pre-release browser scenario |
| Concurrent event update conflict | data integrity | DB concurrency, two sessions | two users update same event/procedure | one succeeds, conflict is handled safely, no silent overwrite | lab / semi-automated |

## 7. Suggested Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Reason suggested |
|---|---|---|---|
| Slow DB during procedure list load | UI resilience | latency injection | verifies loading and retry/error UX |

## 8. Deferred or Rejected Tests

| Test/scenario | Decision | Reason |
|---|---|---|
| Full end-to-end scheduling + billing | Deferred | outside current capability |

## 9. Test Task Derivation

| Task | Source | Type | Requirement | Target suite | Notes |
|---|---|---|---|---|---|
| Add procedure workflow unit tests | Required Unit Tests | Unit | Required | Medical.Application.Tests | domain/handler tests |
| Add template clone DB scenario | Required Hot Test Scenarios | Hot | Required | HotTests/ProcedureScenarios | needs seeded DB |
| Add event commit UI scenario | Required Hot Test Scenarios | Hot | Required | Playwright/Lab | needs auth user |

## 10. Existing Coverage / Gaps

Unknown.

## 11. CI / Execution Guidance

| Suite | Execution mode | Command/procedure | Required environment |
|---|---|---|---|
| Unit | CI | dotnet test | none |
| DB hot scenario | CI or nightly | scenario runner with database container | SQL Server/Postgres container |
| UI hot scenario | pre-release | browser script/manual checklist | running app, seeded user, real DB |

## 12. Traceability

Derived from the patient procedure journey and its issues: missing provider assignment, explicit status, and cancel workflow.
