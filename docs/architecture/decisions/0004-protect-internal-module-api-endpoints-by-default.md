# ADR-0004: Protect internal module API endpoints by default

Date: 2026-06-25
Status: Accepted
App: Platform
Module: Platform
Capability: Internal module API authorization
Owner: Architecture
Related apps: All frontend hosts
Related modules: All modules exposing internal module APIs
Supersedes:
Superseded by:
Related: docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md

## Context

ADR-0003 established frontend/backend isolation through module `Interface`, `Client`, `Contracts`, and `Api` boundaries. Internal module APIs are the HTTP boundary between module clients and backend application use cases, so unprotected route groups would weaken that boundary and could expose application capabilities without the host authorization policy.

## Decision

All internal module API endpoints must require authorization by default. Module API mappings must create route groups through the platform-owned protected endpoint mapping abstraction in `Modules.Base`, which applies the platform endpoint authorization convention.

Anonymous internal module endpoints are allowed only through an explicit, justified, reviewable exception path on the same platform abstraction. The exception must be visible in code at the route-group creation point and is reserved for use cases such as externally invoked webhooks that cannot carry the internal user access token.

Module API/client boundaries remain as defined by ADR-0003:

- `Client` projects speak HTTP using `Contracts` only.
- `Api` projects adapt `Contracts` to `Application` commands and queries.
- `Interface` projects depend on `Client` and `Contracts`, not backend implementation.
- `Application` and `Domain` do not depend on HTTP, JSON, `Api`, `Client`, `Contracts`, or UI artifacts.

## Consequences

Positive consequences:

- Internal module APIs are protected unless a reviewer can see and evaluate an explicit anonymous exception.
- Endpoint authorization is enforced through one platform-owned source of truth.
- ADR-0003 API/client boundaries gain a security enforcement rule.

Negative consequences:

- Module API migrations must replace direct Minimal API route-group creation.
- Legitimate anonymous endpoints require a documented exception at the mapping site.

## Alternatives Considered

### Rely on reviewers to remember `RequireAuthorization`

Rejected. Per-endpoint review is easy to miss and creates multiple sources of truth for authorization policy.

### Host-level blanket authorization only

Rejected. Host-level defaults are useful, but module API mappings still need a reviewable code convention that prevents bypasses when new route groups are introduced.

## Validation / Enforcement

Static validation must fail internal module API mappings that call `MapGroup`, `MapGet`, `MapPost`, `MapPut`, `MapDelete`, or `MapPatch` outside the approved protected endpoint abstraction. Static validation must also fail anonymous routes unless they use the explicit anonymous exception path.

Static validation must fail internal module clients that inject raw `HttpClient` without using the approved authenticated module API client transport abstraction, and direct DI registrations that bypass the shared module-client registration helper. External provider clients are excluded.

## Notes

This ADR records an internal platform security convention. It does not introduce business authorization rules; authorization policy evaluation remains owned by the host identity/authorization layer and application use cases continue to own business decisions.
