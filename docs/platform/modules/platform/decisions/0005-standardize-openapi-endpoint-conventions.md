# ADR-0005: Standardize OpenAPI Endpoint Conventions

Date: 2026-06-27
Status: Proposed
App: Platform
Module: platform
Capability: OpenAPI endpoint documentation
Owner: Platform
Related apps: Main host
Related modules: All HTTP module APIs
Supersedes: None
Superseded by: None
Related: 0004-protect-internal-module-api-endpoints-by-default.md

## Context

Module endpoint mappings were using raw minimal API helpers in many places, which made OpenAPI operation IDs, response metadata, anonymous justifications, and schema ownership inconsistent. The platform also needs one host-owned point for generated document exposure.

## Decision

The host composition root owns OpenAPI service registration and document exposure. Module endpoints must use platform OpenAPI-aware mapping helpers under `src/Modules/Base/Endpoints/`. Contract projects own OpenAPI request and response schemas. Anonymous endpoints and intentionally excluded endpoints require machine-readable justifications.

## Consequences

Endpoint authors must provide stable operation IDs and response metadata when adding module APIs. Infrastructure, UI, identity, and other non-module endpoints may be excluded only through an approved justified exclusion convention. Static validation enforces helper usage and explicit exceptions.

## Alternatives Considered

- Allow raw minimal API mappings and rely on review: rejected because metadata coverage is easy to miss.
- Put OpenAPI setup in each module: rejected because document exposure is a host concern.
- Snapshot the whole OpenAPI document: rejected for now because structural assertions and static validation are less brittle.

## Validation / Enforcement

- `tools/architecture/validate_openapi_endpoint_coverage.py`
- `tools/architecture/validate_module_api_boundaries.py`
- Architecture tests covering endpoint helper usage
- Host OpenAPI smoke/integration tests when host dependencies can run in test-safe configuration

## Notes

This ADR remains Proposed until maintainers explicitly accept the platform-wide convention.
