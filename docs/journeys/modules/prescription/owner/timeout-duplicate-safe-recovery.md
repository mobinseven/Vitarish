# Journey: Submission Timeout — Duplicate-Safe Recovery

## 1. Context
- **Persona:** Doctor (initial), Background Worker (automated), Support Operator (escalation)
- **Trigger:** Prescription submission call times out after outbound request was sent.
- **Goal:** Resolve final state without creating duplicate prescriptions.

## Current Implementation Alignment
- **Status:** `partial`.
- **Implemented now:** submission timeout/failure classification moves prescriptions to `SyncPending`, `ProviderUnavailable`, or `DuplicateRisk`; `PrescriptionOutboxService` stores retry work; `PrescriptionRetryProcessor` runs fetch-before-retry and escalates stale pending records; support commands can refresh provider status or resolve duplicate risk.
- **Current artifacts:** `SubmitPrescriptionCommand`, `PrescriptionRetryProcessor`, `PrescriptionOutboxService`, `RefreshProviderStatusCommand`, `ResolveDuplicateRiskCommand`, `GetReconciliationQueueQuery`, `GetOutboxStatusQuery`, `ReconciliationDesk`, `PrescriptionOperationalDashboard`.
- **Authoritative state:** `PrescriptionStatus` owns local lifecycle; outbox records own retry scheduling only.
- **Known gaps:** request hash is not exposed as a first-class support field in the current UI, doctor-facing pending notifications are limited to status display, and the reconciliation desk route/navigation is not confirmed.

## 2. Entry Conditions
1. Prescription was in `Submitting` state.
2. Outbound API call timed out (request may or may not have reached provider).
3. Idempotency key and request hash are recorded.
4. Correlation ID is available.

## 3. Journey Stages

### Stage 1: Timeout Detection (Doctor-facing)
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1.1 | Doctor confirmed submission (already in-flight) | Adapter call exceeds timeout threshold. |
| 1.2 | — | System moves state to `SyncPending`. Stores: request_hash, correlation_id, idempotency_key, timeout_timestamp. |
| 1.3 | — | Doctor sees: "Submission is being processed. We're verifying the result with insurance. You'll be notified when confirmed." |
| 1.4 | — | Submit button remains disabled. Status panel shows `SyncPending` with timestamp. |

### Stage 2: Automated Recovery (Background Worker)
| Step | User Action | System Response |
|------|-------------|-----------------|
| 2.1 | — (automated) | Recovery worker picks up `SyncPending` record after configurable delay. |
| 2.2 | — | Worker calls provider adapter `FetchPrescription` or status inquiry using correlation data. |
| 2.3a | — | **If remote submission found:** Reconcile local state to `AcceptedByProvider`. Attach tracking code and remote_prescription_id. Notify doctor. |
| 2.3b | — | **If remote submission NOT found:** Safe retry using same idempotency key. |
| 2.4a | — | **If retry succeeds:** State moves to `AcceptedByProvider`. |
| 2.4b | — | **If retry times out again:** Increment retry count. Re-schedule with backoff. |
| 2.5 | — | **If max retries exceeded:** State moves to `SyncFailed`. Support task created. |

### Stage 3: Support Escalation (if unresolved)
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3.1 | Support operator sees `SyncFailed` in reconciliation queue | Transaction details shown with correlation ID, timestamps, retry history. |
| 3.2 | Operator triggers manual status fetch | System queries provider for latest state. |
| 3.3 | Operator reconciles based on verified remote status | State updated to match verified outcome. |
| 3.4 | Operator marks case resolved | Resolution evidence recorded in audit trail. |

## 4. Exit Conditions
1. Prescription reaches a terminal or stable state (`AcceptedByProvider`, `RejectedByProvider`, or `SyncFailed` with support resolution).
2. No duplicate prescription exists at the provider.
3. Full recovery trail audited: each attempt, each response, final resolution.

## 5. Error and Edge States

| Error | UI Manifestation | System Behavior |
|-------|-----------------|-----------------|
| `DuplicateRisk` detected (remote has different tracking but same content) | Support queue shows duplicate-risk flag | Blocks further retry; requires manual reconciliation |
| Provider returns conflicting status across retries | Discrepancy flag on support queue | Escalated for manual review |
| Recovery worker itself fails | Internal alert; `SyncPending` record ages past threshold | Health monitor triggers alert |

## 6. Required Data for Recovery
- `correlation_id` — links all attempts for same prescription
- `request_hash` — deterministic hash of prescription content + provider_key + version
- `idempotency_key` — provider-facing deduplication key
- `retry_count` — number of recovery attempts
- `last_attempt_at` — timestamp of most recent recovery attempt
- `remote_status_snapshot` — last known provider response (if any)

## 7. Acceptance Criteria
1. Timeout never results in duplicate prescription at provider.
2. Doctor is informed of pending state without exposing technical details.
3. Automated recovery resolves majority of cases without human intervention.
4. Retry uses idempotency key so provider treats it as same-operation.
5. `SyncFailed` creates actionable support task with full correlation data.
6. Every state transition during recovery is auditable.
7. Configurable: timeout threshold, retry delay, max retries, escalation threshold.
