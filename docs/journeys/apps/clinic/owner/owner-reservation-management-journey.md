# Clinic Owner Reservation Management Journey

## 1. Context
- Product / service: Vitarish Clinic owner app reservation module
- Journey type: Target state
- Scenario: Clinic owner updates reservation lifecycle safely.
- User goal: Create, confirm, reschedule, cancel, and complete reservations with auditability.
- Business goal: Standardize reservation operations and reduce errors.
- Scope start: Owner opens reservation detail or starts new reservation.
- Scope end: Lifecycle action is completed and audit log is updated.
- Primary persona: Clinic owner / operations manager

## 2. Persona Snapshot
**Name / segment:** Operations owner

**Situation:** Handling exceptions, changes, and day-of-visit flows.

**Motivation:** Keep schedules accurate and patient communication reliable.

**Key needs:** Safe transitions, validation, reasons, and audit history.

**Success means:** Every state change is valid, visible, and reversible when allowed.

## 3. Journey Summary
**Narrative:** The owner creates or opens a reservation, verifies availability and constraints, performs a lifecycle action (confirm, reschedule, cancel, mark present/completed/no-show), records reasons when required, and triggers notifications automatically.

**Biggest friction point:** Inconsistent handling of exceptions (reschedule/cancel reasons).

**Highest-value opportunity:** Explicit command workflows separated from read-only detail.

## 4. Journey Stages

### Stage 1: Create or open reservation
**User goal:** Start with accurate reservation context.

**User actions:** Launch new reservation flow or open existing reservation detail.

**Needs:** Eligibility checks, capacity checks, duplicate/conflict warnings.

### Stage 2: Execute lifecycle action
**User goal:** Perform intended state change correctly.

**User actions:** Confirm, reschedule, cancel, reject, mark present, complete, or mark no-show.

**Needs:** State-machine guardrails and required reason capture for cancel/reject/no-show.

### Stage 3: Notify and audit
**User goal:** Ensure downstream coordination.

**User actions:** Review action result, notification status, and audit trail entry.

**Needs:** Immutable event timeline and operator identity.

## 5. Functional capabilities this module shall provide
1. Full lifecycle commands: create, confirm, reschedule, cancel, reject, present, complete, no-show.
2. Conflict and policy validation before commit (capacity, windows, double-booking).
3. Required reason capture on sensitive transitions.
4. Automatic patient/staff notifications per action.
5. Auditable timeline with before/after state and actor metadata.

