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
Related:
- docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md
- docs/platform/modules/platform/decisions/0004-protect-internal-module-api-endpoints-by-default.md

## Context

Module API endpoints are the HTTP boundary between frontend hosts, module clients, and backend application use cases.

ADR-0003 requires module API projects to adapt `Contracts` artifacts to application commands and queries. ADR-0004 requires module API mappings to use approved protected endpoint abstractions.

OpenAPI is the platform source of truth for reviewable HTTP surface documentation and generated client expectations. If an endpoint is missing from OpenAPI, or if its schema is derived from application, domain, persistence, infrastructure, or UI artifacts, the platform has multiple competing sources of truth for routes, request shapes, response shapes, and review expectations.

## Decision

All module API endpoints must be OpenAPI-covered by platform convention unless they are explicitly excluded with a written justification at the endpoint mapping site.

An endpoint is OpenAPI-covered when its route, HTTP method, request schema, response schema, status codes, and endpoint metadata are represented through approved platform endpoint helpers.

Module API projects must use approved platform endpoint helpers for endpoint mapping, protection conventions, and OpenAPI convention application. Direct Minimal API mapping is allowed only inside the implementation of approved platform helpers.

OpenAPI request and response schemas must be based on `Contracts` artifacts. API projects may adapt `Contracts` artifacts to application commands and queries, but OpenAPI must not publish schemas from application, domain, persistence, infrastructure, or UI artifacts.

Explicit OpenAPI exclusion is allowed only through the approved exclusion helper and must include a non-empty justification string. The exclusion must be visible at the endpoint mapping site so reviewers can distinguish intentional exclusions from accidental omissions.

Authorization and OpenAPI documentation are separate concerns. An endpoint may be protected, internal, or non-public and still be OpenAPI-covered unless it is explicitly excluded with justification.

## Consequences

- Module API documentation coverage becomes consistent across modules.
- `Contracts` artifacts remain the single source of truth for HTTP serialization shapes.
- Reviewers can distinguish intentional OpenAPI exclusions from accidental omissions.
- API projects continue adapting transport contracts to application use cases without leaking internal implementation models into OpenAPI.
- Static validation can enforce route mapping, schema ownership, and documented exception conventions.


## Alternatives Considered

### Rely on manual review for OpenAPI coverage

Rejected. Manual review is inconsistent and easy to miss across modules.

### Allow OpenAPI schemas from application or domain models

Rejected. Application and domain models are implementation concerns, not HTTP transport contracts. Publishing them couples API documentation to core internals and weakens the `Contracts` boundary from ADR-0003.

### Allow direct Minimal API mapping with local OpenAPI calls

Rejected. Local mapping and documentation calls create multiple convention sources and make static validation unreliable.

## Validation / Enforcement

Static validation must fail when a module API endpoint:

- bypasses approved platform endpoint helpers;
- is not OpenAPI-covered and does not use the approved exclusion helper;
- uses an OpenAPI exclusion without a non-empty written justification;
- publishes request or response schemas from application, domain, persistence, infrastructure, or UI artifacts;
- documents a schema shape that is not based on `Contracts` artifacts.
