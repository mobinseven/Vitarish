# Cancel/Delete Submitted Prescription Journey

## 1. Context
- Persona: Doctor
- Trigger: Doctor needs to cancel/delete an already submitted prescription where allowed.
- Goal: Execute policy-compliant cancellation with clear terminal state.

## 2. Entry Conditions
1. Prescription is in cancellable/deletable state.
2. User has permission.

## 3. Journey Flow
1. Doctor chooses cancel/delete action.
2. Confirmation dialog explains impact.
3. Adapter sends delete/cancel request to insurance API.
4. On success, lifecycle moves to `Cancelled`.
5. UI timeline updates and prevents reuse of same local prescription ID for resubmission.

## 4. Exit Conditions
1. `Cancelled` state persisted.
2. Cancellation action and response metadata audited.

## 5. Guardrails
- Prevent repeated cancel attempts on already terminal records.
- Never silently remove cancelled prescriptions from history.
