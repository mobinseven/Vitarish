# ADR-0005: Cover module API endpoints with OpenAPI

Date: 2026-06-27
Status: Accepted
App: Platform
Module: Platform
Capability: Module API OpenAPI coverage
Owner: Architecture
Related apps: All frontend hosts
Related modules: All modules exposing APIs
Supersedes:
Superseded by:
Related: docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md, docs/platform/modules/platform/decisions/0004-protect-internal-module-api-endpoints-by-default.md

## Context

Module APIs are the HTTP boundary between module clients, frontend hosts, and backend application use cases. ADR-0003 established that API projects adapt `Contracts` to application commands and queries, and ADR-0004 established that module API mappings must use approved protected endpoint abstractions.

OpenAPI coverage is the platform source of truth for externally reviewable HTTP surface documentation. Endpoints that are not represented in OpenAPI, or that publish schemas not derived from `Contracts`, create a second source of truth for route shape, request shape, response shape, and review expectations.

## Decision

All API endpoints must be covered by OpenAPI by platform convention unless they are explicitly excluded with a written justification at the mapping site.

Module APIs must use the approved platform endpoint helpers for route mapping and OpenAPI convention application. Direct endpoint mapping is not allowed when it bypasses the approved helpers.

OpenAPI request and response schemas must come from `Contracts` artifacts. API projects may adapt `Contracts` to application commands and queries, but they must not publish OpenAPI schemas from application, domain, persistence, infrastructure, or UI artifacts.

Explicit OpenAPI exclusions are allowed only when the endpoint should not be represented in generated API documentation. The exclusion and justification must be visible in code at the endpoint mapping site so reviewers can evaluate the exception.

## Consequences

Positive consequences:

- Module API documentation coverage is enforced consistently across modules.
- `Contracts` remains the single source of truth for HTTP serialization shapes.
- Reviewers can distinguish intentional OpenAPI exclusions from accidental omissions.
- API projects keep adapting transport contracts to application use cases without leaking domain, persistence, infrastructure, or UI models into OpenAPI.

Negative consequences:

- Existing module API mappings may need to migrate to approved endpoint helpers.
- Legitimate undocumented endpoints require explicit justification at the mapping site.
- Static validation must stay aligned with approved endpoint helper names and exception paths.

## Alternatives Considered

### Rely on manual review for OpenAPI coverage

Rejected. Manual review is easy to miss and creates inconsistent endpoint documentation conventions across modules.

### Allow schemas from application or domain models

Rejected. Application and domain models are not transport contracts. Publishing them as OpenAPI schemas would couple HTTP documentation to core implementation details and duplicate the `Contracts` boundary from ADR-0003.

### Allow direct Minimal API mapping with local OpenAPI calls

Rejected. Local endpoint mapping and documentation calls would create multiple sources of truth for platform conventions and make static validation less reliable.

## Validation / Enforcement

Static validation must fail API endpoint mappings that bypass approved endpoint helpers.

Static validation must fail API endpoint mappings that are not OpenAPI-covered by platform convention and do not use the explicit OpenAPI exclusion path with justification.

Static validation must fail OpenAPI request or response schemas that come from application, domain, persistence, infrastructure, or UI artifacts instead of `Contracts` artifacts.

## Notes

This ADR defines a platform API documentation convention. It does not require every endpoint to be public, nor does it change authorization requirements from ADR-0004. Authorization and OpenAPI documentation remain separate conventions enforced by platform-approved endpoint helpers and static validation.
