# Journey: Support Reconciliation

## 1. Context
- **Persona:** Support Operator
- **Trigger:** Prescription transactions in `SyncFailed`, `DuplicateRisk`, repeated timeout, or incident escalation.
- **Goal:** Reconcile local and remote prescription state safely with full audit trail.

## 2. Entry Conditions
1. Support operator is authenticated with reconciliation permission.
2. One or more prescriptions are in recoverable/escalated states.

## 3. Journey Stages

### Stage 1: Queue Review
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1.1 | Opens reconciliation dashboard | Queue shows items grouped by: error category, provider, age, priority. |
| 1.2 | Filters by: provider, error class, date range, doctor, patient | Filtered view updates with matching records. |
| 1.3 | — | Each item shows: prescription_id, provider_key, operation, current state, error category, retry count, last attempt, correlation_id. |

### Stage 2: Transaction Investigation
| Step | User Action | System Response |
|------|-------------|-----------------|
| 2.1 | Opens specific transaction | Detail view shows: full lifecycle timeline, API transaction history (masked sensitive fields), correlation data, retry history. |
| 2.2 | Views provider-native response (if available) | Raw provider status/code shown alongside canonical mapping for comparison. |
| 2.3 | Checks current remote status | "Fetch Remote Status" action calls provider adapter to get latest state. |

### Stage 3: Resolution Actions
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3.1 | **Safe Retry:** Operator triggers retry | System validates: idempotency key still valid, duplicate-risk check passes, then retries via adapter. |
| 3.2 | **Manual Reconcile:** Operator sets local state to match verified remote outcome | System validates transition is legal; updates state with operator justification. |
| 3.3 | **Escalate:** Operator marks for higher-level review | Creates escalation record with context summary; removes from primary queue. |
| 3.4 | **Close as Resolved:** Operator confirms resolution | Resolution evidence captured: action taken, verified outcome, operator notes. |

### Stage 4: Audit and Closure
| Step | User Action | System Response |
|------|-------------|-----------------|
| 4.1 | — | Every action (retry, reconcile, escalate, close) generates audit event with: operator_id, action, old_state, new_state, justification, timestamp. |
| 4.2 | — | Resolved items move to history; remain searchable for compliance. |

## 4. Exit Conditions
1. Prescription local state matches verified remote outcome.
2. Or: escalation is recorded with sufficient context for next-level review.
3. Full audit trail captures every support action and its outcome.

## 5. Error and Edge States

| Error | UI Manifestation | System Behavior |
|-------|-----------------|-----------------|
| Safe retry creates a duplicate (should be impossible with idempotency) | `DuplicateRisk` state flagged | Blocks further automated action; requires manual investigation |
| Provider returns conflicting information across queries | Discrepancy flag on transaction | Operator must manually verify and reconcile |
| Operator attempts illegal state transition | "Transition not allowed: [reason]" | Action blocked; domain guards enforced |
| Credential expired during support action | "Provider authentication failed — credential refresh needed" | Directs to credential management |

## 6. Acceptance Criteria
1. Support operators can filter and find failed transactions efficiently.
2. Sensitive patient/medical data is masked in support views (national ID, items partially masked).
3. Safe retry respects idempotency — never blind retry.
4. Manual reconciliation requires explicit justification.
5. All support actions are audited with same rigor as doctor actions.
6. Escalation path exists for cases beyond operator resolution.
7. Resolved cases remain searchable for compliance audits.
