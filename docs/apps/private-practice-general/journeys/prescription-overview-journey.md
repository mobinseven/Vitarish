# Social Security E-Prescription Journey Overview

## 1. Context
- Product / service: Vitarish Clinic app (owner and doctor experiences) + Insurance E-Prescription integration module
- Journey type: Target-state implementation journey set
- Scenario: A doctor creates, validates, submits, tracks, edits, and cancels Social Security e-prescriptions inside existing encounter workflows.
- User goal: Complete e-prescribing safely without duplicate entry.
- Business goal: Ensure compliant, auditable, duplicate-safe integration with official insurance APIs.
- Scope start: Doctor opens prescription workspace from patient encounter.
- Scope end: Prescription reaches terminal/operational state (accepted, rejected-needs-correction, cancelled, fallback, or reconciled).
- Primary personas: Doctor, clinical staff (draft-prep), clinic admin, support operator
- Confidence level: Planning baseline

## 2. Journey Set Map

### Core doctor journeys
1. [Eligible Patient — Successful Submission Journey](./prescription-submit-journey.md)
2. [Ineligible Patient — Blocked Submission Journey](./prescription-ineligible-journey.md)
3. [Edit Submitted Prescription Journey](./prescription-edit-journey.md)
4. [Cancel/Delete Submitted Prescription Journey](./prescription-cancel-journey.md)

### Recovery and operations journeys
5. [Submission Timeout and Duplicate-Safe Recovery Journey](./prescription-timeout-recovery-journey.md)
6. [Insurance Outage and Paper Fallback Journey](./prescription-fallback-journey.md)
7. [Reference Data Sync Journey (Admin)](./prescription-reference-sync-journey.md)
8. [Support Reconciliation Journey](./prescription-support-reconciliation-journey.md)

## 3. Shared State and Guardrails

### Common lifecycle states
- Draft
- ReadyForSubmission
- Submitting
- Submitted
- AcceptedByInsurance
- RejectedByInsurance
- NeedsCorrection
- Edited
- Cancelled
- PaperFallback
- SyncPending
- SyncFailed

### Shared rules
1. Eligibility gate blocks normal submission when ineligible.
2. Submit action is disabled while in-flight (`Submitting`).
3. Idempotency and correlation IDs are required for all submission-class operations.
4. Every state transition must be auditable.
5. Technical API errors are translated to clinician-safe UI messaging.

## 4. UI Composition Baseline

Mandatory surfaces in the doctor screen:
- Patient identity panel
- Eligibility panel
- Mobile capture field
- Drug/service selectors from synchronized references
- Prescription items list
- Validation/warnings panel
- Submission result panel
- Tracking code and lifecycle timeline

## 5. Cross-Journey Acceptance Outcomes

1. Doctors can complete prescribing without duplicate re-entry.
2. Ineligible/denied workflows are clearly blocked and explained.
3. Timeout/outage paths remain duplicate-safe and traceable.
4. Edit/cancel actions are policy-controlled and lifecycle-safe.
5. Admin/support journeys can keep reference data fresh and reconcile failures.
