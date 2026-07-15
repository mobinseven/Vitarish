# Appointments Boundary Migration Test Plan

Date: 2026-06-28
Status: Draft
App: Main
Module: Appointments
Capability: ADR-0003 frontend/backend isolation and ADR-0004 protected module API
Owner: Appointments module
Related journey: —
Related roadmap: —
Related ADRs: ADR-0003, ADR-0004
Related tasks: —
Source code/files: `src/Modules/Appointments/Domain`, `src/Modules/Appointments/Application`, `src/Modules/Appointments/Contracts`, `src/Modules/Appointments/Client`, `src/Modules/Appointments/Api`, `src/Modules/Appointments/Interface`
Confidence: Medium

## 1. Objective

Protect the Appointments module against regression in frontend/backend isolation, contract serialization, client failure mapping, API endpoint mapping, and domain validation rules. Appointments is a direct Clinic dependency — correctness of booking, shift, and reservation flows directly impacts the active product.

## 2. Scope

### In Scope

- Contract serialization: appointment command requests, query requests, read DTOs, result/error DTOs, enums, `DateOnly`, `TimeOnly`, `TimeSpan`, UTC temporary hold expiry instants.
- Client failure mapping: validation, unauthorized, forbidden, not found, conflict/stale state, timeout, cancellation, transport, deserialization, and unexpected failures.
- API mapping: contract-to-Application command/query adaptation, Application failures to structured contract errors, protected endpoint group usage.
- Domain rules: `ShiftBookingPolicyRules`, `ShiftReminderPolicyRules`, `ShiftEstimatedSessionLengthCalculator`.
- Static boundary: `Appointments.Interface` references only Client + Contracts.

### Out of Scope

- Full Blazor UI component tests (deferred until client/API endpoint coverage complete).
- Infrastructure integration tests (database, MediatR pipeline, background jobs).
- Hot scenarios requiring real multi-tenant host with seeded data.

## 3. Code / Risk Analysis

| Code area | Risk trigger | Why it matters | Test response |
|---|---|---|---|
| `Appointments.Interface` project references | May reference Application/Domain/Infrastructure/Api | Frontend layer breaks isolation rule | Static boundary assertion |
| Contract serialization | 29 request records, 23 DTO types, 5 enums | Lost fields or wrong enum values break API consumers | Round-trip serialization tests |
| Client failure mapping | 10+ failure paths from HTTP to `AppointmentsErrorCode` | Wrong mapping hides real errors | Theory-based failure mapping tests |
| API endpoint mapping | Contracts → Application binding | Backend types in contract shapes violate isolation | Representative endpoint mapping tests |
| `ShiftBookingPolicyRules` | Validates policy writes against shift day-of-week | Invalid policy data corrupts booking logic | Unit test validation rules |
| `ShiftReminderPolicyRules` | Validates reminder templates and offsets | Empty templates or zero offsets break reminders | Unit test validation rules |
| `ShiftEstimatedSessionLengthCalculator` | Pure math deriving session length | Off-by-one or division errors | Unit test calculation |
| `AppointmentsErrorCode` enum | 16 error codes used by client and API | Missing codes break error handling | Enum coverage test |

## 4. Required Unit Tests

| Test | Behavior protected | Target | Input / edge case | Expected result | Requirement reason |
|---|---|---|---|---|---|
| Contract serialization — request round-trip | All 29 request records preserve fields through JSON | `AppointmentsRequests.cs` types | Representative data per request type | All fields preserved | API contract stability |
| Contract serialization — DTO round-trip | All read/write DTOs preserve fields through JSON | `Dto/*.cs` types | Representative data per DTO | All fields preserved | UI data fidelity |
| Contract serialization — result/error round-trip | `AppointmentsCommandResult`, `AppointmentsQueryResult<T>`, `AppointmentsError` preserve structure | `AppointmentsResultContracts.cs` | Success and error variants | Envelope and error structure preserved | UI error handling |
| Contract serialization — enum stability | All 5 enums serialize with correct JSON values | `Enums/*.cs` | All enum values | Correct string/int values | Client error classification |
| Contract serialization — DateOnly/TimeOnly/TimeSpan | Date and time types round-trip correctly | Request/DTO types with date fields | Representative date/time values | No precision loss, correct serialization | Scheduling accuracy |
| Client failure — HTTP 400 maps to validation | Bad request returns validation error | Appointments Client | Mock handler returning 400 | `AppointmentsErrorCode.ValidationFailure` | Error classification |
| Client failure — HTTP 401 maps to unauthorized | Unauthorized returns auth error | Appointments Client | Mock handler returning 401 | `AppointmentsErrorCode.Unauthorized` | Error classification |
| Client failure — HTTP 403 maps to forbidden | Forbidden returns permission error | Appointments Client | Mock handler returning 403 | `AppointmentsErrorCode.Forbidden` | Error classification |
| Client failure — HTTP 404 maps to not found | Not found returns missing resource error | Appointments Client | Mock handler returning 404 | `AppointmentsErrorCode.NotFound` | Error classification |
| Client failure — HTTP 409 maps to conflict | Conflict returns stale state error | Appointments Client | Mock handler returning 409 | `AppointmentsErrorCode.Conflict` | Concurrency handling |
| Client failure — HTTP 408/504 maps to timeout | Timeout returns timeout error | Appointments Client | Mock handler returning 408 or 504 | `AppointmentsErrorCode.Timeout` | Timeout handling |
| Client failure — cancellation maps to cancelled | Caller cancellation returns cancelled error | Appointments Client | Mock handler throwing `OperationCanceledException` | `AppointmentsErrorCode.Cancelled` | Cancellation handling |
| Client failure — invalid JSON maps to deserialization | Malformed response body returns parse error | Appointments Client | Mock handler returning invalid JSON | `AppointmentsErrorCode.DeserializationFailure` | Transport resilience |
| Client failure — transport error maps to transport failure | `HttpRequestException` returns transport error | Appointments Client | Mock handler throwing `HttpRequestException` | `AppointmentsErrorCode.TransportFailure` | Network failure handling |
| Client failure — unexpected error maps to unknown | Unrecognized status returns unknown error | Appointments Client | Mock handler returning 500 | `AppointmentsErrorCode.Unknown` | Fallback error handling |
| Client route construction — bookable queries | Bookable date/slot queries use correct routes | Appointments Client | `GetBookableDatesAsync`, `GetBookableSlotsAsync` | GET with expected URL paths and query params | Request correctness |
| API mapping — representative endpoints | API endpoints bind contract shapes only | Appointments Api | Representative endpoints (list, create, update, bookable queries) | Contracts → Application inputs, no backend types | Transport isolation |
| API mapping — error mapping | Application failures map to correct contract errors | Appointments Api | Validation, unauthorized, forbidden, not found, conflict failures | Correct `AppointmentsError` body | Error contract correctness |
| Static boundary — Interface project references | `Appointments.Interface.csproj` references only Client + Contracts | `Appointments.Interface.csproj` | Static analysis | No Application/Domain/Infrastructure/Api references | ADR-0003 enforcement |
| Static boundary — Interface source imports | No backend namespace imports in Interface source | `src/Modules/Appointments/Interface/**/*.cs` | Static analysis | No backend namespace imports | ADR-0003 enforcement |
| Policy rules — booking policy validates day-of-week | Policy date must match shift's day-of-week | `ShiftBookingPolicyRules.ValidateWrite` | Policy for Tuesday on a Wednesday shift | Validation error | Data integrity |
| Policy rules — booking policy no duplicate dates | Cannot create two policies for same shift+date | `ShiftBookingPolicyRules.ValidateWrite` | Duplicate date for same shift | Validation error | Data integrity |
| Policy rules — reminder policy validates offset | Reminder offset must be positive | `ShiftReminderPolicyRules.Validate` | Offset of 0 or negative | Validation error | Reminder correctness |
| Policy rules — reminder policy validates templates | Subject and body templates must not be blank | `ShiftReminderPolicyRules.Validate` | Empty subject template | Validation error | Reminder content |
| Session length calculator | Calculates per-reservation duration correctly | `ShiftEstimatedSessionLengthCalculator.Calculate` | 60-min shift, 4 reservations | Returns 15 minutes per reservation | Scheduling math |
| Session length calculator — single reservation | Single reservation gets full shift length | `ShiftEstimatedSessionLengthCalculator.Calculate` | 60-min shift, 1 reservation | Returns 60 minutes | Edge case |
| AppointmentsErrorCode enum — all values covered | Every error code maps correctly | `AppointmentsErrorCode` | All 16 values | Each value serializes and deserializes correctly | Error handling completeness |

## 5. Suggested Unit Tests

| Test | Behavior protected | Target | Notes |
|---|---|---|---|
| Client — successful query returns data | Happy path for bookable dates/slots | `AppointmentsClient` | Mock handler returning valid JSON |
| Client — `GetValueOrDefault` helper | Convenience extension works for success and failure | `EntityResultClientExtensions` | Test both branches |
| API route constants match client routes | Route strings are consistent | `AppointmentsApiRoutes` vs Client methods | Prevent route drift |
| Command record equality | MediatR command records have value equality | `CreateReservationCommand`, `MoveReservationCommand` | Ensure correct dispatch |

## 6. Required Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Trigger | Expected evidence | Execution profile |
|---|---|---|---|---|---|
| Simultaneous booking capacity conflict | Two users booking last slot — only one succeeds | Multi-tenant host with seeded shift and capacity=1 | Two concurrent `CreateReservationCommand` for same shift+date | One returns success, other returns conflict/stale-state error | Semi-automated, pre-release |
| Temporary hold expiry | Hold expires and confirmation is rejected | Host with hold support | Create hold, wait past expiry, attempt confirm | Confirmation rejected, no reservation created | Manual, pre-release |
| Reminder policy authorization | Only authorized principals mutate reminder state | Host with test users in different roles | Create/update/delete reminder policy from unauthorized, provider, tenant-admin contexts | Only tenant-admin succeeds; others receive forbidden | Semi-automated, pre-release |
| Tenant isolation | Cross-tenant reads and commands are rejected | Multi-tenant host with two seeded tenants | Tenant A queries/commands Tenant B reservations, shifts, services, providers, policies | Returns 403/404, no data leaked | Semi-automated, pre-release |

## 7. Suggested Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Reason suggested |
|---|---|---|---|
| Booking flow end-to-end | Complete reservation lifecycle from search to confirm | Running host + browser | High-value e2e, needs real UI interaction |
| Shift lifecycle management | Create, activate, pause, close, cancel, archive | Running host | Validates state machine in integration |
| Provider user linking | Link/unlink user accounts to providers | Running host with auth | Multi-user scenario |

## 8. Deferred or Rejected Tests

| Test/scenario | Decision | Reason |
|---|---|---|
| Full Blazor UI component tests | Deferred | Endpoint coverage incomplete; revisit after client migration |
| MediatR pipeline integration tests | Deferred | Requires full DI container and database |
| Background reminder job tests | Deferred | Requires time-based scheduling infrastructure |
| Capacity conflict stress tests | Deferred | Needs concurrent load generation tooling |

## 9. Test Task Derivation

| Task | Source | Type | Requirement | Target suite | Notes |
|---|---|---|---|---|---|
| Contract serialization tests | §4 contract rows | Contract | Required | `Appointments.Tests.Contracts` | Round-trip all DTOs, requests, results |
| Client failure mapping tests | §4 client failure rows | Unit | Required | `Appointments.Tests.Client` | 10 failure paths with mocked handler |
| Client route construction tests | §4 client route rows | Unit | Required | `Appointments.Tests.Client` | Verify URL paths and query params |
| API mapping tests | §4 API rows | Unit | Required | `Appointments.Tests.Api` | Representative endpoints |
| Domain policy rule tests | §4 policy rows | Unit | Required | `Appointments.Tests.Domain` | Booking and reminder policy validation |
| Session length calculator tests | §4 calculator rows | Unit | Required | `Appointments.Tests.Domain` | Pure math calculation |
| Static boundary tests | §4 static rows | Regression | Required | `Appointments.Tests.Boundary` | Project references and source imports |

## 10. Existing Coverage / Gaps

No test code exists in the working tree. All tests are created fresh based on this plan.

**Coverage targets by layer:**

| Layer | Classes under test | Planned test classes | Coverage approach |
|---|---|---|---|
| Domain | Policy rules, session calculator | 2 (PolicyRulesTests, SessionLengthTests) | Pure unit, no mocks |
| Contracts | 29 requests, 23 DTOs, 5 enums, results | 1 (SerializationTests) | Round-trip JSON |
| Client | HTTP client, failure mapping, routes | 1 (AppointmentsClientTests) | Mock HttpMessageHandler |
| Api | Endpoint mapping, error mapping | 1 (AppointmentsApiMappingTests) | Mock Application services |
| Boundary | Project references, source imports | 1 (BoundaryValidationTests) | Static analysis |

## 11. CI / Execution Guidance

| Suite | Execution mode | Command/procedure | Required environment |
|---|---|---|---|
| Contract serialization | Automated, every PR | `dotnet test tests/Modules/Appointments.Tests/ --filter "Category=Contract"` | None |
| Client failure mapping | Automated, every PR | `dotnet test tests/Modules/Appointments.Tests/ --filter "Category=Client"` | None |
| Domain rules | Automated, every PR | `dotnet test tests/Modules/Appointments.Tests/ --filter "Category=Domain"` | None |
| API mapping | Automated, every PR | `dotnet test tests/Modules/Appointments.Tests/ --filter "Category=Api"` | None |
| Full test suite | Automated, every PR | `dotnet test tests/Modules/Appointments.Tests/` | None |
| Hot scenarios | Semi-automated, pre-release | Manual execution against multi-tenant host | Multi-tenant host with auth |

## 12. Traceability

| Test area | Linked ADR | Linked journey stage | Linked code path |
|---|---|---|---|
| Contract serialization | ADR-0003 (frontend-safe contracts) | — | `src/Modules/Appointments/Contracts/**/*.cs` |
| Client failure mapping | ADR-0003 (Client layer) | — | `src/Modules/Appointments/Client/**/*.cs` |
| API mapping | ADR-0004 (protected endpoints) | — | `src/Modules/Appointments/Api/**/*.cs` |
| Static boundary | ADR-0003 (isolation) | — | `src/Modules/Appointments/Interface/Appointments.Interface.csproj` |
| Domain policy rules | — | — | `src/Modules/Appointments/Domain/Rules/*.cs` |
| Session length calculator | — | — | `src/Modules/Appointments/Domain/Services/ShiftEstimatedSessionLengthCalculator.cs` |
