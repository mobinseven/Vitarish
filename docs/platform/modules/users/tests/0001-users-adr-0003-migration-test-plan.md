# Users ADR-0003 Migration Test Plan

Date: 2026-06-26
Status: Draft
App: Platform
Module: Users
Capability: Users Interface API/client/contracts migration
Owner: Users module
Related ADRs: ADR-0003
Source code/files: `src/Modules/Users/Interface`, `src/Modules/Users/Client`, `src/Modules/Users/Contracts`, `src/Modules/Users/Api`
Confidence: Medium

## Objective

Protect the first Users ADR-0003 migration slice: the Users Interface create/find/find-or-create components must use frontend-safe Users Client and Contracts instead of backend Application, Domain, Infrastructure, or Api artifacts.

## Scope

Required in this slice: user search, get by id, email/phone existence checks, role membership check, create by phone/email, set email, and add role assignment endpoints used by current Interface components.

Deferred follow-up capabilities: profile/account pages, claims UI beyond safe wire shapes, personal access tokens, temporary auto-auth links, full WebAuthn HTTP mapping migration, and host server identity-page cleanup.

## Required Unit and Contract Tests

| Level | Target | Behavior protected | Requirement |
| --- | --- | --- | --- |
| Contract | Users Contracts serialization | Request/response DTOs serialize only safe public fields and do not expose domain/Identity types. | Required |
| Unit | Users Client route construction | Search/check/get/create/set-email/add-role methods build `UsersApiRoutes` URLs and query strings correctly. | Required |
| Unit | Users Client failure mapping | 400, 401, 403, 404, 409, timeout, cancellation, transport, and deserialization failures map to `UsersClientFailureKind`. | Required |
| Unit | Users Api mapping | Domain `User` maps to `UserSummaryResponse` without persistence/internal fields. | Required |
| Regression | ADR-0003 boundary validation | Users Interface has no direct Application/Domain/Infrastructure/Api imports or project references. | Required |

## Required Hot Test Scenarios

| Scenario | Risk protected | Environment | Expected evidence | Requirement |
| --- | --- | --- | --- | --- |
| Create or link user from UI | Multi-request create/check/email-update flow misreports conflicts or validation failures. | Running host with auth and Users API. | UI shows success or structured conflict/validation message; backend creates/links the expected user. | Suggested before release |
| Tenant role assignment on link | Linked user misses required tenant role or UI constructs an invalid backend role. | Running host with tenant context and auth. | User receives tenant role once; repeated link is idempotent from the UI perspective. | Suggested before release |
| Network cancellation during search/create | Canceled UI operations show misleading backend errors or leak pending requests. | Browser or component harness with delayed Users API responses. | Operation is canceled quietly or as a cancellation state; no stale result overwrites newer UI state. | Suggested before release |

## CI / Execution Guidance

Run:

```bash
python3 tools/architecture/validate_interface_boundaries.py
dotnet test tests/Modules/Users.Tests/Users.Tests.csproj
```

Add focused client/API mapper tests to `tests/Modules/Users.Tests` as the next implementation task.
