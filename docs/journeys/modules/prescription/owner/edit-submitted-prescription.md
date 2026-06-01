# Journey: Edit Submitted Prescription

## 1. Context
- **Persona:** Doctor
- **Trigger:** Doctor needs to amend a previously accepted/submitted prescription.
- **Goal:** Apply allowed changes through official provider edit path with version tracking.

## Current Implementation Alignment
- **Status:** `partial`.
- **Implemented now:** an accepted prescription exposes an edit action in `PrescriptionDetail`; `EditPrescriptionCommand` calls the provider adapter and updates canonical status/tracking.
- **Current artifacts:** `PrescriptionDetail`, `PrescriptionSubmitPanel`, `PrescriptionTimeline`, `EditPrescriptionCommand`, `IInsuranceProviderAdapter.EditPrescriptionAsync`.
- **Authoritative state:** the original `PrescriptionRecord` and its `VersionNumber` remain the local source of truth; provider edit response maps back to `PrescriptionStatus`.
- **Known gaps:** there is no dedicated edit workspace for changing items after acceptance, no diff/confirmation step, no edit reason capture, and no separate route for edit mode.

## 2. Entry Conditions
1. Prescription exists in editable state (`AcceptedByProvider` or provider-specific edit-eligible state).
2. Doctor has edit permission for this prescription.
3. Provider capability `SupportsEditPrescription` is true.
4. Edit time window has not expired (provider-specific rule).

## 3. Journey Stages

### Stage 1: Edit Eligibility Check
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1.1 | Opens existing prescription from patient history | System loads prescription with current lifecycle state. |
| 1.2 | Clicks "Edit" action | System checks: lifecycle state allows edit, provider supports edit, time window valid, doctor authorized. |
| 1.3 | — | If all checks pass: prescription enters edit mode. Otherwise: reason shown (e.g., "Edit window expired" or "Provider does not support edit"). |

### Stage 2: Draft Modification
| Step | User Action | System Response |
|------|-------------|-----------------|
| 2.1 | Modifies allowed fields/items | System enforces provider-specific edit constraints (which fields are mutable). |
| 2.2 | Adds/removes/changes items | Local validation runs inline. |
| 2.3 | Saves edit draft | Version number increments locally. State: `EditRequested`. |

### Stage 3: Edit Submission
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3.1 | Clicks "Submit Edit" | Re-validates full prescription against provider rules. |
| 3.2 | Confirms edit submission | State moves to `Submitting`. System calls provider adapter `EditPrescription`. |
| 3.3a | — | **On acceptance:** State moves to `Edited` → `AcceptedByProvider`. Version number updated. Timeline reflects change. |
| 3.3b | — | **On rejection:** State moves to `NeedsCorrection`. Rejection reason shown in clinician-safe language. |

## 4. Exit Conditions
1. Edited version stored with incremented version number.
2. Audit trail links original and edited states with diff metadata.
3. If rejected: `NeedsCorrection` state with actionable guidance.

## 5. Error and Edge States

| Error | UI Manifestation | System Behavior |
|-------|-----------------|-----------------|
| Edit window expired between opening and submitting | "Edit window has expired" message | Blocks edit submission; suggests cancel-and-resubmit if allowed |
| Provider does not support edit | Edit button hidden or disabled with tooltip | Capability check prevents action |
| Edit submission times out | Follows timeout-recovery journey | State: `SyncPending` for edit operation |
| Concurrent edit by another user | Optimistic concurrency error | "Prescription was modified. Please reload and retry." |

## 6. Acceptance Criteria
1. Edit action only visible when provider supports it and lifecycle allows it.
2. Version history preserved — original never overwritten.
3. Audit trail clearly shows before/after for each edit.
4. Provider-specific edit constraints enforced (not all fields editable).
5. Edit timeout follows same duplicate-safe recovery pattern as initial submission.
6. Doctor cannot edit prescriptions in terminal states (Cancelled, SyncFailed).
