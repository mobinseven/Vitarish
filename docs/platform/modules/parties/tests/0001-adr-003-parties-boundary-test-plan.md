# ADR-003 Parties boundary test plan

Date: 2026-06-25
Status: Suggested
App: Shared platform
Module: Parties
Capability: Interface-to-HTTP client isolation for tenant workflows
Related ADRs: ADR-003
Confidence: Medium

## Objective
Verify that tenant create/update and tenant-indicator creation flow through Parties Client, Contracts, and Api instead of exposing Application services to Interface.

## Required unit/contract tests
- Parties client maps create/update tenant success responses into successful `PartiesResult<TenantReadDto>` values.
- Parties client maps validation, unauthorized, forbidden, conflict, deserialization, and transport failures into explicit `PartiesError` outcomes.
- Contract serialization round-trips tenant and tenant-indicator DTOs if these shapes become externally consumed.

## Required integration tests
- API maps `POST /api/modules/parties/tenants` to the existing Application create-tenant call.
- API maps `PUT /api/modules/parties/tenants/{tenantId}` to the existing Application update-tenant call and rejects route/body id mismatch.
- API maps `POST /api/modules/parties/tenant-indicators` to the existing Application create-indicator call.

## Hot tests
No hot test is required for this code-only boundary migration unless tenant creation is validated against a real database, tenant routing, or authorization environment before release.
