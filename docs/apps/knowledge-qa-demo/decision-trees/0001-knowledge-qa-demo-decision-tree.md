# Knowledge QA Demo Decision Tree

Status: Active
Owner: Knowledge QA Demo app
Scope: Knowledge QA demo app branching logic
Last reviewed: 2026-07-09

## Entry

User enters the Knowledge QA demo app at `/`.

1. Is the user authenticated?
   - **Yes** → Show Knowledge QA surface.
   - **No / Demo is public** → Show public QA surface with explicit auth-gated failure states for endpoints requiring credentials.

## Submit Question

User types a question and submits.

2. Is the question input empty or whitespace-only?
   - **Yes** → Disable submit, show validation hint.
   - **No** → Proceed to submit.

3. Is the backend API reachable?
   - **Yes** → Proceed to step 4.
   - **No** → Show retryable error state with error message and retry action.

## Processing

4. Show loading state while waiting for response.

5. Did the backend return a successful answer?
   - **Yes, answer content present** → Show answer card with source attribution.
   - **Yes, empty answer** → Show empty/no-answer state with suggestion to rephrase or try a different corpus.
   - **No, API error** → Show retryable error state.

## Answer Display

6. Is source attribution available?
   - **Yes** → Show sources below the answer.
   - **No** → Show answer without sources.

7. User may submit another question → Return to step 2.

## Error Recovery

8. On API failure, user may:
   - Retry the same question → Return to step 3.
   - Modify the question → Return to step 2.
   - Navigate away → Exit flow.

## Persona-Specific Behavior

- **Authenticated user**: Full QA surface with session context.
- **Public/anonymous user**: QA surface available but API calls requiring credentials show explicit failure states.

## Cancellation

- No in-flight cancellation in initial demo scope. Loading state persists until response or timeout.

## Idempotency

- Re-submitting the same question produces a new answer (not cached). This is acceptable for a demo scope.

## Concurrency

- Only one question is in flight at a time. Submitting while loading is blocked.
