# Knowledge QA Demo Test Plan

Status: Active
Owner: Knowledge QA Demo app
Scope: Test coverage for Knowledge QA demo routes and components
Last reviewed: 2026-07-09
Verifies journey: [0001 Knowledge QA Demo Journey](../journeys/0001-knowledge-qa-demo-journey.md)

## Covered Scenarios

| Scenario | Expected Behavior | Priority |
|---|---|---|
| Initial route renders | `/knowledge/qa` renders the QA surface with input and submit | High |
| Landing route renders | `/` renders public product context and a login action | High |
| Protected route redirects | Unauthenticated access to `/knowledge/qa` redirects to `/login` with the return URL | High |
| Login succeeds | Valid credentials establish a session and continue to the requested Knowledge route | High |
| Authenticated login redirects | An authenticated user visiting `/login` continues to `/knowledge/qa` | Medium |
| Question input accepts text | Input field accepts and displays typed text | High |
| Submit invokes API | Submit triggers the Knowledge QA API call with the question | High |
| Loading state displays | Loading indicator appears while API call is in flight | High |
| Successful answer displays | Answer card renders with response text | High |
| Successful answer with sources | Source links render below answer | Medium |
| Empty/no-answer state | Empty state renders with rephrase guidance | High |
| API failure displays error | Retryable error state renders with retry button | High |
| Retry after failure | Retry button re-submits the question | High |
| Submit disabled during loading | Submit button is disabled while a question is in flight | High |
| Empty question blocked | Submit is disabled for empty/whitespace input | Medium |
| Private-practice routes not rendered | Navigation does not include owner/clinic routes | High |

## Deferred Scenarios

| Scenario | Reason |
|---|---|
| Cancellation | Not in initial demo scope |
| Idempotency verification | Accepted as non-cached for demo scope |
| Concurrency control | Single-question-in-flight is enforced by UI, not deeply tested |
| Corpus selection | Deferred until corpus list integration is wired |

## Test Placement

Tests live under `src/Hosts/Web/apps/knowledge-qa-demo/src/__tests__/` after workspace restructuring.

## Environment

- MSW mocks for Knowledge QA API endpoints.
- No backend dependency for component-level tests.
- Vitest + jsdom.
