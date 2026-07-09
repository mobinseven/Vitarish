# Submission Timeout and Duplicate-Safe Recovery Journey

## 1. Context
- Persona: Doctor + background recovery worker + support operator
- Trigger: Submission timeout after outbound request attempt.
- Goal: Resolve final state without creating duplicates.

## 2. Journey Flow
1. Doctor confirms submit.
2. Adapter call times out.
3. Local state moves to `SyncPending`; request hash/correlation recorded.
4. Recovery worker queries remote status.
5. If remote submission exists: local state reconciles to `Submitted`/`AcceptedByInsurance` with tracking code.
6. If remote submission absent: safe retry executes using idempotency key.
7. If unresolved after retry policy: state moves to `SyncFailed` and support task is created.

## 3. Exit Conditions
1. Duplicate prescription is prevented.
2. Final local state is reconciled or explicitly escalated.
3. Support has traceable metadata to continue recovery.

## 4. Required Data
- Correlation ID
- Request hash
- Operation timestamp window
- Retry attempt history

## 5. Guardrails
- Never blind-retry without duplicate-risk check.
- Maintain auditable transitions between `Submitting`, `SyncPending`, `SyncFailed`, and recovered states.
