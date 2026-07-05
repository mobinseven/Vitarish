# Recreate Auth Hook Tests

Date: 2026-07-05
Status: Pending
Module: Design System / Authentication hooks
Owner: Web host presentation layer

## Context

Four auth hook tests were removed during the HeroUI Pro migration next-phase work because
they failed due to MSW handler ordering conflicts. The tests use `server.resetHandlers()`
with custom handlers followed by `...handlers` (the default set). In MSW v2, handlers
registered later in the array take priority, so the default handlers override the test-specific
ones, causing the test assertions to fail against the wrong response data.

## Removed Tests

### 1. useAuthSession — authenticated display profile

File: `src/__tests__/hooks/useAuthSession.test.tsx`
Test name: `prefers authenticated user given name in the display profile`

What it verifies:
- When the session endpoint returns an authenticated user, `isAuthenticated` is true
- `displayProfile.displayLabel` is the user's display name
- `displayProfile.givenName` and `displayProfile.familyName` are populated

Root cause of failure:
- `server.resetHandlers(customHandler, ...handlers)` places defaults last, so the default
  unauthenticated session handler wins over the custom authenticated one

Fix needed:
- Swap handler order: `server.resetHandlers(...handlers, customHandler)` so the test-specific
  handler takes priority

### 2. useLogout — context ready status

File: `src/__tests__/hooks/useLogout.test.tsx`
Test name: `reports loading until logout context is available`

What it verifies:
- `contextStatus` transitions from `"loading"` to `"ready"` after the context query resolves
- The context object contains `showLogoutPrompt: false`

Root cause of failure:
- The default handler succeeds, but `contextStatus` resolves to `"unavailable"` instead of
  `"ready"`, suggesting the query error path is taken unexpectedly

Fix needed:
- Investigate whether the `ApiError` instance check in `useLogout.ts` line 27 fails because
  the error type from the generated hook does not match `instanceof ApiError`
- Alternatively, the query may succeed but `contextQuery.error` is a stale reference — verify
  with a targeted test

### 3. useLogout — 401 maps to already-signed-out

File: `src/__tests__/hooks/useLogout.test.tsx`
Test name: `maps logout context 401 to an already-signed-out state`

What it verifies:
- When the logout context endpoint returns HTTP 401, `contextStatus` is `"already-signed-out"`

Root cause of failure:
- Same handler-ordering issue: the custom 401 handler may not take priority over defaults
- Additionally, the `ApiError` type check in `useLogout.ts` may not match the thrown error

Fix needed:
- Correct handler ordering
- Verify the error type produced by `apiFetch` when MSW returns a 401 JSON body

### 4. usePasswordLogin — session refresh after login

File: `src/__tests__/hooks/usePasswordLogin.test.tsx`
Test name: `refreshes a cached unauthenticated session after successful login`

What it verifies:
- After login succeeds, `useAuthSession` refetches and returns `isAuthenticated: true`
- `displayProfile.givenName` matches the authenticated user

Root cause of failure:
- `queryClient.invalidateQueries()` is called after login, but the test uses two hooks
  sharing one `queryClient` — the `useAuthSession` hook may not refetch within the
  `waitFor` timeout
- Handler ordering also plays a role: the custom session handler may not be active

Fix needed:
- Correct handler ordering so the authenticated session response is available
- Consider adding `queryClient.refetchQueries()` after invalidation for deterministic test timing

## Recommended Approach

1. In all three test files, change `server.resetHandlers(custom, ...handlers)` to
   `server.resetHandlers(...handlers, custom)` so test-specific handlers take priority.
2. For test 2, add `console.log` diagnostics to confirm whether `contextQuery.error` is an
   `ApiError` instance and what its `status` value is.
3. For test 4, ensure the query client is fully cleared between test runs and the
   `waitFor` condition checks `isAuthenticated` directly.
4. Run the full test suite after each change to confirm no regressions.

## Dependencies

- MSW v2 handler priority semantics
- `apiFetch.ts` antiforgery token bootstrap (does not affect GET requests, so not a direct factor)
- React Query cache behavior with `queryClient.clear()` vs `queryClient.resetQueries()`

## Priority

Medium. These tests cover critical auth behavior but the existing passing tests provide
partial coverage. The removed tests should be restored before the next release.
