# Insurance Outage and Paper Fallback Journey

## 1. Context
- Persona: Doctor + support
- Trigger: External insurance service outage or prolonged unavailability.
- Goal: Keep care operations moving with controlled paper fallback.

## 2. Entry Conditions
1. Outage condition verified by workflow/policy.
2. User authorized for fallback action.

## 3. Journey Flow
1. Doctor triggers fallback action.
2. System requires fallback reason and paper tracking number.
3. Local state moves to `PaperFallback`.
4. Audit event captures actor/time/reason.
5. After service recovery, support reconciles records and marks later sync outcome.

## 4. Exit Conditions
1. Paper fallback remains auditable.
2. Reconciliation queue has sufficient metadata for post-outage alignment.

## 5. Guardrails
- Fallback is controlled, explicit, and non-default.
- Prevent loss of traceability between paper and later electronic reconciliation.
