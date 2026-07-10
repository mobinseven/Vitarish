---
name: platform-api-client-boundary
description: Aligns module HTTP APIs, OpenAPI endpoint metadata, contract DTOs, generated React clients, apiFetch, query hooks, query keys, and feature API wrappers. Use when creating or reviewing API endpoints, OpenAPI contracts, Orval output, generated clients, MSW mocks, or API-to-React data flow.
---

# Platform API Client Boundary

## Purpose

Keep the transport contract stable and boring. This skill owns OpenAPI contract shape, generated-client boundaries, and API/client handoffs; it does not own feature UI composition or business rules.

## Use

Use this skill for Minimal API endpoint metadata, module route groups, operation IDs, request/response schemas, ProblemDetails/error shapes, auth metadata, generated React clients, generated DTOs/hooks/query keys, custom fetchers, MSW mocks, and feature API wrappers over generated hooks.

Skip it when a UI-only change consumes an already stable feature hook without changing transport, generated code, or API contracts.

## Read

- `AGENTS.md`
- `docs/architecture/conventions/openapi-endpoint-conventions.md`
- `docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md`
- Host package scripts and Orval/OpenAPI configuration before running generation.

## Procedure

1. Confirm the endpoint is a thin adapter from Contracts to Application command/query behavior.
2. Use approved module route-group and OpenAPI-aware endpoint helpers.
3. Keep operation IDs stable, PascalCase, and shaped as `<Module><Action><Resource>`.
4. Expose Contract-owned serialization shapes only. Do not expose Domain, EF, Infrastructure, vendor SDK, or UI models.
5. Document auth and error outcomes with standard response metadata.
6. Treat generated clients, DTOs, hooks, query keys, and MSW mocks as generated infrastructure. Do not edit them manually.
7. Wrap generated hooks in feature-owned API wrappers or hooks before page consumption when UI state, invalidation, or error mapping is needed.
8. Keep stable error/status codes in transport; map display copy in React presentation.

## Output

```md
## API Client Boundary
Contract owner:
Operations/schemas:
Auth/errors:
Generated artifacts:
Feature wrappers:
Manual-edit risks:
Validation:
Handoff:
```

## Hard Stops

- Endpoint handlers contain business rules or return domain/persistence models.
- OpenAPI schemas expose implementation types instead of contract DTOs.
- Generated code is edited manually.
- Feature UI bypasses generated clients/hooks without a narrow documented reason.
- Transport or fetch infrastructure localizes user-facing copy.

## Validation

For endpoint coverage and module API boundaries, run:

```bash
python3 tools/architecture/validate_openapi_endpoint_coverage.py
python3 tools/architecture/validate_module_api_boundaries.py
dotnet test tests/Architecture.Tests/Architecture.Tests.csproj --filter InternalModuleApiArchitectureTests
```

For generated-client changes, run the host's documented generation, typecheck, and test scripts.
