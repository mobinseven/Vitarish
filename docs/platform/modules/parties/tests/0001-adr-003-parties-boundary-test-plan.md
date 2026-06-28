# Parties ADR-0003 Boundary Test Plan

Date: 2026-06-28
Status: Draft
App: Main
Module: Parties
Capability: Interface-to-HTTP client isolation for tenant workflows
Owner: Parties module
Related journey: —
Related roadmap: —
Related ADRs: ADR-0003, ADR-0004
Related tasks: —
Source code/files: `src/Modules/Parties/Interface`, `src/Modules/Parties/Client`, `src/Modules/Parties/Contracts`, `src/Modules/Parties/Api`
Confidence: Medium

## 1. Objective

Verify that tenant create/update and tenant-indicator creation flow through Parties Client, Contracts, and Api instead of exposing Application services to Interface. Parties is a core entity module providing foundational tenant data.

## 2. Scope

### In Scope

- Client success/failure mapping for tenant create/update and indicator creation.
- Contract serialization: tenant and tenant-indicator DTOs round-trip correctly.
- API mapping: contract requests to Application service calls.
- Static boundary: Interface has no backend references.

### Out of Scope

- Full tenant lifecycle integration tests.
- Infrastructure integration tests (database, multi-tenancy).
- Hot scenarios requiring real tenant routing or authorization.

## 3. Code / Risk Analysis

| Code area | Risk trigger | Why it matters | Test response |
|---|---|---|---|
| `Parties.Interface` project references | May reference Application/Domain/Infrastructure/Api | Frontend layer breaks isolation | Static boundary assertion |
| Client failure mapping | 6+ failure paths | Wrong mapping hides errors | Failure mapping tests |
| Contract serialization | Tenant/indicator DTOs | Lost fields break consumers | Round-trip tests |
| API mapping | Contract→Application binding | Backend types in contracts | Endpoint mapping tests |

## 4. Required Unit Tests

| Test | Behavior protected | Target | Input / edge case | Expected result | Requirement reason |
|---|---|---|---|---|---|
| Client — create tenant success | Successful create maps to result | Parties Client | Mock handler returning 200/201 | `PartiesResult<TenantReadDto>` success | Happy path |
| Client — update tenant success | Successful update maps to result | Parties Client | Mock handler returning 200 | `PartiesResult<TenantReadDto>` success | Happy path |
| Client — validation failure | 400 maps to validation error | Parties Client | Mock handler returning 400 | `PartiesError` with validation kind | Error classification |
| Client — unauthorized failure | 401 maps to auth error | Parties Client | Mock handler returning 401 | `PartiesError` with unauthorized kind | Error classification |
| Client — forbidden failure | 403 maps to permission error | Parties Client | Mock handler returning 403 | `PartiesError` with forbidden kind | Error classification |
| Client — conflict failure | 409 maps to conflict | Parties Client | Mock handler returning 409 | `PartiesError` with conflict kind | Concurrency handling |
| Client — deserialization failure | Invalid JSON maps to parse error | Parties Client | Mock handler returning bad JSON | `PartiesError` with deserialization kind | Transport resilience |
| Client — transport failure | `HttpRequestException` maps to transport error | Parties Client | Mock handler throwing exception | `PartiesError` with transport kind | Network failure handling |
| Contract serialization — tenant DTO | Tenant DTO round-trips correctly | Parties Contracts | Representative tenant data | All fields preserved | API contract stability |
| Contract serialization — indicator DTO | Indicator DTO round-trips correctly | Parties Contracts | Representative indicator data | All fields preserved | API contract stability |
| API mapping — create tenant | POST maps to Application create | Parties Api | Valid contract request | Application service called with correct args | Transport isolation |
| API mapping — update tenant | PUT maps to Application update | Parties Api | Valid contract request + route ID | Application service called, route/body ID match enforced | Transport isolation |
| API mapping — create indicator | POST maps to Application create indicator | Parties Api | Valid contract request | Application service called with correct args | Transport isolation |
| Static boundary — Interface project references | Interface references only Client + Contracts | `Parties.Interface.csproj` | Static analysis | No backend references | ADR-0003 enforcement |
| Static boundary — Interface source imports | No backend namespace imports | `src/Modules/Parties/Interface/**/*.cs` | Static analysis | No backend imports | ADR-0003 enforcement |

## 5. Suggested Unit Tests

| Test | Behavior protected | Target | Notes |
|---|---|---|---|
| Client — timeout failure | Timeout maps correctly | Parties Client | Mock handler returning 408/504 |
| Client — cancellation | Cancellation maps correctly | Parties Client | Mock handler throwing `OperationCanceledException` |
| Route constants match client routes | Route strings consistent | `PartiesApiRoutes` vs Client | Prevent route drift |

## 6. Required Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Trigger | Expected evidence | Execution profile |
|---|---|---|---|---|---|
| Tenant creation and update flow | Create/update tenant from UI produces correct backend state | Running host with auth | Create tenant via UI, update via UI | Tenant created/updated correctly in backend | Semi-automated, pre-release |

Note: No additional hot test is required for this code-only boundary migration unless tenant creation is validated against a real database, tenant routing, or authorization environment before release.

## 7. Suggested Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Reason suggested |
|---|---|---|---|
| Tenant indicator lifecycle | Create/read indicators with real data | Running host | Validates full indicator flow |

## 8. Deferred or Rejected Tests

| Test/scenario | Decision | Reason |
|---|---|---|
| Full tenant lifecycle integration | Deferred | Requires database and multi-tenancy setup |
| Tenant routing tests | Deferred | Infrastructure concern |

## 9. Test Task Derivation

| Task | Source | Type | Requirement | Target suite | Notes |
|---|---|---|---|---|---|
| Contract serialization tests | §4 contract rows | Contract | Required | `Parties.Tests.Contracts` | Round-trip DTOs |
| Client failure mapping tests | §4 client rows | Unit | Required | `Parties.Tests.Client` | 6 failure paths |
| API mapping tests | §4 API rows | Unit | Required | `Parties.Tests.Api` | 3 endpoint mappings |
| Static boundary tests | §4 static rows | Regression | Required | `Parties.Tests.Boundary` | Project refs and imports |

## 10. Existing Coverage / Gaps

No test code exists in the working tree. All tests are created fresh based on this plan.

**Coverage targets by layer:**

| Layer | Classes under test | Planned test classes | Coverage approach |
|---|---|---|---|
| Contracts | Tenant/indicator DTOs | 1 (SerializationTests) | Round-trip JSON |
| Client | HTTP client, failures | 1 (PartiesClientTests) | Mock HttpMessageHandler |
| Api | Endpoint mapping | 1 (PartiesApiMappingTests) | Mock Application services |
| Boundary | Project references, imports | 1 (BoundaryValidationTests) | Static analysis |

## 11. CI / Execution Guidance

| Suite | Execution mode | Command/procedure | Required environment |
|---|---|---|---|
| Contract serialization | Automated, every PR | `dotnet test tests/Modules/Parties.Tests/ --filter "Category=Contract"` | None |
| Client tests | Automated, every PR | `dotnet test tests/Modules/Parties.Tests/ --filter "Category=Client"` | None |
| API mapping | Automated, every PR | `dotnet test tests/Modules/Parties.Tests/ --filter "Category=Api"` | None |
| Static boundary | Automated, every PR | `python3 tools/architecture/validate_interface_boundaries.py` | None |
| Full test suite | Automated, every PR | `dotnet test tests/Modules/Parties.Tests/` | None |

## 12. Traceability

| Test area | Linked ADR | Linked journey stage | Linked code path |
|---|---|---|---|
| Contract serialization | ADR-0003 (frontend-safe contracts) | — | `src/Modules/Parties/Contracts/**/*.cs` |
| Client failure mapping | ADR-0003 (Client layer) | — | `src/Modules/Parties/Client/**/*.cs` |
| API mapping | ADR-0004 (protected endpoints) | — | `src/Modules/Parties/Api/**/*.cs` |
| Static boundary | ADR-0003 (isolation) | — | `src/Modules/Parties/Interface/Parties.Interface.csproj` |
