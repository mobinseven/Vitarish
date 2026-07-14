# Patient Procedure Clinical Workspace Decision Tree

Status: Future state  
Owner: Private Practice General app  
Scope: Patient procedure workspace branching and recovery  
Primary persona: Clinician/provider  
Secondary persona: Assistant, subject to the same SEA permissions  
Last reviewed: 2026-07-13

## Entry and access

1. User opens a patient procedure route or its existing event deep link.
2. Is access authorized by the backend?
   - **No** -> Preserve safe app navigation and show the standard access-denied outcome.
   - **Yes** -> Load patient identity, procedure detail, and ordered SEA events.
3. Is the procedure query pending?
   - **Yes** -> Show shape-matched identity, summary, and timeline skeletons.
   - **No** -> Continue.
4. Was the procedure found?
   - **No** -> Show an actionable not-found state with a route back to the patient.
   - **Yes** -> Show patient/procedure identity and completion summary.

## Step selection

5. Does the procedure contain clinical steps?
   - **No** -> Show the empty timeline and, when write permission permits, an add-step action.
   - **Yes** -> Render ordered SEA events as a read-oriented timeline.
6. Does the URL identify an event?
   - **No** -> Select the first ordered event deterministically for desktop review without changing SEA state.
   - **Yes, event belongs to the procedure** -> Select it and query activity only for that event.
   - **Yes, event is missing or outside the procedure** -> Preserve procedure context and show inline not-found recovery.
7. Is the viewport mobile and an event URL is selected?
   - **Yes** -> Open the selected detail in a bottom Sheet; closing navigates to the procedure route.
   - **No** -> Keep timeline and selected detail side by side.

## Permissions and commands

8. Does SEA grant write permission for the selected event?
   - **No** -> Show read-only history and hide or disable state-changing controls with localized explanation.
   - **Yes** -> Offer note/file creation and secondary edit, delete, and reorder actions; complete/reopen is primary.
9. User chooses to create a step.
   - Validate required fields locally for presentation and authoritatively in SEA.
   - On success -> Navigate to the existing event URL and refresh ordered events.
   - On recoverable failure -> Keep form values and show an inline error.
10. User chooses Earlier or Later.
    - If the action is unavailable at the boundary -> Disable it.
    - On success -> Refresh SEA order and retain selected event.
    - On failure -> Preserve selection and announce a retryable error.
11. User chooses complete or reopen, edit, or delete.
    - On success -> Refresh the authoritative SEA event state.
    - On delete success -> Return to the procedure route and choose the next deterministic selection.
    - On failure -> Preserve context and show an inline error or toast appropriate to the command.

## Activity and evidence

12. Is selected-event activity pending, empty, or failed?
    - **Pending** -> Show a compact activity skeleton.
    - **Empty** -> Show note/file creation guidance when writable, otherwise a read-only empty state.
    - **Failed** -> Show retry without discarding local drafts or upload selection.
    - **Available** -> Show newest-first items with type, time, filename fallback, MIME type, size, and available actions.
13. User submits a note.
    - Blank -> Keep draft and show validation.
    - Success -> Clear only the submitted draft and refresh selected-event activity.
    - Failure -> Keep the draft and show retry.
14. User selects one file.
    - Persist the browser-provided base filename as bounded display metadata.
    - Success -> Clear the completed upload and refresh selected-event activity.
    - Failure -> Keep the file and expose retry/remove actions without fake progress.

## State ownership and recovery

- SEA owns persisted events, order, status, permissions, notes, and attachments.
- React owns only selected presentation state, unsaved note drafts, and in-flight/failed upload state.
- Browser back, refresh, and deep links resolve from the existing route URLs.
- Repeated commands rely on current backend idempotency, concurrency, authorization, and deletion semantics; this workspace adds no competing clinical policy.

