# Journey Fit Review

Classification: Partially Fits

## Covered journey needs

- The user can open a patient and access the procedures grid.
- The user can create or open a procedure.
- The user can record procedure events and timeline notes.

## Missing journey needs

- Provider assignment is not explicit.
- Procedure status is inferred rather than modeled.
- Cancellation is handled as delete instead of close/cancel with reason.

## Implementation gaps

- Add medical procedure metadata for assigned provider, planned date, status, cancel reason, and clinical owner.
- Show provider and status in the procedure list.
- Replace destructive delete in the clinical journey with cancel/close action.
- Keep event commits as auditable step facts, separate from procedure workflow state.

## Recommended next slice

Add explicit procedure status and provider assignment metadata, then surface both in `ProceduresGrid`.

## Risks

- Continuing to infer procedure state from event commits makes cancelled or paused work ambiguous.
- Using Sea event roles as provider assignment leaks infrastructure semantics into clinical scheduling.
