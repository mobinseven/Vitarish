# Medical ADR-0003 Interface Isolation Test Plan

Date: 2026-06-28
Status: Draft
App: Main
Module: Medical
Capability: ADR-0003 frontend/backend isolation for clinical, prescription, provider, and import flows
Owner: Medical module
Related journey: —
Related roadmap: —
Related ADRs: ADR-0003, ADR-0004
Related tasks: —
Source code/files: `src/Modules/Medical/Interface`, `src/Modules/Medical/Client`, `src/Modules/Medical/Contracts`, `src/Modules/Medical/Api`
Confidence: Medium

## 1. Objective

Prove Medical Interface depends only on frontend-safe Medical Client/Contracts, while Medical Api adapts contract DTOs to backend Application use cases without exposing Domain/Application implementation details over HTTP.

## 2. Scope

### In Scope

- Medical Contracts serialization shapes: code-system, prescription, provider/import, enums, error contracts.
- Medical Client HTTP failure mapping: 13 failure paths to `MedicalErrorCode`.
- Medical Api mapping: contract→Application adaptation, authorization-context propagation.
- Interface boundary validation: static project reference and source import checks.
- Provider/import/prescription resilience visibility.

### Out of Scope

- Provider SDK contract tests (Tamin, Salamat adapters).
- Large-file streaming/upload behavior.
- Database migration correctness.
- Clinical business-rule tests owned by Application/Domain.
- Realtime progress, resumable upload, range request, SignalR tests.

## 3. Code / Risk Analysis

| Code area | Risk trigger | Why it matters | Test response |
|---|---|---|---|
| Medical prescriptions/credentials | Clinical/security-sensitive data | Tenant/clinic/doctor context must remain backend-authoritative | Contract serialization + authorization tests |
| Provider calls | Outage, timeout, rejection, credential revocation, retry exhaustion | UI must receive safe failure contracts only | Client failure mapping tests |
| Import payload | Currently JSON-body scope; may evolve | Binary upload boundary must stay with Files | File-reference contract tests |
| Contracts references | Must not reference Application, Domain, Infrastructure, Api, UI libs, provider SDKs, secrets | Backend leakage | Static boundary assertion |

## 4. Required Unit Tests

| Test | Behavior protected | Target | Input / edge case | Expected result | Requirement reason |
|---|---|---|---|---|---|
| Contract — code-system request/response round-trip | Code-system DTOs preserve all fields | Medical.Contracts | Representative code-system data | IDs, keys, category, active state, capability flags preserved | API contract stability |
| Contract — prescription detail/history/template round-trip | Prescription DTOs preserve all fields | Medical.Contracts | Representative prescription data | Item arrays, status enums, dates, capability flags preserved | Clinical data accuracy |
| Contract — provider/import result round-trip | Provider metadata excludes credentials | Medical.Contracts | Representative provider result | Provider key, operation, correlation ID, retry counts, user-safe message preserved; no raw payloads | Security boundary |
| Contract — enum stability | All Medical enums serialize correctly | Medical.Contracts | All enum values | Status, import, credential, provider, item category enums stable | Error handling correctness |
| Contract — error contract round-trip | All 14 failure codes round-trip | Medical.Contracts | All `MedicalErrorCode` values | Validation, unauthorized, forbidden, not found, conflict, provider, credential, sync, retry, reconciliation, timeout, cancellation, deserialization, unexpected preserved | Error contract stability |
| Client — prescription query success | 200 OK maps to success | Medical.Client | Mock handler returning valid JSON | Successful Medical result | Happy path |
| Client — prescription command success | Command result maps to success | Medical.Client | Mock handler returning valid JSON | Successful command result | Happy path |
| Client — validation failure | 400 maps to validation | Medical.Client | Mock handler returning 400 | `MedicalErrorCode.ValidationFailure` | Error classification |
| Client — unauthorized failure | 401 maps to unauthorized | Medical.Client | Mock handler returning 401 | `MedicalErrorCode.Unauthorized` | Error classification |
| Client — forbidden failure | 403 maps to forbidden | Medical.Client | Mock handler returning 403 | `MedicalErrorCode.Forbidden` | Error classification |
| Client — not found failure | 404 maps to not found | Medical.Client | Mock handler returning 404 | `MedicalErrorCode.NotFound` | Error classification |
| Client — conflict failure | 409 maps to conflict | Medical.Client | Mock handler returning 409 | `MedicalErrorCode.Conflict` | Concurrency handling |
| Client — provider unavailable | 503 maps to provider unavailable | Medical.Client | Mock handler returning 503 | `MedicalErrorCode.ProviderUnavailable` | Provider resilience |
| Client — provider rejected | Provider-specific failure mapped | Medical.Client | Mock handler returning provider error body | `MedicalErrorCode.ProviderRejected` with details | Provider error handling |
| Client — timeout failure | 504 or client timeout maps to timeout | Medical.Client | Mock handler returning 504 or timeout | `MedicalErrorCode.Timeout` | Timeout handling |
| Client — cancellation | Cancellation maps to cancelled | Medical.Client | Mock handler throwing `OperationCanceledException` | `MedicalErrorCode.Cancelled` | Cancellation safety |
| Client — deserialization failure | Invalid JSON maps to parse error | Medical.Client | Mock handler returning invalid JSON | `MedicalErrorCode.DeserializationFailure` | Transport resilience |
| Client — unexpected failure | Unknown status maps to unexpected | Medical.Client | Mock handler returning 500 | `MedicalErrorCode.Unexpected` | Fallback handling |
| API mapping — prescription draft request | Contract request maps to Application command | Medical.Api | Valid contract request | Only Application fields populated; tenant/user from host context | Transport isolation |
| API mapping — prescription detail response | Application DTO maps to contract response | Medical.Api | Application prescription detail | Domain/Application enums converted; no backend-only fields leaked | Contract fidelity |
| API mapping — provider/import failures | Application failures map to HTTP status + contract error | Medical.Api | Known Application outcomes | Correct HTTP status (400/401/403/404/409/503/504) and Medical error body | Error contract correctness |
| API — never returns Domain entities | Endpoint delegates expose contract types only | Medical.Api | Static analysis | No Domain entity types in endpoint return signatures | Isolation enforcement |
| API — never returns backend-only Application DTOs | Mapping functions create contract-owned DTOs | Medical.Api | Static analysis | No Application DTO types in endpoint return signatures | Isolation enforcement |
| API — tenant/user context from host | Requested scope treated as scope, not authority | Medical.Api | Request with Patient/Clinic/Doctor values | Context resolved from host/API context, not trusted from Interface | Authorization correctness |
| Static boundary — Interface project references | Interface references only Client + Contracts | `Medical.Interface.csproj` | Static analysis | No Application/Domain/Infrastructure/Api references | ADR-0003 enforcement |
| Static boundary — Interface source imports | No backend namespace imports | `src/Modules/Medical/Interface/**/*.cs` | Static analysis | No backend imports | ADR-0003 enforcement |

## 5. Suggested Unit Tests

| Test | Behavior protected | Target | Notes |
|---|---|---|---|
| Component tests for Interface failure rendering | UI correctly handles client failure results | Medical Interface components | Render with mock failure states |
| Smoke test for Medical API route registration | Protected routes registered correctly | Medical Api composition root | Verify endpoint groups exist |
| Contracts project has no non-serialization refs | Contracts stays pure serialization | `Medical.Contracts.csproj` | Static analysis for framework refs |

## 6. Required Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Trigger | Expected evidence | Execution profile |
|---|---|---|---|---|---|
| Provider outage during eligibility check | External outage corrupts prescription state | Authenticated app, seeded prescription, provider outage simulation | Check eligibility while provider adapter unavailable | UI receives provider unavailable; prescription state unchanged | Semi-automated, pre-release |
| Provider timeout during prescription submission | Timeout causes duplicate or lost submission | App with timeout/failure injection and prescription outbox | Submit prescription, force provider timeout | Prescription remains in expected retry/reconciliation state | Semi-automated, nightly |
| Retry exhaustion surfaces operational visibility | Exhausted retries disappear from support workflows | Background retry worker or simulated retry pipeline | Force max retries | Retry-exhausted result and reconciliation visibility exposed safely | Semi-automated, nightly |
| Import/provider sync failure is safe | Import failures leak provider payloads or hide state | Seeded import/sync request and failing provider adapter | Run sync/import with injected failure | Failure recorded, safe status visible to UI; no raw payloads | Semi-automated, nightly |
| Tenant isolation — cross-tenant data blocked | Tenant A cannot access Tenant B prescriptions/credentials | Multi-tenant auth/session setup | Tenant A requests Tenant B scope | Unauthorized/forbidden failure; no data returned or mutated | Semi-automated, pre-release |
| Unauthorized support user blocked | Privilege escalation through reconciliation actions | Authenticated non-support user, support-only records | Invoke reconciliation actions | Forbidden failure, no state mutation | Semi-automated, pre-release |
| Credential expiration blocks provider submission | Invalid credentials used for clinical submission | Seeded expired/revoked credential | Submit prescription through affected provider | Credential failure returned; submission blocked | Semi-automated, pre-release |

## 7. Suggested Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Reason suggested |
|---|---|---|---|
| Full prescription lifecycle with real provider | End-to-end prescribing flow | Running host with Tamin/Salamat adapter | Highest-value integration, needs real provider |
| Import pipeline with real data | Import→parse→validate→store | Running host with seed data | Validates full import chain |

## 8. Deferred or Rejected Tests

| Test/scenario | Decision | Reason |
|---|---|---|
| Provider SDK payload replay | Deferred | Belongs to Infrastructure/provider adapter suites |
| Realtime progress/resumable upload | Rejected | Not required by current ADRs |
| Database migration correctness | Deferred | Infrastructure concern |

## 9. Test Task Derivation

| Task | Source | Type | Requirement | Target suite | Notes |
|---|---|---|---|---|---|
| Contract serialization tests | §4 contract rows | Contract | Required | `Medical.Tests.Contracts` | Round-trip all DTOs, enums, errors |
| Client failure mapping tests | §4 client rows | Unit | Required | `Medical.Tests.Client` | 13 failure paths |
| API mapping tests | §4 API rows | Unit | Required | `Medical.Tests.Api` | 6 mapping tests + 2 static checks |
| Static boundary tests | §4 static rows | Regression | Required | `Medical.Tests.Boundary` | Project refs and imports |

## 10. Existing Coverage / Gaps

No test code exists in the working tree. All tests are created fresh based on this plan.

**Coverage targets by layer:**

| Layer | Classes under test | Planned test classes | Coverage approach |
|---|---|---|---|
| Contracts | 5 contract categories (code-system, prescription, provider, enums, errors) | 1 (SerializationTests) | Round-trip JSON |
| Client | HTTP client, 13 failure paths | 1 (MedicalClientTests) | Mock HttpMessageHandler |
| Api | Endpoint mapping, error mapping, static checks | 1 (MedicalApiMappingTests) | Mock Application services + static analysis |
| Boundary | Project references, imports | 1 (BoundaryValidationTests) | Static analysis |

## 11. CI / Execution Guidance

| Suite | Execution mode | Command/procedure | Required environment |
|---|---|---|---|
| Contract serialization | Automated, every PR | `dotnet test tests/Modules/Medical.Tests/ --filter "Category=Contract"` | None |
| Client tests | Automated, every PR | `dotnet test tests/Modules/Medical.Tests/ --filter "Category=Client"` | None |
| API mapping | Automated, every PR | `dotnet test tests/Modules/Medical.Tests/ --filter "Category=Api"` | None |
| Static boundary | Automated, every PR | `python3 tools/architecture/validate_interface_boundaries.py` | None |
| Full test suite | Automated, every PR | `dotnet test tests/Modules/Medical.Tests/` | None |
| Hot scenarios | Semi-automated, pre-release/nightly | Manual execution against host with provider fault injection | Host with seeded tenants, credentials, prescriptions |

## 12. Traceability

| Test area | Linked ADR | Linked journey stage | Linked code path |
|---|---|---|---|
| Contract serialization | ADR-0003 (frontend-safe contracts) | — | `src/Modules/Medical/Contracts/**/*.cs` |
| Client failure mapping | ADR-0003 (Client layer) | — | `src/Modules/Medical/Client/**/*.cs` |
| API mapping | ADR-0004 (protected endpoints) | — | `src/Modules/Medical/Api/**/*.cs` |
| Static boundary | ADR-0003 (isolation) | — | `src/Modules/Medical/Interface/Medical.Interface.csproj` |
| Provider resilience | — | — | `src/Modules/Medical/Infrastructure/Providers/**/*.cs` |
| Tenant isolation | — | — | `src/Modules/Medical/Infrastructure/Data/MedicalDbContext.cs` |
