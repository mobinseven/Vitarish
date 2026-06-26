# Sea ADR-003 Migration Test Plan

Date: 2026-06-26
Status: Draft
App: Platform
Module: Sea
Capability: ADR-003 frontend/backend isolation migration
Owner: Sea module
Related ADRs: docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md
Confidence: Medium

## Objective

Verify the Sea migration keeps `Sea.Interface` frontend-safe, keeps `Sea.Contracts` serialization-only, routes Sea UI data access through `Sea.Client`, and exposes protected `Sea.Api` endpoints that adapt contracts to application use cases without moving business rules into transport layers.

## Required Unit and Contract Tests

| Area | Required tests |
| --- | --- |
| Contract serialization | DTOs, enums, routes, `SeaResult<T>`, `SeaCommandResult`, `SeaQueryResult<T>`, `SeaError`, and `SeaErrorCode` serialize and deserialize with stable JSON names. |
| Attachment metadata contracts | Sea attachment contracts serialize metadata-only fields (`AttachmentId`, `EventId`, `Content`, `ContentType`, `ContentThumbnail`, `Size`, `FileId`, audit fields) and do not expose streams, byte arrays, browser files, form files, range state, resumable upload state, or transfer progress protocols. |
| Progress/status contracts | Progress/status DTOs and `ProgressState` round-trip without referencing Domain or UI behavior. |
| Client failure handling | HTTP 400, 401, 403, 404, 409, 408/504, caller cancellation, timeout exception, invalid JSON, `HttpRequestException`, structured error body parsing, and unexpected failures map to the required `SeaErrorCode` values. |
| API mapping | Representative project, procedure, event, attachment, role, receipt, statistics, snapshot, metadata, and progress/status endpoints bind contract requests, propagate `CancellationToken`, resolve tenant context from host/API context, and return structured Sea contract responses/errors. |
| Static boundary | CI runs `python3 tools/architecture/validate_interface_boundaries.py` and fails when `Sea.Interface` references or imports Application, Domain, Infrastructure, Api, or other backend-only module projects. |

## Required Hot Test Scenarios

| Scenario | Risk protected | Environment | Expected evidence |
| --- | --- | --- | --- |
| Tenant A cannot access Tenant B Sea records | Tenant authority must be resolved at API/Application boundaries and enforced by Finbuckle-backed `SeaDbContext`. | Multi-tenant app/test host with two seeded tenants. | Tenant A cannot list/read/update/delete Tenant B projects, procedures, events, attachments, roles, receipts, or statistics; API returns forbidden/not found without leaking records. |
| Sea attachment metadata with Files-owned bytes | Sea must not own binary upload/download/stream/range/progress behavior. | App/test host with Sea and Files modules enabled. | Sea stores and returns attachment metadata including `FileId`; binary transfer goes through Files client/API only. |
| Sea client network degradation | UI must receive explicit failure categories. | Test host or fake handler injecting latency, timeout, invalid JSON, and transport failures. | UI/client receives timeout, deserialization failure, and transport failure without swallowed exceptions. |

## Suggested Tests

- Component tests for base Sea Interface loading/success/empty/failure states using `SeaResult<T>`.
- Smoke test for `/api/modules/sea/health` after module initialization.
- Regression tests for UI-only edit mappings for projects, procedures, events, and attachments.

## Deferred or Rejected Tests

- Full browser workflows for every Sea screen are deferred until the client/API endpoint coverage is complete.
- Binary transfer stress tests are rejected for Sea because Files owns binary transfer behavior.

## CI / Execution Guidance

Run boundary validation in CI after build/test restore. Add module-specific unit and integration tests to the existing test projects that own Contracts, Client, Api, and Interface concerns once the Sea endpoint implementation is completed.
