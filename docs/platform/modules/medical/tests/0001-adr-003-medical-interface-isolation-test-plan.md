# ADR-003 Medical Interface Isolation Test Plan

- Date: 2026-06-26
- Status: Draft
- App: Platform
- Module: Medical
- Capability: ADR-003 frontend/backend isolation for Medical clinical, prescription, provider, and import flows
- Owner: Platform / Medical module maintainers
- Related modules: Files if import file transfer is later required; Parties/Users if tenant/user context integration is involved
- Related ADRs: ADR-003 module Interface isolation; Medical ADR-003 migration
- Confidence: Medium; endpoint implementation and runtime fixtures still need follow-up tests.

## Objective

Prove Medical Interface depends only on frontend-safe Medical Client/Contracts, while Medical Api adapts contract DTOs to backend Application use cases without exposing Domain/Application implementation details over HTTP.

## Scope

In scope: Medical Contracts serialization shapes, Medical Client HTTP failure mapping, Medical Api mapping and authorization-context propagation, Interface boundary validation, provider/import/prescription resilience visibility.

Out of scope: provider SDK contract tests, large-file streaming/upload behavior, database migration correctness, and clinical business-rule tests already owned by Application/Domain.

## Code/Risk Analysis

- Medical prescriptions and credentials are clinical/security-sensitive; tenant/clinic/doctor context must remain backend-authoritative.
- Provider calls can fail by outage, timeout, rejection, credential revocation, or retry exhaustion; UI must receive safe failure contracts only.
- Import payload size is currently treated as JSON-body scope. If large files, resumable upload, range requests, streaming progress, or custom transfer are required, draft a module-specific ADR and evaluate Files-owned file IDs before implementation.
- Contracts must not reference Application, Domain, Infrastructure, Api, Interface, UI libraries, persistence libraries, provider SDKs, or secret payloads.

## Required Contract Tests

| Requirement | Category | Target | Expected result |
| --- | --- | --- | --- |
| Serialize and deserialize Medical code-system request/response DTOs. | Contract | Medical.Contracts | Round-tripped DTOs preserve IDs, keys, category, active state, and safe capability flags. |
| Serialize and deserialize prescription detail, history, and template DTOs. | Contract | Medical.Contracts | Prescription and template item arrays, status enums, dates, and safe capability flags round-trip. |
| Serialize and deserialize provider/import result contracts. | Contract | Medical.Contracts | Provider metadata excludes credentials/raw payloads and preserves provider key, operation, correlation ID, retry counts, and user-safe message. |
| Verify contract enums preserve expected wire names/values. | Contract | Medical.Contracts | Medical status, import, credential, provider, and item category enums remain stable. |
| Verify validation/error contracts round-trip all expected failure codes. | Contract | Medical.Contracts | Validation, unauthorized, forbidden, not found, conflict, provider, credential, sync, retry, reconciliation, timeout, cancellation, deserialization, and unexpected failures round-trip. |

## Required Client Tests

| Requirement | Category | Target | Expected result |
| --- | --- | --- | --- |
| Prescription query success maps `200 OK` to success result. | Unit/Contract | Medical.Client | `GetPrescriptionAsync` returns a successful Medical result. |
| Prescription command success maps command result to success. | Unit/Contract | Medical.Client | Command methods return a successful command result without throwing. |
| Validation failure maps `400`. | Unit/Contract | Medical.Client | Failure code is validation failure. |
| Unauthorized maps `401`. | Unit/Contract | Medical.Client | Failure code is unauthorized. |
| Forbidden maps `403`. | Unit/Contract | Medical.Client | Failure code is forbidden. |
| Not found maps `404`. | Unit/Contract | Medical.Client | Failure code is not found. |
| Conflict maps `409`. | Unit/Contract | Medical.Client | Failure code is conflict. |
| Provider unavailable maps `503`. | Unit/Contract | Medical.Client | Failure code is provider unavailable. |
| Provider rejected maps provider-specific contract failure. | Unit/Contract | Medical.Client | Provider rejection response maps to provider rejected. |
| Timeout maps `504` or client timeout to timeout failure. | Unit/Contract | Medical.Client | Failure code is timeout. |
| Cancellation maps to cancellation failure without unexpected failure. | Unit/Contract | Medical.Client | CancellationToken cancellation returns cancellation failure. |
| Invalid JSON maps to deserialization failure. | Unit/Contract | Medical.Client | Failure code is deserialization failure. |
| Unknown exception/status maps to unexpected failure. | Unit/Contract | Medical.Client | Failure code is unexpected failure. |

## Required Api Mapping Tests

| Requirement | Category | Target | Expected result |
| --- | --- | --- | --- |
| Contract prescription draft request maps to Application command. | Unit/Integration | Medical.Api | Only Application command fields are populated; tenant/user authority comes from host context. |
| Application prescription detail DTO maps to contract response. | Unit | Medical.Api | Domain/Application enums are converted to contract enums and backend-only fields are not leaked. |
| Application provider/import failures map to correct HTTP status and contract error. | Unit/Integration | Medical.Api | Known Application outcomes map to 400/401/403/404/409/503/504 and Medical error contracts. |
| Api never returns Domain entities directly. | Static/Unit | Medical.Api | Endpoint delegates expose contract response/result types only. |
| Api never returns Application DTOs directly when they expose backend-only shape. | Static/Unit | Medical.Api | Mapping functions create contract-owned DTOs. |
| Api resolves tenant/user context from host/API context rather than trusting Interface/Client. | Integration | Medical.Api | Requested Patient/Clinic/Doctor/Tenant values are treated as scope, not authority. |

## Required Hot Test Scenarios

| Scenario | Risk protected | Environment | Trigger | Expected behavior | Evidence | Level |
| --- | --- | --- | --- | --- | --- | --- |
| Provider outage during eligibility check. | External outage corrupts prescription state. | Authenticated app, seeded prescription, provider outage simulation. | Check eligibility while provider adapter is unavailable. | UI receives provider unavailable; prescription state is not corrupted. | HTTP 503 or failure contract, unchanged DB state, log/trace correlation. | Required |
| Provider timeout during prescription submission. | Timeout causes duplicate or lost submission. | App with timeout/failure injection and prescription outbox. | Submit prescription and force provider timeout. | Prescription remains in expected retry/reconciliation state. | Outbox row, prescription status, correlation log. | Required |
| Retry exhaustion surfaces operational visibility. | Exhausted retries disappear from support workflows. | Background retry worker or simulated retry pipeline. | Force max retries. | Retry-exhausted result and operational/reconciliation visibility are exposed safely. | Outbox status, dashboard alert, safe error payload. | Required |
| Import/provider sync failure is safe. | Import failures leak provider payloads or hide state. | Seeded import/sync request and failing provider/import adapter. | Run sync/import with injected failure. | Failure is recorded and safe status is visible to UI. | Import status, warnings/errors without raw payloads. | Required |
| Tenant A cannot access Tenant B prescription or credential data. | Cross-tenant data leak. | Multi-tenant auth/session setup. | Tenant A requests Tenant B scope. | Unauthorized/forbidden failure; no data returned or mutated. | HTTP 401/403, audit/security log. | Required |
| Unauthorized support user cannot resolve duplicate risk, reconcile fallback, or escalate. | Privilege escalation. | Authenticated non-support user and support-only records. | Invoke reconciliation actions. | Forbidden failure and no state mutation. | HTTP 403, unchanged DB state, audit entry. | Required |
| Credential expiration/revocation blocks provider submission. | Invalid credentials used for clinical submission. | Seeded expired/revoked credential. | Submit prescription through affected provider. | Credential failure returned; provider submission blocked. | Credential failure contract, no provider payload, audit/log. | Required |

## Suggested Tests

- Component tests for Interface failure-to-notification rendering once components complete client-result handling.
- Smoke test for protected Medical API route registration in the host composition root.
- Static test asserting Medical.Contracts project has no non-serialization framework references.

## Deferred or Rejected Tests

- Realtime progress, resumable upload, range request, SignalR, and streaming tests are rejected for this migration unless a future import/file-transfer ADR requires them.
- Provider SDK payload replay tests are deferred to Infrastructure/provider adapter test suites.

## CI / Execution Guidance

- Run the ADR-003 interface boundary validator for Medical in FailFast mode after temporary exceptions are removed.
- Run Medical.Contracts and Medical.Client unit/contract suites in normal CI.
- Run hot scenarios in pre-release or nightly environments with seeded tenants, credentials, prescriptions, provider fault injection, and auditable logs.

## Traceability

This plan protects ADR-003 frontend/backend isolation and the Medical ADR-003 migration requirement that Medical Interface uses Client/Contracts only, Api adapts HTTP to Application, and backend behavior remains in Application/Domain/Infrastructure.
