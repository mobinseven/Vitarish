# Journey: Cancel Prescription

## 1. Context
- **Persona:** Doctor
- **Trigger:** Doctor needs to cancel/delete an accepted or submitted prescription.
- **Goal:** Execute policy-compliant cancellation with clear terminal state.

## 2. Entry Conditions
1. Prescription exists in cancellable state (`AcceptedByProvider`, `Submitted`, or provider-specific cancel-eligible state).
2. Doctor has cancellation permission.
3. Provider capability `SupportsCancelPrescription` is true.
4. Cancellation time window valid (provider-specific).

## 3. Journey Stages

### Stage 1: Cancel Eligibility Check
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1.1 | Opens existing prescription | System shows current state and available actions. |
| 1.2 | Clicks "Cancel" action | System checks: lifecycle state allows cancel, provider supports cancel, time window valid, doctor authorized. |
| 1.3 | — | If checks pass: confirmation dialog opens. Otherwise: reason shown. |

### Stage 2: Cancellation Confirmation
| Step | User Action | System Response |
|------|-------------|-----------------|
| 2.1 | — | Confirmation dialog shows: consequences, policy notes, tracking code of prescription to cancel. |
| 2.2 | Doctor optionally enters cancellation reason | Reason captured for audit trail. |
| 2.3 | Confirms cancellation | State moves to `CancelRequested` → `Submitting`. |

### Stage 3: Provider Cancellation
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3.1 | — | System calls provider adapter `CancelPrescription` with remote_prescription_id. |
| 3.2a | — | **On success:** State moves to `Cancelled`. Timeline updated. |
| 3.2b | — | **On rejection:** Provider denies cancel (e.g., already dispensed). Reason shown to doctor. State reverts to pre-cancel. |
| 3.2c | — | **On timeout:** Follows timeout-recovery pattern for cancel operation. |

## 4. Exit Conditions
1. `Cancelled` state persisted (on success).
2. Cancellation reason, actor, timestamp recorded in audit.
3. Same local prescription_id cannot be reused for new submission.
4. Cancelled prescription remains visible in history (never deleted from view).

## 5. Error and Edge States

| Error | UI Manifestation | System Behavior |
|-------|-----------------|-----------------|
| Provider rejects cancel (already dispensed) | "This prescription cannot be cancelled: [reason]" | State unchanged; action logged |
| Cancel time window expired | "Cancellation window has expired" | Cancel button disabled with explanation |
| Provider does not support cancel | Cancel button hidden | Capability check prevents action |
| Cancel timeout | "Cancellation is being verified" | Follows timeout-recovery for cancel |

## 6. Acceptance Criteria
1. Cancel action only available when provider supports it and lifecycle allows it.
2. Confirmation dialog prevents accidental cancellation.
3. Cancelled prescriptions remain in history with clear terminal state badge.
4. Re-submission using same prescription_id is blocked post-cancel.
5. Cancellation reason is part of audit record.
6. Cancel timeout follows duplicate-safe recovery pattern.
7. Provider-specific cancel rules (time windows, dispensing checks) are enforced.
