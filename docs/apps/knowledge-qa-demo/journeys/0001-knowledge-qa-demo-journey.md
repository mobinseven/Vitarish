# Knowledge QA Demo Journey

Status: Active
Owner: Knowledge QA Demo app
Scope: Knowledge QA demo user experience
Last reviewed: 2026-07-09
Derives from: [0001 Knowledge QA Demo Decision Tree](../decision-trees/0001-knowledge-qa-demo-decision-tree.md)

## Journey Overview

User enters the Knowledge QA demo, submits a question against the Knowledge module, and receives a grounded answer or an honest error/empty state.

## Steps

1. **Enter demo**: User navigates to `/` and sees the public Knowledge QA landing page.
2. **Authenticate**: User selects **Log in**, signs in at `/login`, and returns to the originally requested Knowledge route. Direct access to `/knowledge/qa` redirects unauthenticated users to login.
3. **View corpus context**: The authenticated user sees available corpora. If the user lacks corpus access, the app shows an explicit empty state.
4. **Type question**: User enters a natural-language question in the input field.
5. **Submit question**: User presses submit or Enter. Submit is disabled while a question is in flight.
6. **Loading state**: A loading indicator appears while the backend processes the question.
7. **Answer displayed**: On success, the answer card renders with the response text and optional source links.
8. **Empty/no-answer**: If the backend returns no answer, an empty state suggests rephrasing or selecting a different corpus.
9. **Error state**: If the backend returns an error, a retryable error state is shown with a retry button.
10. **Retry or new question**: User may retry the failed question or type a new question.

## Success Scenario

User enters a valid question, backend returns a grounded answer, answer card renders with sources.

## Empty Scenario

User enters a question but the Knowledge module has no matching answer. Empty state renders with guidance.

## Failure Scenario

Backend API returns an error (timeout, 5xx, network). Error state renders with retry action.

## Permission Scenario (if authenticated)

User is authenticated but lacks access to any corpus. Explicit message states no corpora are available.

## Cancellation Scenario

Not in initial demo scope. Loading state persists until completion or timeout.

## Idempotency Scenario

Re-submitting the same question triggers a new backend call and may produce a different answer.

## Concurrency Scenario

Only one question in flight. Submit is blocked during loading.

## Recovery Scenario

After a failure, user retries and receives a successful answer or retries the same error path.
