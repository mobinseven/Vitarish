# Support Reconciliation Journey

## 1. Context
- Persona: Support operator
- Trigger: Transaction in `SyncFailed`, repeated timeout, or incident escalation.
- Goal: Reconcile local and remote state safely.

## 2. Journey Flow
1. Operator filters failed transactions by error class.
2. Operator reviews transaction metadata (masked).
3. Operator chooses safe action:
   - Retry operation when duplicate-safe.
   - Fetch latest remote status.
   - Escalate credential/outage/data incidents.
4. Operator updates local state based on verified remote response.
5. Case is closed with resolution evidence.

## 3. Exit Conditions
1. Prescription local state matches verified remote outcome, or escalation is recorded.
2. Audit and support logs capture action history.

## 4. Guardrails
- Support tooling must expose correlation IDs and operation names.
- Operator actions must preserve idempotency safeguards.
