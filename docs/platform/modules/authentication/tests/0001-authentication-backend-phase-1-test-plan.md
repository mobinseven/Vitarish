# Authentication Backend Phase 1 Test Plan

Scope classification:

- App: Main host
- Module: authentication
- Capability: backend authentication API, orchestration seams, and OpenAPI contract
- Related apps: Main
- Related modules: Users, Communications

## Required unit coverage

- Domain challenge expiry, reuse prevention, and attempt exhaustion.
- Verification code format and attempt policy decisions.
- Safe local return URL acceptance and external/ambiguous URL rejection.
- Authentication method classification.
- Application handler input validation and structured failure mapping.
- Authentication API ProblemDetails mapper status/code mapping.

## Required integration and contract coverage

- Password login success, failure, and lockout responses.
- Phone begin/complete success, failure, expired challenge, reused challenge, and too many attempts.
- Email begin/complete success, failure, expired challenge, reused challenge, and too many attempts.
- Passkey begin/complete success and failure.
- Logout context and logout behavior.
- Current session and current user query behavior.
- Endpoint anonymous/authenticated metadata and antiforgery metadata.
- OpenAPI document validity for `/openapi/authentication.json`, including stable operation IDs, tags, schemas, auth metadata, ProblemDetails, and response types.

## Required hot scenarios

- Browser/client obtains an antiforgery token, sends `X-XSRF-TOKEN`, and completes a state-changing authentication request without mutating identity during token retrieval.
- Cookie/session compatibility across IdentityServer, ASP.NET authentication, authorization, session, antiforgery, and module endpoint order.
- Provider outage or timeout for SMS, email, Duende logout context, and WebAuthn server adapters returns explicit structured failures.
- Concurrent verification attempts cannot reuse a completed challenge or exceed attempt limits.

## Deferred Phase 2 coverage

- React authentication pages, client hooks, browser WebAuthn interop, and deletion of legacy Blazor/Razor UI are deferred until the frontend migration owns those artifacts.
