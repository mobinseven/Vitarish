# Files Migration Test Plan

Date: 2026-06-28
Status: Draft
App: Main
Module: Files
Capability: Interface to Client/Contracts/Api migration
Owner: Files module
Related journey: —
Related roadmap: —
Related ADRs: ADR-0003, ADR-0004
Related tasks: —
Source code/files: `src/Modules/Files/Interface`, `src/Modules/Files/Client`, `src/Modules/Files/Contracts`, `src/Modules/Files/Api`
Confidence: Medium

## 1. Objective

Protect the Files module against regression in frontend/backend isolation, contract serialization, client failure mapping, and API endpoint mapping. Files is a Clinic dependency via Sea.Interface — binary transfer ownership and metadata accuracy directly impact active product workflows.

## 2. Scope

### In Scope

- Client failure mapping: metadata success/failure, upload cancellation, download failure, non-success response handling.
- Contract serialization: file metadata DTOs with supported metadata fields.
- API mapping: metadata get/list/create requests to Application DTOs.
- Static boundary: Interface has no backend references.

### Out of Scope

- Actual binary upload/download integration tests (require object storage).
- Storage provider implementation tests.
- Hot scenarios requiring real storage or browser.

## 3. Code / Risk Analysis

| Code area | Risk trigger | Why it matters | Test response |
|---|---|---|---|
| `Files.Interface` project references | May reference backend layers | Frontend isolation break | Static boundary assertion |
| Client metadata mapping | Success/failure response mapping | Wrong mapping breaks UI | Failure mapping tests |
| Client upload cancellation | Cancellation must propagate cleanly | Corrupt state or hidden failures | Cancellation behavior tests |
| Client download failure | Non-success responses and disposal | Resource leaks or swallowed errors | Download failure tests |
| Contract serialization | Metadata DTOs | Lost fields break consumers | Round-trip tests |
| API metadata mapping | Contract→Application binding | Backend types in contracts | Endpoint mapping tests |

## 4. Required Unit Tests

| Test | Behavior protected | Target | Input / edge case | Expected result | Requirement reason |
|---|---|---|---|---|---|
| Client — metadata success mapping | Successful metadata response deserializes correctly | Files Client | Mock handler returning valid metadata JSON | Correct `FileMetadata` fields | Happy path |
| Client — metadata failure mapping | Non-success metadata responses map to errors | Files Client | Mock handler returning 400/401/403/404 | Correct failure kind per status | Error classification |
| Client — upload cancellation | Canceled upload returns cancellation error | Files Client | Mock handler throwing `OperationCanceledException` | Cancellation error, no partial state | Cancellation safety |
| Client — upload timeout | Timed-out upload returns timeout error | Files Client | Mock handler returning 408/504 | Timeout error | Timeout handling |
| Client — download failure maps non-success | Download non-success responses map to errors | Files Client | Mock handler returning 400/403/404 | Correct failure kind | Error classification |
| Client — download disposes failed response | Failed download response is disposed | Files Client | Mock handler returning error | Response disposed, no resource leak | Resource safety |
| Client — transport failure | `HttpRequestException` maps to transport error | Files Client | Mock handler throwing exception | Transport failure kind | Network resilience |
| Client — deserialization failure | Invalid JSON maps to parse error | Files Client | Mock handler returning invalid JSON | Deserialization failure kind | Transport resilience |
| Contract serialization — metadata DTO | File metadata round-trips correctly | Files Contracts | Representative metadata data | All fields preserved | API contract stability |
| API mapping — metadata get | GET maps to Application query | Files Api | Valid contract request | Application service called correctly | Transport isolation |
| API mapping — metadata list | List maps to Application query | Files Api | Valid list request | Application service called with filters | Transport isolation |
| API mapping — metadata create | POST maps to Application command | Files Api | Valid create request | Application service called with correct args | Transport isolation |
| Static boundary — Interface project references | Interface references only Client + Contracts | `Files.Interface.csproj` | Static analysis | No backend references | ADR-0003 enforcement |
| Static boundary — Interface source imports | No backend namespace imports | `src/Modules/Files/Interface/**/*.cs` | Static analysis | No backend imports | ADR-0003 enforcement |

## 5. Suggested Unit Tests

| Test | Behavior protected | Target | Notes |
|---|---|---|---|
| Client — happy path upload | Successful upload returns metadata | Files Client | Mock handler returning valid response |
| Client — happy path download | Successful download returns stream | Files Client | Mock handler returning valid content |
| Route constants match client routes | Route strings consistent | `FilesApiRoutes` vs Client | Prevent route drift |

## 6. Required Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Trigger | Expected evidence | Execution profile |
|---|---|---|---|---|---|
| Upload and download realistic files | End-to-end binary transfer works correctly | Running host with object storage | Upload file, verify metadata created, download and compare bytes | Upload completes, metadata correct, download matches original | Semi-automated, pre-release |
| Upload cancellation | Cancellation propagates without corruption | Browser/HTTP client capable of aborting | Start upload, cancel mid-flight | No corrupt content created, UI shows cancellation state | Semi-automated, pre-release |
| Storage failure during upload/download | Storage unavailable produces structured error | Host with fault-injected storage | Attempt upload/download with storage down | Structured failure returned, no storage internals exposed | Semi-automated, nightly |

## 7. Suggested Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Reason suggested |
|---|---|---|---|
| Concurrent upload of same file | Race condition on metadata creation | Running host with storage | Validates idempotency |
| Large file upload with progress | Progress tracking works for large files | Running host + browser | UI feedback accuracy |

## 8. Deferred or Rejected Tests

| Test/scenario | Decision | Reason |
|---|---|---|
| Object storage provider integration | Deferred | Requires real or simulated storage backend |
| Background file processing tests | Deferred | Requires worker runtime |
| CDN/caching tests | Deferred | Requires CDN infrastructure |

## 9. Test Task Derivation

| Task | Source | Type | Requirement | Target suite | Notes |
|---|---|---|---|---|---|
| Contract serialization tests | §4 contract row | Contract | Required | `Files.Tests.Contracts` | Round-trip metadata DTO |
| Client failure mapping tests | §4 client rows | Unit | Required | `Files.Tests.Client` | 8 failure paths |
| API mapping tests | §4 API rows | Unit | Required | `Files.Tests.Api` | 3 endpoint mappings |
| Static boundary tests | §4 static rows | Regression | Required | `Files.Tests.Boundary` | Project refs and imports |

## 10. Existing Coverage / Gaps

No test code exists in the working tree. All tests are created fresh based on this plan.

**Coverage targets by layer:**

| Layer | Classes under test | Planned test classes | Coverage approach |
|---|---|---|---|
| Contracts | Metadata DTOs | 1 (SerializationTests) | Round-trip JSON |
| Client | HTTP client, failures, upload/download | 1 (FilesClientTests) | Mock HttpMessageHandler |
| Api | Metadata endpoint mapping | 1 (FilesApiMappingTests) | Mock Application services |
| Boundary | Project references, imports | 1 (BoundaryValidationTests) | Static analysis |

## 11. CI / Execution Guidance

| Suite | Execution mode | Command/procedure | Required environment |
|---|---|---|---|
| Contract serialization | Automated, every PR | `dotnet test tests/Modules/Files.Tests/ --filter "Category=Contract"` | None |
| Client tests | Automated, every PR | `dotnet test tests/Modules/Files.Tests/ --filter "Category=Client"` | None |
| API mapping | Automated, every PR | `dotnet test tests/Modules/Files.Tests/ --filter "Category=Api"` | None |
| Full test suite | Automated, every PR | `dotnet test tests/Modules/Files.Tests/` | None |
| Hot scenarios | Semi-automated, pre-release | Manual execution against host with storage | Host with object storage |

## 12. Traceability

| Test area | Linked ADR | Linked journey stage | Linked code path |
|---|---|---|---|
| Contract serialization | ADR-0003 (frontend-safe contracts) | — | `src/Modules/Files/Contracts/**/*.cs` |
| Client failure mapping | ADR-0003 (Client layer) | — | `src/Modules/Files/Client/**/*.cs` |
| API mapping | ADR-0004 (protected endpoints) | — | `src/Modules/Files/Api/**/*.cs` |
| Static boundary | ADR-0003 (isolation) | — | `src/Modules/Files/Interface/Files.Interface.csproj` |
