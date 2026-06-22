# Implementation Tasks: Patient Procedure Tracking

Source roadmap: 0003-patient-procedure-roadmap.md
Roadmap status: Approved
Generated date: 2026-06-22
Execution status: Ready

## Task Summary

| ID | Title | Type | Phase | Priority | Depends On | Source |
|---|---|---|---|---|---|---|
| PPT-001 | Add clinical procedure metadata model | Data | MVP | High | — | Phase 1 |
| PPT-002 | Show provider and status in ProceduresGrid | UI | MVP | High | PPT-001 | Stage 1 |
| PPT-003 | Add procedure cancel/close workflow | Feature | Short-Term | High | PPT-001 | Phase 2 |

## Tickets

### PPT-001: Add clinical procedure metadata model

Type: Data
Phase: MVP
Priority: High
Depends on: —
Source roadmap section: Phase 1
Source journey stage: Stage 1 / Stage 2
Related ADR: Possible

#### Why

Generic Sea procedure records do not expose medical-specific provider, planned date, status, cancellation reason, or clinical owner fields.

#### Scope

Add a medical metadata wrapper or extension for patient procedure clinical fields.

#### Implementation Notes

Prefer extension metadata over rewriting Sea procedure components.

#### Acceptance Criteria

- [ ] Provider assignment can be stored and read.
- [ ] Explicit procedure status can be stored and read.
- [ ] Event commit history remains independent from procedure status.

#### Validation

- [ ] Unit tests cover metadata serialization and defaults.
- [ ] Existing procedure creation still works.

#### Out of Scope

Scheduling engine and billing integration.

### PPT-002: Show provider and status in ProceduresGrid

Type: UI
Phase: MVP
Priority: High
Depends on: PPT-001
Source roadmap section: Phase 1
Source journey stage: Stage 1
Related ADR:

#### Why

Clinicians need ownership and status at a glance when selecting a procedure.

#### Scope

Update procedure list display to include provider and explicit status.

#### Implementation Notes

Keep clinical display logic outside generic Sea semantics when possible.

#### Acceptance Criteria

- [ ] Procedure grid shows assigned provider.
- [ ] Procedure grid shows explicit status.
- [ ] Empty provider/status states are understandable.

#### Validation

- [ ] UI renders old records without metadata.
- [ ] Selection flow still navigates correctly.

#### Out of Scope

Overdue risk indicator.

### PPT-003: Add procedure cancel/close workflow

Type: Feature
Phase: Short-Term
Priority: High
Depends on: PPT-001
Source roadmap section: Phase 2
Source journey stage: Stage 2
Related ADR:

#### Why

Stopping clinical work should preserve a reason and timestamp instead of permanently deleting procedure history.

#### Scope

Add cancel/close action, reason capture, timestamp, and visible cancelled state.

#### Implementation Notes

Keep delete as administrative cleanup if still required, but do not use it as the primary clinical stop action.

#### Acceptance Criteria

- [ ] Cancel action requires reason.
- [ ] Cancelled procedure remains visible.
- [ ] Cancellation timestamp is recorded.
- [ ] Existing event history remains readable.

#### Validation

- [ ] Tests cover invalid transitions.
- [ ] UI confirms cancellation state.

#### Out of Scope

Full reversible workflow unless separately approved.
