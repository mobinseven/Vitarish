# Procedure Template Management Decision Tree

Status: Future state  
Owner: Private Practice General app  
Scope: Owner entry, template workspace decisions, and recovery  
Primary persona: Private-practice owner  
Last reviewed: 2026-07-22

## Entry and authorization

1. Owner selects **Procedure Templates** from Clinic Settings and opens `/settings/procedure-templates`.
2. Do the owner route layout and Journey gate authorize entry?
   - **No** -> Preserve the existing protected-route outcome; do not mount or query the workspace.
   - **Yes** -> Mount the shared `ProcedureTemplateManagementPanel` and request authoritative SEA template data.
3. Is the initial request pending, failed, or successful?
   - **Pending** -> Show the panel's full-height loading state.
   - **Failed** -> Show a localized retryable error without inventing app-local state.
   - **Successful and empty** -> Show the create-template empty state.
   - **Successful with projects** -> Select a project deterministically and load its routines.

## Project and routine selection

4. User selects or creates a template project.
   - Keep the selected project while it remains in the refetched collection.
   - If it disappears, select the next valid project deterministically; otherwise use the empty state.
5. Does the project contain routines?
   - **No** -> Show the no-routines state and offer creation.
   - **Yes** -> Preserve the selected routine when valid, otherwise select a deterministic valid routine.
6. User creates or edits a routine.
   - **Cancel** -> Close the dialog without mutation or selection change.
   - **Validation failure** -> Keep entered values and show localized feedback.
   - **Success** -> Refetch and select the created or edited routine.
   - **Request failure** -> Keep context and allow retry.
7. User deletes a routine.
   - Require destructive confirmation.
   - **Cancel** -> Make no change.
   - **Success** -> Refetch and recover selection to the next valid routine.
   - **Failure** -> Keep the routine selected and report the error.
8. User duplicates a routine sequentially.
   - **Complete success** -> Select the duplicate after refetch.
   - **Routine created but one or more ordered steps fail** -> Report partial duplication explicitly, refetch authoritative data, and retain/select the created routine when returned.
   - **Routine creation fails** -> Do not imply a duplicate exists; preserve the source selection.

## Ordered steps

9. Does the selected routine contain steps?
   - **No** -> Show the no-steps state and offer step creation.
   - **Yes** -> Select a valid step deterministically and show its editor.
10. User creates or edits a step.
    - **Cancel or validation failure** -> Preserve current authoritative data; retain invalid draft where supported by the shared panel.
    - **Success** -> Refetch, preserve the edited step, or select the new step.
    - **Failure** -> Preserve selection and show retryable feedback.
11. User moves a step earlier or later.
    - Disable the unavailable boundary direction.
    - Execute reorder against SEA, refetch, and keep the same step selected.
    - On failure, preserve the prior rendered order until authoritative refetch and report the error.
12. User deletes a step.
    - Require confirmation.
    - **Cancel** -> Preserve the step.
    - **Success** -> Refetch and select the next valid adjacent step deterministically.
    - **Failure** -> Keep selection and show the error.

## Ownership

- The private-practice owner app owns only route composition and intent-to-route mapping.
- Clinic Web owns the Settings entry card, localized card copy, and template panel composition.
- SEA hooks and APIs own persisted projects, routines, ordered steps, mutations, authorization semantics, and refetched truth.
- No generated client, backend policy, patient-procedure behavior, or app-local workflow state is introduced.
