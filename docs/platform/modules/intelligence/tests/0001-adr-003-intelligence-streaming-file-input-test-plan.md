# Intelligence ADR-0003 Streaming and File-Input Test Plan

Date: 2026-06-28
Status: Draft
App: Main
Module: Intelligence
Capability: ADR-0003 frontend/backend isolation, streaming, and Files-owned file input
Owner: Intelligence module
Related journey: —
Related roadmap: —
Related ADRs: ADR-0003, ADR-0004
Related tasks: —
Source code/files: `src/Modules/Intelligence/Interface`, `src/Modules/Intelligence/Client`, `src/Modules/Intelligence/Contracts`, `src/Modules/Intelligence/Api`
Confidence: Medium

## 1. Objective

Protect the Intelligence ADR-0003 migration by proving contracts serialize safely, the frontend client maps transport failures explicitly, protected API endpoints adapt contracts to Application without leaking backend types, streaming disposes resources, and file input remains Files-owned. Intelligence is a Clinic dependency via Sea.Interface.

## 2. Scope

### In Scope

- Contract serialization: request, result, error, chunk, and file-reference DTOs.
- Client non-streaming failure mapping: validation, unauthorized, forbidden, unsupported media type, file unavailable, too-large payload, provider unavailable, provider timeout, cancellation, deserialization, unexpected.
- Client malformed chunk handling: malformed or out-of-order newline JSON chunks.
- API mapping: protected endpoints use contract DTOs, call Application abstractions, return contract responses/chunks.
- Static boundary: Interface has no backend references.

### Out of Scope

- Provider SDK contract tests (Ollama, OpenAI, etc.).
- Database migration correctness.
- Clinical business-rule tests owned by Application/Domain.
- Realtime progress, resumable upload, range request, SignalR tests (unless future ADR requires).

## 3. Code / Risk Analysis

| Code area | Risk trigger | Why it matters | Test response |
|---|---|---|---|
| `Intelligence.Interface` project references | May reference Application/Domain/Infrastructure/Api | Frontend isolation break | Static boundary assertion |
| Contract serialization | Request/result/error/chunk/file-reference DTOs | Provider SDK or binary content leaking | Round-trip serialization tests |
| Client failure mapping | 11+ failure paths from HTTP to `IntelligenceErrorCode` | Wrong mapping hides provider errors | Theory-based failure mapping tests |
| Streaming chunk handling | Malformed or out-of-order JSON chunks | Crash or incorrect display | Chunk parsing tests |
| API mapping | Contract→Application binding | Backend types in contracts | Endpoint mapping tests |

## 4. Required Unit Tests

| Test | Behavior protected | Target | Input / edge case | Expected result | Requirement reason |
|---|---|---|---|---|---|
| Contract serialization — request DTO round-trip | Request DTOs preserve fields, no backend types | Intelligence Contracts | Representative data | All safe fields preserved, no provider SDK types | Frontend-safe boundary |
| Contract serialization — result/error DTO round-trip | Result and error DTOs preserve structure | Intelligence Contracts | Success and error variants | Envelope preserved | UI error handling |
| Contract serialization — chunk DTO round-trip | Chunk DTOs preserve fields | Intelligence Contracts | Representative chunk data | All fields preserved | Streaming contract stability |
| Contract serialization — file-reference DTO round-trip | File-reference DTOs preserve metadata only | Intelligence Contracts | Representative file ref | No binary/stream/transfer fields | Files-owned boundary |
| Client — validation failure | 400 maps to validation error | Intelligence Client | Mock handler returning 400 | `IntelligenceErrorCode.ValidationFailure` | Error classification |
| Client — unauthorized failure | 401 maps to auth error | Intelligence Client | Mock handler returning 401 | `IntelligenceErrorCode.Unauthorized` | Error classification |
| Client — forbidden failure | 403 maps to permission error | Intelligence Client | Mock handler returning 403 | `IntelligenceErrorCode.Forbidden` | Error classification |
| Client — unsupported media type | 415 maps to unsupported media | Intelligence Client | Mock handler returning 415 | `IntelligenceErrorCode.UnsupportedMediaType` | Media validation |
| Client — file unavailable | 404 maps to file unavailable | Intelligence Client | Mock handler returning 404 | `IntelligenceErrorCode.FileUnavailable` | File reference handling |
| Client — too-large payload | 413 maps to too large | Intelligence Client | Mock handler returning 413 | `IntelligenceErrorCode.TooLargePayload` | Size validation |
| Client — provider unavailable | 503 maps to provider unavailable | Intelligence Client | Mock handler returning 503 | `IntelligenceErrorCode.ProviderUnavailable` | Provider resilience |
| Client — provider timeout | 504 maps to timeout | Intelligence Client | Mock handler returning 504 | `IntelligenceErrorCode.ProviderTimeout` | Provider timeout |
| Client — cancellation | Cancellation maps to cancelled | Intelligence Client | Mock handler throwing `OperationCanceledException` | `IntelligenceErrorCode.Cancelled` | Cancellation handling |
| Client — deserialization failure | Invalid JSON maps to parse error | Intelligence Client | Mock handler returning invalid JSON | `IntelligenceErrorCode.DeserializationFailure` | Transport resilience |
| Client — unexpected error | Unrecognized status maps to unexpected | Intelligence Client | Mock handler returning 500 | `IntelligenceErrorCode.Unexpected` | Fallback handling |
| Client — malformed chunk | Malformed JSON chunk emits terminal failure | Streaming reader | Malformed newline JSON | `MalformedChunk` or `DeserializationFailure` | Chunk integrity |
| Client — out-of-order chunk | Out-of-order chunk handled gracefully | Streaming reader | Chunks arriving out of sequence | Graceful handling or terminal failure | Streaming robustness |
| API mapping — protected endpoints | Endpoints use contract DTOs only | Intelligence Api | Representative endpoints | Contracts → Application inputs, no backend types | Transport isolation |
| Static boundary — Interface project references | Interface references only Client + Contracts | `Intelligence.Interface.csproj` | Static analysis | No backend references | ADR-0003 enforcement |
| Static boundary — Interface source imports | No backend namespace imports | `src/Modules/Intelligence/Interface/**/*.cs` | Static analysis | No backend imports | ADR-0003 enforcement |

## 5. Suggested Unit Tests

| Test | Behavior protected | Target | Notes |
|---|---|---|---|
| Client — transport failure | `HttpRequestException` maps correctly | Intelligence Client | Network error path |
| Contract enum stability | All Intelligence enums serialize correctly | Intelligence Contracts | Prevent drift |
| Route constants match client routes | Route strings consistent | `IntelligenceApiRoutes` vs Client | Prevent route drift |

## 6. Required Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Trigger | Expected evidence | Execution profile |
|---|---|---|---|---|---|
| Streaming cancellation and disposal | Response/reader leaks after cancellation | Browser or integration host with streaming provider stub | Cancel responder/rewrite stream mid-output | Client emits cancellation/failure state; server request aborts without leaked resources | Semi-automated, nightly |
| Partial stream failure | Provider stream dies after partial output | API host with provider stub that throws mid-stream | Start responder/rewrite stream | Terminal `PartialStreamFailure` visible; partial text remains presentation state only | Semi-automated, nightly |
| Provider timeout/outage | Provider unavailability leaks raw backend exceptions | API host with provider timeout/outage stub | Non-streaming and streaming calls | Contract error is `ProviderTimeout` or `ProviderUnavailable`; no provider SDK type leaves backend | Semi-automated, nightly |
| Browser audio capture failure | UI media APIs unavailable or denied | Browser with denied microphone permission | Start transcription capture | Interface reports supported/recording state safely, does not call backend with tenant/user data | Manual, pre-release |
| File input rejection | Intelligence does not become binary upload owner | Browser/API host with Files reference flow | Attach unsupported/unavailable/oversized file reference | Files owns transfer; Intelligence receives only reference metadata and returns explicit errors | Semi-automated, pre-release |
| Tenant/user authorization | Interface/client cannot forge authority | Authenticated host with multiple tenants/users | Attempt cross-tenant provider/model/file use | Protected API context determines identity; Application rejects unauthorized access | Semi-automated, pre-release |

## 7. Suggested Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Reason suggested |
|---|---|---|---|
| End-to-end responder with real provider | Full streaming response with real LLM | Running host + Ollama/OpenAI | Highest-value integration, needs provider |
| Multi-model provider switching | Different models respond correctly | Running host with multiple providers | Provider abstraction correctness |

## 8. Deferred or Rejected Tests

| Test/scenario | Decision | Reason |
|---|---|---|
| Provider SDK payload replay | Deferred | Belongs to Infrastructure/provider adapter suites |
| Realtime progress/resumable upload | Rejected | Not required by current ADRs |
| SignalR streaming tests | Deferred | Unless future ADR requires |

## 9. Test Task Derivation

| Task | Source | Type | Requirement | Target suite | Notes |
|---|---|---|---|---|---|
| Contract serialization tests | §4 contract rows | Contract | Required | `Intelligence.Tests.Contracts` | Round-trip all DTOs |
| Client failure mapping tests | §4 client rows | Unit | Required | `Intelligence.Tests.Client` | 11 failure paths |
| Streaming chunk tests | §4 chunk rows | Unit | Required | `Intelligence.Tests.Client` | Malformed/out-of-order chunks |
| API mapping tests | §4 API rows | Unit | Required | `Intelligence.Tests.Api` | Protected endpoint mapping |
| Static boundary tests | §4 static rows | Regression | Required | `Intelligence.Tests.Boundary` | Project refs and imports |

## 10. Existing Coverage / Gaps

No test code exists in the working tree. All tests are created fresh based on this plan.

**Coverage targets by layer:**

| Layer | Classes under test | Planned test classes | Coverage approach |
|---|---|---|---|
| Contracts | Request/result/error/chunk/file-ref DTOs | 1 (SerializationTests) | Round-trip JSON |
| Client | HTTP client, failures, streaming chunks | 1 (IntelligenceClientTests) | Mock HttpMessageHandler |
| Api | Endpoint mapping | 1 (IntelligenceApiMappingTests) | Mock Application services |
| Boundary | Project references, imports | 1 (BoundaryValidationTests) | Static analysis |

## 11. CI / Execution Guidance

| Suite | Execution mode | Command/procedure | Required environment |
|---|---|---|---|
| Contract serialization | Automated, every PR | `dotnet test tests/Modules/Intelligence.Tests/ --filter "Category=Contract"` | None |
| Client tests | Automated, every PR | `dotnet test tests/Modules/Intelligence.Tests/ --filter "Category=Client"` | None |
| API mapping | Automated, every PR | `dotnet test tests/Modules/Intelligence.Tests/ --filter "Category=Api"` | None |
| Static boundary | Automated, every PR | `python3 tools/architecture/validate_interface_boundaries.py` | None |
| Full test suite | Automated, every PR | `dotnet test tests/Modules/Intelligence.Tests/` | None |
| Hot scenarios | Semi-automated, nightly | Manual execution against host with provider stubs | Host with mocked providers |

## 12. Traceability

| Test area | Linked ADR | Linked journey stage | Linked code path |
|---|---|---|---|
| Contract serialization | ADR-0003 (frontend-safe contracts) | — | `src/Modules/Intelligence/Contracts/**/*.cs` |
| Client failure mapping | ADR-0003 (Client layer) | — | `src/Modules/Intelligence/Client/**/*.cs` |
| Streaming chunks | ADR-0003 (Client layer) | — | `src/Modules/Intelligence/Client/**/*.cs` |
| API mapping | ADR-0004 (protected endpoints) | — | `src/Modules/Intelligence/Api/**/*.cs` |
| Static boundary | ADR-0003 (isolation) | — | `src/Modules/Intelligence/Interface/Intelligence.Interface.csproj` |
