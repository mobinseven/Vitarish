# Journey Map — Clinic Owner / Doctor: Social Security E-Prescription

## Journey Purpose
Define doctor and support journeys for creating, validating, submitting, tracking, editing, cancelling, and recovering Social Security e-prescriptions inside existing clinic workflows.

## Personas
1. **Doctor**: creates and submits prescriptions.
2. **Clinical Staff**: prepares drafts where policy allows.
3. **Clinic Owner/Admin**: configures reference sync, monitors failures.
4. **Support Operator**: resolves failed/retry/reconciliation cases.

---

## Journey 1 — Eligible Patient, Successful Submission

### Entry conditions
- Doctor is authenticated.
- Doctor has submission role.
- Reference data is fresh (or acceptable cached state).

### Steps
1. Open prescription screen from patient encounter.
2. Search/select patient by national ID.
3. Capture/confirm patient mobile.
4. Run eligibility check.
5. System shows Eligible + inquiry tracking info.
6. Add visit + drug/service items from reference selectors.
7. Save draft.
8. Run validation (local + remote prechecks).
9. Review warnings (if any) and confirm submit.
10. Submit to insurance.
11. System disables duplicate clicks while in-flight.
12. Show result panel with insurance tracking code and current lifecycle state.
13. Print/summary view available.

### Exit conditions
- Prescription lifecycle state is `Submitted` or `AcceptedByInsurance`.
- Audit + transaction records persisted.

---

## Journey 2 — Ineligible Patient (Blocked Flow)

### Steps
1. Doctor selects patient and runs entitlement check.
2. System receives `NotEligible` response.
3. UI shows understandable clinical/business message.
4. Submit button remains disabled for normal e-submission.
5. Doctor may proceed only via approved outage/override workflow (if policy allows).

### Exit conditions
- No normal insurance submission attempt made.
- Entitlement response and tracking code logged.

---

## Journey 3 — Timeout During Submission (Duplicate-Safe Recovery)

### Steps
1. Doctor confirms submit.
2. Adapter call times out after request is sent.
3. System marks local state `SyncPending` and stores request hash/correlation.
4. Background worker fetches remote status.
5. If remote submission exists: mark local as `Submitted/AcceptedByInsurance` and attach tracking code.
6. If remote submission absent: retry with idempotency key.
7. If repeated failure: move to `SyncFailed` and create support task.

### Exit conditions
- No duplicate prescription created.
- Operator has clear action path if unresolved.

---

## Journey 4 — Edit Submitted Prescription (Allowed Path)

### Steps
1. Doctor opens existing submitted prescription.
2. System verifies edit eligibility by state and insurance rules.
3. Doctor applies permitted edits.
4. System re-validates and sends `EditElectronicPrescription`.
5. Lifecycle transitions to `Edited` then refreshed remote state.
6. Audit trail links original and edited versions.

### Exit conditions
- Edit accepted and version incremented, or rejection reason shown with `NeedsCorrection`.

---

## Journey 5 — Cancel/Delete Submitted Prescription (Allowed Path)

### Steps
1. Doctor chooses cancel/delete action.
2. Confirmation dialog displays consequence and policy notes.
3. Adapter sends delete/cancel request.
4. Local state moves to `Cancelled` on success.
5. Future submissions blocked for same local prescription ID.

### Exit conditions
- Cancelled state persisted and visible in UI timeline.

---

## Journey 6 — Insurance Outage and Paper Fallback

### Entry conditions
- External outage or prolonged unavailability confirmed.

### Steps
1. Doctor triggers controlled fallback action.
2. System requires reason + paper tracking number.
3. Local prescription marked `PaperFallback`.
4. Audit event captures actor, reason, timestamp.
5. After recovery, support reconciles and optionally syncs final electronic status.

### Exit conditions
- Fallback remains fully auditable and reconcilable.

---

## Journey 7 — Admin Reference Data Operations

### Steps
1. Admin views reference freshness dashboard.
2. Scheduled sync failure alerts after threshold.
3. Admin triggers manual sync.
4. New version imported; old codes inactivated, not deleted.
5. Doctors immediately see updated active code set for new drafts.

### Exit conditions
- Freshness timestamp updated.
- Sync history retained for compliance.

---

## Journey 8 — Support Reconciliation Desk

### Steps
1. Operator filters failed transactions by error class.
2. Opens transaction details (masked payload metadata).
3. Runs safe retry or fetch-latest-status action.
4. Updates local prescription state based on verified remote response.
5. Escalates recurring credential/outage incidents.

### Exit conditions
- Failed cases resolved or escalated with evidence.

---

## UI State Model (Doctor Screen)

Mandatory panels:
- Patient identity panel
- Eligibility status panel
- Mobile capture field
- Reference data search/selectors
- Prescription item grid
- Warnings and validation panel
- Submission result panel
- Tracking code + lifecycle timeline

State-safe interaction rules:
- Disable submit when `Submitting`.
- Hide technical adapter errors; show clinician-safe wording.
- Require explicit confirmation before final submit.
- Render actions based on lifecycle state + permissions.

---

## Journey Acceptance Matrix

1. Doctors can complete full prescription flow without leaving encounter context.
2. Ineligible paths are clearly blocked and explained.
3. Timeout/outage paths are duplicate-safe and auditable.
4. Edit/cancel actions respect official policy and lifecycle guards.
5. Admin/support roles can maintain data freshness and recover failures.
