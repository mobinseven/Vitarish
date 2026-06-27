# ADR-0005: Standardize OpenAPI Endpoint Conventions

Date: 2026-06-27
Status: Accepted
App: Platform
Module: Platform
Capability: OpenAPI endpoint conventions and coverage
Owner: Platform Architecture
Related apps: All frontend hosts
Related modules: All HTTP module APIs
Supersedes: None
Superseded by: None
Related:
- docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md
- docs/platform/modules/platform/decisions/0004-protect-internal-module-api-endpoints-by-default.md

## Context

Module API endpoints are the HTTP boundary between frontend hosts, module clients, and backend application use cases.

ADR-0003 requires module API projects to adapt `Contracts` artifacts to application commands and queries. ADR-0004 requires module API mappings to use approved protected endpoint abstractions.

Module endpoint mappings previously used raw Minimal API helpers in multiple places. This made OpenAPI operation IDs, response metadata, anonymous endpoint justifications, exclusion justifications, and schema ownership inconsistent.

OpenAPI is the platform source of truth for reviewable HTTP surface documentation and generated client expectations. If an endpoint is missing from OpenAPI, or if its schema is derived from application, domain, persistence, infrastructure, or UI artifacts, the platform has multiple competing sources of truth for routes, request shapes, response shapes, status codes, and review expectations.

The platform also needs one host-owned point for OpenAPI service registration and generated document exposure.

## Decision

The host composition root owns OpenAPI service registration and document exposure. Individual modules must not expose their own independent OpenAPI documents by default.

All module API endpoints must be OpenAPI-covered by platform convention unless they are explicitly excluded through an approved justified exclusion path.

An endpoint is OpenAPI-covered when its route, HTTP method, stable operation ID, request schema, response schema, status codes, and endpoint metadata are represented through approved platform endpoint helpers.

Module API projects must use the approved OpenAPI-aware endpoint helpers under `src/Modules/Base/Core/Endpoints/` for endpoint mapping, protection conventions, and OpenAPI convention application. Direct Minimal API mapping is allowed only inside the implementation of approved platform helpers.

OpenAPI request and response schemas must be based on `Contracts` artifacts. API projects may adapt `Contracts` artifacts to application commands and queries, but OpenAPI must not publish schemas from application, domain, persistence, infrastructure, identity, UI, or persistence artifacts.

Endpoint authors must provide stable operation IDs and explicit response metadata when adding or changing module APIs.

Anonymous endpoints and intentionally excluded endpoints require machine-readable, non-empty justifications at the endpoint mapping site. The justification must be visible in code so reviewers and static validation can distinguish intentional exceptions from accidental omissions.

Authorization and OpenAPI documentation are separate concerns. An endpoint may be protected, internal, or non-public and still be OpenAPI-covered unless it is explicitly excluded with justification.

Infrastructure, UI, identity, and other non-module endpoints may be excluded from module OpenAPI coverage only through the approved justified exclusion convention.

## Consequences

Positive consequences:

- Module API documentation coverage becomes consistent across modules.
- Host-level OpenAPI registration and document exposure remain centralized.
- `Contracts` artifacts remain the single source of truth for HTTP serialization shapes.
- Stable operation IDs and response metadata improve generated client reliability.
- Reviewers can distinguish intentional anonymous or undocumented endpoints from accidental omissions.
- API projects continue adapting transport contracts to application use cases without leaking internal implementation models into OpenAPI.
- Static validation can enforce helper usage, OpenAPI coverage, schema ownership, and documented exception conventions.

Negative consequences:

- Existing raw Minimal API mappings may need to migrate to approved endpoint helpers.
- Endpoint authors must provide operation IDs, response metadata, and justifications where required.
- Static validation must stay aligned with approved helper names, schema conventions, and exception paths.
- Host OpenAPI smoke tests may require test-safe host dependency configuration.

## Alternatives Considered

### Rely on manual review for OpenAPI coverage

Rejected. Manual review is inconsistent, easy to miss, and does not scale across modules.

### Allow raw Minimal API mappings with local OpenAPI calls

Rejected. Local mapping and documentation calls create multiple convention sources and make static validation unreliable.

### Put OpenAPI setup in each module

Rejected. OpenAPI document exposure is a host composition concern. Per-module setup would fragment the generated API surface and duplicate platform registration conventions.

### Allow OpenAPI schemas from application or domain models

Rejected. Application and domain models are implementation concerns, not HTTP transport contracts. Publishing them couples API documentation to core internals and weakens the `Contracts` boundary from ADR-0003.

### Snapshot the whole OpenAPI document as the main validation strategy

Rejected for now. Full-document snapshots are brittle. Structural assertions, static validation, and focused host smoke tests provide stronger and less noisy enforcement.

## Validation / Enforcement

Static validation must fail when a module API endpoint:

- bypasses approved platform endpoint helpers;
- is not OpenAPI-covered and does not use the approved exclusion helper;
- uses an OpenAPI exclusion without a machine-readable, non-empty justification;
- allows anonymous access without a machine-readable, non-empty justification;
- omits a stable operation ID;
- omits required response metadata;
- publishes request or response schemas from application, domain, persistence, infrastructure, identity, or UI artifacts;
- documents a schema shape that is not based on `Contracts` artifacts.

The validation entry points are:

- `tools/architecture/validate_openapi_endpoint_coverage.py`
- `tools/architecture/validate_module_api_boundaries.py`
- architecture tests covering approved endpoint helper usage
- host OpenAPI smoke or integration tests when host dependencies can run in test-safe configuration

## Notes

This ADR defines the platform convention for OpenAPI endpoint coverage. It does not make every endpoint public and does not weaken authorization requirements from ADR-0004. Authorization, anonymous access, OpenAPI coverage, and OpenAPI exclusion remain separate conventions enforced through approved platform endpoint helpers and static validation.
