# Users ADR-0003 Migration Test Plan

Date: 2026-06-28
Status: Draft
App: Main
Module: Users
Capability: Users Interface API/client/contracts migration
Owner: Users module
Related journey: —
Related roadmap: —
Related ADRs: ADR-0003, ADR-0004
Related tasks: —
Source code/files: `src/Modules/Users/Interface`, `src/Modules/Users/Client`, `src/Modules/Users/Contracts`, `src/Modules/Users/Api`
Confidence: Medium

## 1. Objective

Protect the Users ADR-0003 migration slice: Interface create/find/find-or-create components must use frontend-safe Users Client and Contracts instead of backend Application, Domain, Infrastructure, or Api artifacts. Users is a core identity module depended on by Sea.Infrastructure and Clinic.

## 2. Scope

### In Scope

- User search, get by id, email/phone existence checks, role membership check, create by phone/email, set email, and add role assignment endpoints.
- Contract serialization: request/response DTOs expose only safe public fields.
- Client route construction: correct URL and query string building.
- Client failure mapping: 400, 401, 403, 404, 409, timeout, cancellation, transport, deserialization.
- API mapping: domain `User` to `UserSummaryResponse` without persistence fields.
- Static boundary: Interface has no backend references.

### Out of Scope

- Profile/account pages, claims UI beyond safe wire shapes.
- Personal access tokens, temporary auto-auth links.
- Full WebAuthn HTTP mapping migration.
- Host server identity-page cleanup.
- Infrastructure integration tests (ASP.NET Identity, database).

## 3. Code / Risk Analysis

| Code area | Risk trigger | Why it matters | Test response |
|---|---|---|---|
| `Users.Interface` project references | May reference Application/Domain/Infrastructure/Api | Frontend layer breaks isolation rule | Static boundary assertion |
| Contract serialization | Request/response DTOs must not expose domain/Identity types | Backend leakage into frontend | Round-trip serialization test |
| Client failure mapping | 9+ failure paths from HTTP to `UsersClientFailureKind` | Wrong mapping hides real errors | Theory-based failure mapping tests |
| Client route construction | URL and query string correctness | Wrong routes hit wrong endpoints | Route construction tests |
| API mapping | Domain `User` → `UserSummaryResponse` | Persistence/internal fields leak into contract | Mapping test |

## 4. Required Unit Tests

| Test | Behavior protected | Target | Input / edge case | Expected result | Requirement reason |
|---|---|---|---|---|---|
| Contract serialization — request round-trip | Request DTOs preserve fields, no domain types | `Users.Contracts` request types | Representative data | All safe fields preserved, no Identity types | Frontend-safe boundary |
| Contract serialization — response round-trip | Response DTOs preserve fields, no internal types | `Users.Contracts` response types | Representative data | All safe fields preserved | API contract stability |
| Client route — search | Search method builds correct URL | `UsersClient.SearchAsync` | Search query | Correct `UsersApiRoutes` URL with query params | Request correctness |
| Client route — get by id | Get method builds correct URL | `UsersClient.GetByIdAsync` | User ID | Correct URL with ID segment | Request correctness |
| Client route — email existence check | Check method builds correct URL | `UsersClient.CheckEmailAsync` | Email address | Correct URL with email param | Request correctness |
| Client route — create | Create method builds correct URL and body | `UsersClient.CreateAsync` | Phone/email | POST to correct URL with request body | Request correctness |
| Client route — set email | Set-email method builds correct URL | `UsersClient.SetEmailAsync` | User ID, email | Correct URL with user ID | Request correctness |
| Client route — add role | Add-role method builds correct URL | `UsersClient.AddRoleAsync` | User ID, role | Correct URL with user ID and role body | Request correctness |
| Client failure — HTTP 400 | Bad request maps to validation failure | Users Client | Mock handler returning 400 | `UsersClientFailureKind.ValidationFailure` | Error classification |
| Client failure — HTTP 401 | Unauthorized maps to auth failure | Users Client | Mock handler returning 401 | `UsersClientFailureKind.Unauthorized` | Error classification |
| Client failure — HTTP 403 | Forbidden maps to permission failure | Users Client | Mock handler returning 403 | `UsersClientFailureKind.Forbidden` | Error classification |
| Client failure — HTTP 404 | Not found maps to missing resource | Users Client | Mock handler returning 404 | `UsersClientFailureKind.NotFound` | Error classification |
| Client failure — HTTP 409 | Conflict maps to conflict | Users Client | Mock handler returning 409 | `UsersClientFailureKind.Conflict` | Concurrency handling |
| Client failure — timeout | Timeout maps to timeout | Users Client | Mock handler returning 408/504 | `UsersClientFailureKind.Timeout` | Timeout handling |
| Client failure — cancellation | Cancellation maps to cancelled | Users Client | Mock handler throwing `OperationCanceledException` | `UsersClientFailureKind.Cancelled` | Cancellation handling |
| Client failure — transport error | `HttpRequestException` maps to transport failure | Users Client | Mock handler throwing `HttpRequestException` | `UsersClientFailureKind.TransportFailure` | Network failure handling |
| Client failure — invalid JSON | Malformed response maps to deserialization failure | Users Client | Mock handler returning invalid JSON | `UsersClientFailureKind.DeserializationFailure` | Transport resilience |
| Client failure — unexpected error | Unrecognized status maps to unknown | Users Client | Mock handler returning 500 | `UsersClientFailureKind.Unknown` | Fallback handling |
| API mapping — User to UserSummaryResponse | Domain user maps without persistence fields | Users Api mapper | Domain `User` entity | `UserSummaryResponse` with only safe fields | Contract isolation |
| Static boundary — Interface project references | Interface references only Client + Contracts | `Users.Interface.csproj` | Static analysis | No backend references | ADR-0003 enforcement |
| Static boundary — Interface source imports | No backend namespace imports | `src/Modules/Users/Interface/**/*.cs` | Static analysis | No backend imports | ADR-0003 enforcement |

## 5. Suggested Unit Tests

| Test | Behavior protected | Target | Notes |
|---|---|---|---|
| Client — happy path returns data | Successful responses deserialize correctly | Users Client | Mock handler returning valid JSON |
| API mapping — enum stability | User-related enums serialize correctly | Users Contracts enums | Prevent drift |
| Route constants match client routes | Route strings are consistent | `UsersApiRoutes` vs Client | Prevent route drift |

## 6. Required Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Trigger | Expected evidence | Execution profile |
|---|---|---|---|---|---|
| Create or link user from UI | Multi-request create/check/email-update flow | Running host with auth and Users API | UI create/find/create flow | Success or structured conflict/validation message | Semi-automated, pre-release |
| Tenant role assignment on link | Linked user receives correct tenant role | Running host with tenant context and auth | Link user to tenant | User has tenant role once; repeated link idempotent | Semi-automated, pre-release |
| Network cancellation during search/create | Canceled operations don't leak or mislead | Browser/component harness with delayed API | Cancel mid-flight operation | Operation canceled cleanly, no stale state | Semi-automated, pre-release |

## 7. Suggested Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Reason suggested |
|---|---|---|---|
| Multi-tenant user isolation | Users from tenant A not visible in tenant B | Multi-tenant host | Security-sensitive, needs multi-tenant setup |
| WebAuthn credential flow | Passkey registration and authentication | Running host + browser | Hardware-dependent |

## 8. Deferred or Rejected Tests

| Test/scenario | Decision | Reason |
|---|---|---|
| Profile/account page tests | Deferred | Follow-up migration slice |
| Personal access token tests | Deferred | Not in current migration scope |
| Temporary auto-auth link tests | Deferred | Not in current migration scope |
| ASP.NET Identity integration | Deferred | Requires real database |

## 9. Test Task Derivation

| Task | Source | Type | Requirement | Target suite | Notes |
|---|---|---|---|---|---|
| Contract serialization tests | §4 contract rows | Contract | Required | `Users.Tests.Contracts` | Round-trip all DTOs |
| Client route construction tests | §4 route rows | Unit | Required | `Users.Tests.Client` | 6 route tests |
| Client failure mapping tests | §4 failure rows | Unit | Required | `Users.Tests.Client` | 9 failure paths |
| API mapping tests | §4 API rows | Unit | Required | `Users.Tests.Api` | User→Response mapping |
| Static boundary tests | §4 static rows | Regression | Required | `Users.Tests.Boundary` | Project refs and imports |

## 10. Existing Coverage / Gaps

No test code exists in the working tree. All tests are created fresh based on this plan.

**Coverage targets by layer:**

| Layer | Classes under test | Planned test classes | Coverage approach |
|---|---|---|---|
| Contracts | Request/response DTOs | 1 (SerializationTests) | Round-trip JSON |
| Client | HTTP client, routes, failures | 1 (UsersClientTests) | Mock HttpMessageHandler |
| Api | User→Response mapping | 1 (UsersApiMappingTests) | Mock mapper |
| Boundary | Project references, imports | 1 (BoundaryValidationTests) | Static analysis |

## 11. CI / Execution Guidance

| Suite | Execution mode | Command/procedure | Required environment |
|---|---|---|---|
| Contract serialization | Automated, every PR | `dotnet test tests/Modules/Users.Tests/ --filter "Category=Contract"` | None |
| Client tests | Automated, every PR | `dotnet test tests/Modules/Users.Tests/ --filter "Category=Client"` | None |
| API mapping | Automated, every PR | `dotnet test tests/Modules/Users.Tests/ --filter "Category=Api"` | None |
| Static boundary | Automated, every PR | `python3 tools/architecture/validate_interface_boundaries.py` | None |
| Full test suite | Automated, every PR | `dotnet test tests/Modules/Users.Tests/` | None |
| Hot scenarios | Semi-automated, pre-release | Manual execution against host with auth | Host with Users API |

## 12. Traceability

| Test area | Linked ADR | Linked journey stage | Linked code path |
|---|---|---|---|
| Contract serialization | ADR-0003 (frontend-safe contracts) | — | `src/Modules/Users/Contracts/**/*.cs` |
| Client route/failure | ADR-0003 (Client layer) | — | `src/Modules/Users/Client/**/*.cs` |
| API mapping | ADR-0004 (protected endpoints) | — | `src/Modules/Users/Api/**/*.cs` |
| Static boundary | ADR-0003 (isolation) | — | `src/Modules/Users/Interface/Users.Interface.csproj` |
