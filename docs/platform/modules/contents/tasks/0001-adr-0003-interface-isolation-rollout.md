# ADR-003 Module Migration Plan

## Scope
This plan completes the module Interface isolation rollout for this module and preserves backend-only dependencies, tenant ownership, file/streaming/provider boundaries, ADR-004 endpoint protection, and validation evidence.

## Boundary work
- Remove forbidden `<Module>.Interface` project references to same-module Application, Domain, Infrastructure, Api projects and cross-module backend projects.
- Replace Interface backend namespace imports with `<Module>.Contracts` and `<Module>.Client` namespaces.
- Keep cross-module backend dependencies in Application/Domain/Infrastructure only; Interface may use only approved cross-module Contracts/Interface libraries.
- Keep `Entities.Server` usage outside Interface; Interface may use `Entities.Contracts` only.
- Resolve tenant context in API/Application/Infrastructure. Do not trust arbitrary UI tenant values unless a public contract explicitly owns the value.

## Contract, client, and API ownership
- Contracts own wire DTOs, route constants, public API enums, operation result shapes, and error shapes.
- Client owns authenticated HTTP transport, cancellation propagation, status/error/deserialization/timeout/cancellation handling, and contract serialization.
- API owns protected route mapping through `MapProtectedModuleGroup`, malformed request handling, status mapping, and contract-to-Application command/query adaptation. Business rules remain in Domain/Application.

## ADR-004 implications
- Internal module APIs must use `MapProtectedModuleGroup`.
- Anonymous endpoints require `MapAnonymousModuleGroup(prefix, non-empty justification)`.
- Internal module clients must use `AuthenticatedModuleApiClient` and registration through `AddInternalModuleApiClient<TClientContract,TClientImplementation>`.
- External provider clients remain excluded only when explicitly documented and outside internal module API transport.

## Validation commands
- `python3 tools/architecture/validate_no_blazor.py`
- `python3 tools/architecture/validate_module_api_boundaries.py`

## Done criteria
- The Interface project has no backend project references and no backend namespace usage.
- The Client uses HTTP, contract DTOs, cancellation tokens, and explicit failure handling.
- The API maps protected contract routes to Application commands/queries without business rules.
- Static ADR-003 and ADR-004 validators pass with zero violations.

## Contents-specific requirements
- Keep asset/file binary ownership with the Files module; Contents contracts carry metadata and IDs only.
- Keep search indexing integration backend-only.
- Map content item, revision, relation, term, operation result, and error contract shapes at the API boundary.
