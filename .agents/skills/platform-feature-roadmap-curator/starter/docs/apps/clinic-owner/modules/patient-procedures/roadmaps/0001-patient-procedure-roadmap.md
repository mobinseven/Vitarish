# Roadmap: Patient Procedure Tracking

Date: 2026-06-22
Status: Draft
App: Clinic Owner
Module: Patient Procedures
Capability: Procedure history tracking
Owner: Product / Clinic
Related journey: Patient Procedure Journey
Related ADRs:
Related modules: Vitarish Clinic Owner App, Sea Procedures
Confidence: Medium

## 1. Objective

Make patient procedures trackable as clinical work, not only as generic Sea procedure records.

## 2. Background

The current journey shows that clinicians can add procedures, open procedure details, commit event steps, and record notes. The major gaps are provider assignment, explicit procedure status, and cancellation/close workflow.

## 3. Target Users / Personas

Provider admin / clinician.

## 4. Success Criteria

- Clinician can see procedure owner/provider at a glance.
- Clinician can distinguish active, completed, and cancelled procedures.
- Cancelled work is preserved with reason and timestamp instead of deleted.
- Event history remains auditable and separate from procedure status.

## 5. Scope

### In Scope

- Procedure metadata wrapper for clinical fields.
- Status display in procedure list and details.
- Provider assignment display/editing.
- Cancel/close workflow.
- Derived implementation tickets after approval.

### Out of Scope

- Full scheduling engine.
- Room/resource optimization.
- Billing integration.

## 6. Roadmap Summary

### MVP / First Useful Version

Model and display explicit status and provider assignment for patient procedures.

### Short-Term

Add cancel/close workflow with reason and timestamp.

### Mid-Term

Add next-step and overdue-risk indicators.

### Long-Term

Introduce clinical procedure planning and scheduling capabilities.

## 7. Phased Delivery Plan

### Phase 1: Clinical Procedure Metadata

Goal: Add structured clinical fields without rewriting Sea procedure core.

User value: Clinicians can understand ownership and state at a glance.

Business value: Procedure history becomes safer and more reliable.

Required artifacts: DTO extension or metadata wrapper, UI fields, validation.

Required features: provider assignment, planned date, status.

Technical work: metadata mapping, read/write service changes, UI rendering.

Dependencies: Sea procedure metadata support.

Risks: Leaking Sea role semantics into clinical scheduling.

Acceptance criteria:

- Procedure grid shows provider and status.
- Procedure details show editable clinical fields.
- Event commits remain separate from procedure status.

### Phase 2: Cancel / Close Workflow

Goal: Replace destructive delete behavior with a clinical close/cancel action.

User value: Stopped work remains explainable.

Business value: Clinical record integrity improves.

Required artifacts: cancel dialog, cancel reason field, closed timestamp, audit note.

Required features: cancel/close procedure, display cancellation state.

Technical work: workflow transition handling, validation, UI actions.

Dependencies: Phase 1 status model.

Risks: Existing delete behavior may be relied on for cleanup.

Acceptance criteria:

- Cancel action requires reason.
- Cancelled procedure remains visible as historical record.
- Delete is no longer the main clinical stop action.

## 8. Artifact Map

| Artifact | Type | Purpose | Owner | Phase | Source |
|---|---|---|---|---|---|
| ClinicalProcedureMetadata | Data model | Store clinical fields | Medical | MVP | Journey gaps |
| ProcedureGrid status columns | UI | Show status/provider | Clinic UI | MVP | Stage 1 |
| CancelProcedureDialog | UI | Capture cancel reason | Clinic UI | Short-Term | Stage 2 |

## 9. Feature Map

| Feature | User Problem | Journey Stage | Phase | Priority | Notes |
|---|---|---|---|---|---|
| Provider assignment | No ownership visible | Add/open procedure | MVP | High | Medical abstraction, not Sea role |
| Procedure status | State inferred from events | Record history | MVP | High | Separate from event commits |
| Cancel workflow | Work stoppage not modeled | Record history | Short-Term | High | Preserve audit trail |

## 10. Dependencies

- Sea procedure metadata extensibility.
- Clinic UI permission rules.
- Possible ADR for medical wrapper over generic Sea procedure.

## 11. Risks and Open Questions

- Should provider assignment support multiple providers?
- Should completion be manual, inferred, or both?
- Should cancellation be reversible?

## 12. Implementation Notes

Avoid changing Sea procedure core if metadata extension or medical wrapper is enough.

## 13. Traceability

- Stage 1: Add/open procedure -> provider/status display.
- Stage 2: Record history -> status, cancel reason, workflow state.
