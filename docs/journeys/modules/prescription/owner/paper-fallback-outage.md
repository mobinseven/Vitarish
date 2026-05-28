# Journey: Paper Fallback — Provider Outage

## 1. Context
- **Persona:** Doctor (initiates), Clinic Admin (policy), Support Operator (reconciles)
- **Trigger:** Insurance provider is unavailable (outage, degraded, prolonged timeout) and clinical care cannot wait.
- **Goal:** Continue prescription operations via controlled paper fallback with full audit trail and post-recovery reconciliation path.

## 2. Entry Conditions
1. Provider is confirmed unavailable or degraded (repeated timeouts, known outage, admin declaration).
2. User is authorized for fallback action.
3. Clinical need justifies bypassing electronic submission.

## 3. Journey Stages

### Stage 1: Outage Recognition
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1.1 | Doctor encounters repeated failures or sees provider status indicator | System shows provider operational status: online/degraded/offline. |
| 1.2 | Doctor decides to use paper fallback | "Paper Fallback" action becomes available when provider status is degraded/offline or after N failed attempts. |

### Stage 2: Fallback Initiation
| Step | User Action | System Response |
|------|-------------|-----------------|
| 2.1 | Clicks "Paper Fallback" action | Dialog requires: fallback reason (selection + optional free text), paper tracking number/barcode. |
| 2.2 | Enters reason and paper tracking reference | System validates paper tracking format (if format is known). |
| 2.3 | Confirms fallback | Prescription state moves to `PaperFallback`. Audit event captures: actor, reason, paper_tracking_number, timestamp. |
| 2.4 | — | Prescription visible in history with `PaperFallback` badge. Doctor can continue patient care. |

### Stage 3: Post-Recovery Reconciliation (Support)
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3.1 | Provider comes back online | System notifies support of pending `PaperFallback` records that may be reconcilable. |
| 3.2 | Support operator reviews fallback prescriptions | Reconciliation queue shows paper-fallback records with reason, paper tracking, original draft data. |
| 3.3 | Operator decides: submit electronically now OR mark as permanently paper-only | System supports either path. |
| 3.4a | If submitting electronically: operator triggers submission using original draft | Normal submission flow with existing idempotency handling. On success: state moves from `PaperFallback` → `AcceptedByProvider`. |
| 3.4b | If marking as paper-only: operator confirms final status | State remains `PaperFallback` with reconciliation note: "Confirmed paper-only — [reason]". |
| 3.5 | — | Reconciliation audit captures resolution decision, actor, timestamp. |

## 4. Exit Conditions
1. Paper fallback is fully auditable: who, when, why, paper tracking number.
2. Doctor's clinical workflow is unblocked.
3. Reconciliation path exists for post-recovery electronic submission.
4. No prescription is "lost" — all fallbacks appear in history and reconciliation queues.

## 5. Error and Edge States

| Error | UI Manifestation | System Behavior |
|-------|-----------------|-----------------|
| Doctor tries fallback when provider is actually online | Fallback action only enabled when provider status is degraded/offline or repeated failures detected | Policy prevents fallback when provider is clearly available |
| Paper tracking number format invalid | Inline validation error on dialog | Submission blocked until corrected |
| Post-recovery electronic submission fails | Follows normal submission error handling | Reconciliation queue retains record for further action |

## 6. Acceptance Criteria
1. Fallback is controlled and explicit — never the default path.
2. Fallback requires reason and paper tracking reference.
3. Every fallback is audited with full context.
4. Reconciliation queue shows all unresolved fallback records.
5. Support can convert paper-fallback to electronic submission after recovery.
6. Paper-fallback prescriptions remain permanently visible in patient history.
7. Policy can restrict which roles/conditions allow fallback initiation.
