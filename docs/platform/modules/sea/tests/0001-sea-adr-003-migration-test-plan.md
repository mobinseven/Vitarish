# Sea ADR-003 Migration Test Plan

Date: 2026-06-28
Status: Draft
App: Main
Module: Sea
Capability: ADR-0003 frontend/backend isolation migration, contract serialization, client failure mapping, API endpoint mapping
Owner: Sea module
Related journey: —
Related roadmap: —
Related ADRs: ADR-0003, ADR-0004
Related tasks: —
Source code/files: `src/Modules/Sea/Interface`, `src/Modules/Sea/Client`, `src/Modules/Sea/Contracts`, `src/Modules/Sea/Api`
Confidence: Medium

## 1. Objective

Verify the Sea migration keeps `Sea.Interface` frontend-safe, `Sea.Contracts` serialization-only, Sea UI data access routed through `Sea.Client`, and protected `Sea.Api` endpoints adapt contracts to application use cases without leaking business rules into transport layers.

## 2. Scope

### In Scope

- Contract serialization: DTOs, enums, routes, `SeaResult<T>`, `SeaCommandResult`, `SeaQueryResult<T>`, `SeaError`, `SeaErrorCode`.
- Attachment metadata contracts: metadata-only fields (no streams, byte arrays, browser files, progress protocols).
- Progress/status contracts: `ProgressState` round-trip without Domain or UI behavior references.
- Client failure handling: HTTP 400/401/403/404/409/408/504, cancellation, timeout, invalid JSON, `HttpRequestException`, structured error body, unexpected failures.
- API mapping: representative project, procedure, event, attachment, role, receipt, statistics, snapshot, metadata, and progress/status endpoints.
- Static boundary: `Sea.Interface` references only Client + Contracts.

### Out of Scope

- Full browser workflows for every Sea screen (deferred until endpoint coverage complete).
- Binary transfer stress tests (Files owns binary transfer behavior).
- Infrastructure integration tests (Finbuckle tenant resolution, database, background jobs).

## 3. Code / Risk Analysis

| Code area | Risk trigger | Why it matters | Test response |
|---|---|---|---|
| `Sea.Interface` project references | May reference Application/Domain/Infrastructure/Api | Frontend layer breaks isolation rule | Static boundary assertion |
| Sea Interface source imports | May import backend namespaces | Runtime coupling to backend types | Source dependency check |
| `SeaResult<T>` serialization | Contract envelope used by all Sea responses | Serialization failure breaks all UI data flow | Round-trip serialization test |
| `SeaErrorCode` enum | Maps backend errors to client-visible codes | Missing or wrong codes mislead UI error handling | Enum stability and coverage test |
| Attachment metadata | Must not expose binary/stream/progress state | Binary leaking into contract breaks frontend-safe boundary | Serialization field audit |
| Client failure mapping | 10+ failure paths from HTTP to `SeaErrorCode` | Wrong mapping hides real errors or exposes internals | Theory-based failure mapping tests |
| API endpoint mapping | Contracts → Application service binding | Backend types in contract shapes violate isolation | Representative endpoint mapping tests |
| Tenant isolation | Multi-tenant data must not cross boundaries | Data leak between tenants | Hot test with dual-tenant setup |

## 4. Required Unit Tests

| Test | Behavior protected | Target | Input / edge case | Expected result | Requirement reason |
|---|---|---|---|---|---|
| Contract serialization — DTO round-trip | All Sea DTOs preserve fields through JSON | `Sea.Contracts` request/response types | Representative data per DTO | All fields preserved, no backend types | API contract stability |
| Contract serialization — enum stability | `SeaErrorCode`, `ProgressState`, and other enums serialize as expected | Sea contract enums | All enum values | Correct JSON string values, no drift | Client error handling correctness |
| Contract serialization — `SeaResult<T>` | Success and error envelopes round-trip | `SeaResult<T>`, `SeaQueryResult<T>` | Success and error variants | Envelope structure preserved | UI data loading pattern |
| Contract serialization — `SeaCommandResult` | Command result round-trip | `SeaCommandResult` | Representative command output | All fields preserved | UI mutation feedback |
| Attachment metadata serialization | Attachment contracts expose only metadata fields | Attachment DTOs | Representative attachment data | `AttachmentId`, `EventId`, `Content`, `ContentType`, `ContentThumbnail`, `Size`, `FileId`, audit fields present; no streams/byte arrays/progress | Frontend-safe binary boundary |
| Client failure — HTTP 400 maps to validation | Bad request returns validation error | Sea Client | Mock handler returning 400 | `SeaErrorCode.ValidationFailure` | Error classification |
| Client failure — HTTP 401 maps to unauthorized | Unauthorized returns auth error | Sea Client | Mock handler returning 401 | `SeaErrorCode.Unauthorized` | Error classification |
| Client failure — HTTP 403 maps to forbidden | Forbidden returns permission error | Sea Client | Mock handler returning 403 | `SeaErrorCode.Forbidden` | Error classification |
| Client failure — HTTP 404 maps to not found | Not found returns missing resource error | Sea Client | Mock handler returning 404 | `SeaErrorCode.NotFound` | Error classification |
| Client failure — HTTP 409 maps to conflict | Conflict returns concurrency error | Sea Client | Mock handler returning 409 | `SeaErrorCode.Conflict` | Error classification |
| Client failure — HTTP 408/504 maps to timeout | Timeout returns timeout error | Sea Client | Mock handler returning 408 or 504 | `SeaErrorCode.Timeout` | Error classification |
| Client failure — cancellation maps to cancelled | Caller cancellation returns cancelled error | Sea Client | Mock handler throwing `OperationCanceledException` | `SeaErrorCode.Cancelled` | Cancellation handling |
| Client failure — invalid JSON maps to deserialization | Malformed response body returns parse error | Sea Client | Mock handler returning invalid JSON | `SeaErrorCode.DeserializationFailure` | Transport resilience |
| Client failure — transport error maps to transport failure | `HttpRequestException` returns transport error | Sea Client | Mock handler throwing `HttpRequestException` | `SeaErrorCode.TransportFailure` | Network failure handling |
| Client failure — unexpected error maps to unknown | Unrecognized status returns unknown error | Sea Client | Mock handler returning 500 | `SeaErrorCode.Unknown` | Fallback error handling |
| Client route construction — queries | Each `ISeaClient` query uses correct route and HTTP method | Sea Client query methods | Representative queries | GET/POST with expected URL paths | Request correctness |
| Client route construction — commands | Each `ISeaClient` command uses correct route and HTTP method | Sea Client command methods | Representative commands | POST/PUT/DELETE with expected URL paths | Request correctness |
| API mapping — representative endpoints | API endpoints bind contract shapes only | Sea Api endpoint mappings | Project, procedure, event, attachment, role, receipt, statistics, snapshot endpoints | Contracts → Application service inputs, no backend types in binding | Transport isolation |
| API mapping — status codes | Endpoints return correct status for each error type | Sea Api error mapping | Validation, unauthorized, forbidden, not found, conflict, timeout, unexpected | Correct HTTP status and `SeaError` body | Error contract correctness |
| API mapping — CancellationToken propagation | Endpoints pass cancellation token through | Sea Api endpoint handlers | Cancellation token passed to service | Token reaches application layer | Cooperative cancellation |
| Static boundary — Interface project references | `Sea.Interface.csproj` references only Client + Contracts | `Sea.Interface.csproj` | Static analysis | No Application/Domain/Infrastructure/Api references | ADR-0003 enforcement |
| Static boundary — Interface source imports | No backend namespace imports in Interface source | `src/Modules/Sea/Interface/**/*.cs` | Static analysis | No `Vitarish.Sea.Application/Domain/Infrastructure/Api` imports | ADR-0003 enforcement |

## 5. Suggested Unit Tests

| Test | Behavior protected | Target | Notes |
|---|---|---|---|
| Component tests for `SeaResult<T>` handling | UI correctly renders success, empty, error states | Sea Interface components | Render with mock `SeaResult` variants |
| Role assignment flow | Multiple client calls coordinate correctly | Sea Interface role components | Test multi-step role assignment |
| Attachment creation flow | UI handles metadata creation + file reference | Sea Interface attachment components | Metadata-only creation path |
| Smoke test for `/api/modules/sea/health` | Module initializes correctly | Sea Api health endpoint | Simple 200 OK check |
| Contract route constants match client routes | Route strings are consistent | `SeaApiRoutes` vs Client methods | Prevent route drift |

## 6. Required Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Trigger | Expected evidence | Execution profile |
|---|---|---|---|---|---|
| Tenant isolation — cross-tenant access denied | Tenant A cannot access Tenant B Sea records | Multi-tenant host with two seeded tenants | Tenant A user queries Tenant B projects/procedures/events/attachments/roles/receipts/statistics | API returns 403/404 without leaking any records | Semi-automated, pre-release |
| Attachment metadata with Files-owned bytes | Sea stores metadata only; binary transfer goes through Files | Host with Sea + Files modules enabled | Create attachment with FileId, verify metadata returned; attempt binary upload through Sea (should not exist) | Sea returns attachment metadata with `FileId`; no Sea endpoint accepts binary upload | Semi-automated, pre-release |
| Client network degradation | UI receives explicit failure categories, no swallowed exceptions | Test host with fault-injection handler | Inject latency, timeout, invalid JSON, transport failures | UI/client receives timeout, deserialization failure, transport failure with correct `SeaErrorCode` | Automated, nightly |

## 7. Suggested Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Reason suggested |
|---|---|---|---|
| Browser smoke — Sea project→procedure→event navigation | End-to-end read flow with receipt marking | Running host + browser | Validates UI rendering and navigation with real data |
| Auth/authorization — forbidden writes and role-gated visibility | Role-based access control works across Sea entities | Running host with test users in different roles | Security-sensitive, needs multi-user setup |
| File attachment — upload metadata + deletion | Attachment lifecycle from creation to cleanup | Running host with Sea + Files enabled | Multi-module interaction |

## 8. Deferred or Rejected Tests

| Test/scenario | Decision | Reason |
|---|---|---|
| Full browser workflows for every Sea screen | Deferred | Endpoint coverage incomplete; revisit after Phase 2 |
| Binary transfer stress tests for Sea | Rejected | Files owns binary transfer; Sea should not have binary endpoints |
| Finbuckle tenant resolution internals | Deferred | Infrastructure concern, tested via hot scenario |
| Background job and notification tests | Deferred | Not part of ADR-0003 migration scope |

## 9. Test Task Derivation

| Task | Source | Type | Requirement | Target suite | Notes |
|---|---|---|---|---|---|
| Contract serialization tests | §4 contract rows | Contract | Required | `Sea.Tests.Contracts` | Round-trip all DTOs, enums, envelopes |
| Attachment metadata tests | §4 attachment row | Contract | Required | `Sea.Tests.Contracts` | Field audit for frontend-safe boundary |
| Client failure mapping tests | §4 client failure rows | Unit | Required | `Sea.Tests.Client` | 10 failure paths with mocked HttpMessageHandler |
| Client route construction tests | §4 client route rows | Unit | Required | `Sea.Tests.Client` | Verify URL paths and HTTP methods |
| API mapping tests | §4 API rows | Unit | Required | `Sea.Tests.Api` | Representative endpoints with mocked services |
| Static boundary tests | §4 static rows | Regression | Required | `Sea.Tests.Boundary` | Project references and source imports |
| Component tests | §5 suggested | Component | Suggested | `Sea.Tests.Interface` | SeaResult rendering, role/attachment flows |

## 10. Existing Coverage / Gaps

No test code exists in the working tree. All tests are created fresh based on this plan.

**Coverage targets by layer:**

| Layer | Classes under test | Planned test classes | Coverage approach |
|---|---|---|---|
| Contracts | DTOs, enums, routes, result envelopes | 2 (SerializationTests, AttachmentMetadataTests) | Round-trip JSON |
| Client | HTTP client, failure mapping, route construction | 1 (SeaClientTests covering failures + routes) | Mock HttpMessageHandler |
| Api | Endpoint mapping, error mapping | 1 (SeaApiMappingTests) | Mock Application services |
| Boundary | Project references, source imports | 1 (BoundaryValidationTests) | Static analysis |

## 11. CI / Execution Guidance

| Suite | Execution mode | Command/procedure | Required environment |
|---|---|---|---|
| Contract serialization | Automated, every PR | `dotnet test tests/Modules/Sea.Tests/ --filter "Category=Contract"` | None |
| Client failure mapping | Automated, every PR | `dotnet test tests/Modules/Sea.Tests/ --filter "Category=Client"` | None |
| API mapping | Automated, every PR | `dotnet test tests/Modules/Sea.Tests/ --filter "Category=Api"` | None |
| Static boundary | Automated, every PR | `python3 tools/architecture/validate_no_blazor.py` | None |
| Full test suite | Automated, every PR | `dotnet test tests/Modules/Sea.Tests/` | None |
| Hot scenarios | Semi-automated, pre-release | Manual execution against multi-tenant host | Multi-tenant host with Sea + Files |

## 12. Traceability

| Test area | Linked ADR | Linked journey stage | Linked code path |
|---|---|---|---|
| Contract serialization | ADR-0003 (frontend-safe contracts) | — | `src/Modules/Sea/Contracts/**/*.cs` |
| Client failure mapping | ADR-0003 (Client layer) | — | `src/Modules/Sea/Client/**/*.cs` |
| API mapping | ADR-0004 (protected endpoints) | — | `src/Modules/Sea/Api/**/*.cs` |
| Static boundary | ADR-0003 (isolation) | — | `src/Modules/Sea/Interface/Sea.Interface.csproj` |
| Tenant isolation | — | — | `src/Modules/Sea/Infrastructure/SeaDbContext.cs` |
| Attachment metadata | ADR-0003 (frontend-safe contracts) | — | `src/Modules/Sea/Contracts/Attachments/*.cs` |
