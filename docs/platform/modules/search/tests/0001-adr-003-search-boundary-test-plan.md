# Search ADR-0003 Boundary Test Plan

Date: 2026-06-28
Status: Draft
App: Main
Module: Search
Capability: Interface-to-HTTP client isolation for search and index status
Owner: Search module
Related journey: —
Related roadmap: —
Related ADRs: ADR-0003, ADR-0004
Related tasks: —
Source code/files: `src/Modules/Search/Interface`, `src/Modules/Search/Client`, `src/Modules/Search/Contracts`, `src/Modules/Search/Api`
Confidence: Medium

## 1. Objective

Verify that Search Interface components call Search Client contracts instead of `IMediator`, Application queries, Domain models, or Infrastructure. Search provides low-risk boundary migration with straightforward client/API mapping.

## 2. Scope

### In Scope

- Client success/failure/cancellation/deserialization for search and index-status requests.
- Contract serialization: search request/result DTOs with representative metadata payloads.
- API mapping: search and index-status contract requests to Application queries.
- Static boundary: Interface has no backend references.

### Out of Scope

- Search engine integration tests (Elasticsearch, SQL Server full-text).
- Indexing pipeline tests.
- Hot scenarios requiring real search infrastructure.

## 3. Code / Risk Analysis

| Code area | Risk trigger | Why it matters | Test response |
|---|---|---|---|
| `Search.Interface` project references | May reference Application/Domain/Infrastructure | Frontend isolation break | Static boundary assertion |
| Client failure mapping | 4+ failure paths for search and index-status | Wrong mapping breaks UI | Failure mapping tests |
| Contract serialization | Search request/result DTOs | Lost metadata fields | Round-trip tests |
| API mapping | Contract→Application binding | Backend types in contracts | Endpoint mapping tests |

## 4. Required Unit Tests

| Test | Behavior protected | Target | Input / edge case | Expected result | Requirement reason |
|---|---|---|---|---|---|
| Client — search success | Successful search response deserializes correctly | Search Client | Mock handler returning valid result JSON | Correct search results | Happy path |
| Client — search failure | Non-success search responses map to errors | Search Client | Mock handler returning 400/401/403/404 | Correct failure kind per status | Error classification |
| Client — search cancellation | Canceled search returns cancellation error | Search Client | Mock handler throwing `OperationCanceledException` | Cancellation error | Cancellation safety |
| Client — search deserialization failure | Invalid JSON maps to parse error | Search Client | Mock handler returning invalid JSON | Deserialization failure | Transport resilience |
| Client — index-status success | Successful index-status response deserializes | Search Client | Mock handler returning valid status JSON | Correct status data | Happy path |
| Client — index-status failure | Non-success index-status responses map to errors | Search Client | Mock handler returning 400/401/403/404 | Correct failure kind | Error classification |
| Client — index-status cancellation | Canceled index-status returns cancellation | Search Client | Mock handler throwing `OperationCanceledException` | Cancellation error | Cancellation safety |
| Client — index-status deserialization | Invalid JSON maps to parse error | Search Client | Mock handler returning invalid JSON | Deserialization failure | Transport resilience |
| Contract serialization — search request DTO | Search request round-trips correctly | Search Contracts | Representative search query with metadata | All fields preserved | API contract stability |
| Contract serialization — search result DTO | Search result round-trips correctly | Search Contracts | Representative search results | All fields preserved, metadata intact | API contract stability |
| Contract serialization — index-status DTO | Index-status round-trips correctly | Search Contracts | Representative status data | All fields preserved | API contract stability |
| API mapping — search endpoint | POST maps to Application SearchQuery | Search Api | Valid contract request | Application service called correctly | Transport isolation |
| API mapping — index-status endpoint | GET maps to Application GetIndexStatusQuery | Search Api | Valid request | Application service called correctly | Transport isolation |
| Static boundary — Interface project references | Interface references only Client + Contracts | `Search.Interface.csproj` | Static analysis | No backend references | ADR-0003 enforcement |
| Static boundary — Interface source imports | No backend namespace imports | `src/Modules/Search/Interface/**/*.cs` | Static analysis | No backend imports | ADR-0003 enforcement |

## 5. Suggested Unit Tests

| Test | Behavior protected | Target | Notes |
|---|---|---|---|
| Client — transport failure | `HttpRequestException` maps correctly | Search Client | Network error path |
| Route constants match client routes | Route strings consistent | `SearchApiRoutes` vs Client | Prevent route drift |

## 6. Required Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Trigger | Expected evidence | Execution profile |
|---|---|---|---|---|---|
| Stale index / eventual consistency | Search before indexing catches up shows correct or degraded state | Host with runtime indexing enabled | Create/update indexed content, search immediately | UI displays current results or intentional stale/degraded state with observable logs | Semi-automated, nightly |

## 7. Suggested Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Reason suggested |
|---|---|---|---|
| Search with real index | End-to-end search accuracy | Running host with search engine | Validates full retrieval pipeline |
| Index status after reindex | Status correctly reflects reindex progress | Running host with search engine | Operational visibility |

## 8. Deferred or Rejected Tests

| Test/scenario | Decision | Reason |
|---|---|---|
| Search engine integration | Deferred | Requires Elasticsearch/SQL Server full-text |
| Indexing pipeline tests | Deferred | Requires background worker and document pipeline |
| Search relevance scoring | Deferred | Non-deterministic, requires LLM/embedding service |

## 9. Test Task Derivation

| Task | Source | Type | Requirement | Target suite | Notes |
|---|---|---|---|---|---|
| Contract serialization tests | §4 contract rows | Contract | Required | `Search.Tests.Contracts` | Round-trip all DTOs |
| Client failure mapping tests | §4 client rows | Unit | Required | `Search.Tests.Client` | 8 failure paths |
| API mapping tests | §4 API rows | Unit | Required | `Search.Tests.Api` | 2 endpoint mappings |
| Static boundary tests | §4 static rows | Regression | Required | `Search.Tests.Boundary` | Project refs and imports |

## 10. Existing Coverage / Gaps

No test code exists in the working tree. All tests are created fresh based on this plan.

**Coverage targets by layer:**

| Layer | Classes under test | Planned test classes | Coverage approach |
|---|---|---|---|
| Contracts | Search request/result/status DTOs | 1 (SerializationTests) | Round-trip JSON |
| Client | HTTP client, failures | 1 (SearchClientTests) | Mock HttpMessageHandler |
| Api | Endpoint mapping | 1 (SearchApiMappingTests) | Mock Application services |
| Boundary | Project references, imports | 1 (BoundaryValidationTests) | Static analysis |

## 11. CI / Execution Guidance

| Suite | Execution mode | Command/procedure | Required environment |
|---|---|---|---|
| Contract serialization | Automated, every PR | `dotnet test tests/Modules/Search.Tests/ --filter "Category=Contract"` | None |
| Client tests | Automated, every PR | `dotnet test tests/Modules/Search.Tests/ --filter "Category=Client"` | None |
| API mapping | Automated, every PR | `dotnet test tests/Modules/Search.Tests/ --filter "Category=Api"` | None |
| Full test suite | Automated, every PR | `dotnet test tests/Modules/Search.Tests/` | None |
| Hot scenarios | Semi-automated, nightly | Manual execution against host with search engine | Host with search infrastructure |

## 12. Traceability

| Test area | Linked ADR | Linked journey stage | Linked code path |
|---|---|---|---|
| Contract serialization | ADR-0003 (frontend-safe contracts) | — | `src/Modules/Search/Contracts/**/*.cs` |
| Client failure mapping | ADR-0003 (Client layer) | — | `src/Modules/Search/Client/**/*.cs` |
| API mapping | ADR-0004 (protected endpoints) | — | `src/Modules/Search/Api/**/*.cs` |
| Static boundary | ADR-0003 (isolation) | — | `src/Modules/Search/Interface/Search.Interface.csproj` |
