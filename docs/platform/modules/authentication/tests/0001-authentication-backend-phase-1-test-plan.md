# Authentication Backend Phase 1 Test Plan

Date: 2026-06-28
Status: Draft
App: Main
Module: Authentication
Capability: Backend authentication API, orchestration seams, domain rules, and OpenAPI contract
Owner: Authentication module
Related journey: —
Related roadmap: —
Related ADRs: ADR-0003, ADR-0004, ADR-0005
Related tasks: —
Source code/files: `src/Modules/Authentication/Domain`, `src/Modules/Authentication/Application`, `src/Modules/Authentication/Api`, `src/Modules/Authentication/Contracts`, `src/Modules/Authentication/Infrastructure`
Confidence: High

## 1. Objective

Protect the authentication backend against regression in domain security rules, handler orchestration, endpoint mapping, contract serialization, and ProblemDetails error mapping. Authentication is the foundation every request depends on — failures here compromise the entire system.

## 2. Scope

### In Scope

- Domain challenge lifecycle: creation, expiry, reuse prevention, attempt exhaustion, completion, cancellation.
- Domain policies: `LoginAttemptPolicy`, `VerificationCodePolicy`, `ReturnUrlPolicy`.
- Value object guards: `AuthenticationUserId`, `AuthenticationChallengeId`.
- Application handlers: password login, email/phone/passkey begin+complete, logout, session query, logout context.
- `ChallengeCompletionHelper` shared OTP flow: code normalization (Arabic/Persian digits), failure registration, challenge state transitions.
- `AuthenticationInputValidator`: `HasText` and `SafeReturnUrl`.
- API mapping: `AuthenticationEndpointMapper` (domain→contract), `AuthenticationProblemHttpMapper` (failure→HTTP status).
- Contract serialization: request/response DTOs round-trip through System.Text.Json.

### Out of Scope

- React authentication pages, client hooks, browser WebAuthn interop (deferred to frontend migration).
- Infrastructure integration tests (ASP.NET Identity, Duende IdentityServer, distributed cache, Fido2NetLib).
- Hot scenarios requiring real database, browser, SMS provider, or email service.
- Legacy Blazor/Razor UI deletion.

## 3. Code / Risk Analysis

| Code area | Risk trigger | Why it matters | Test response |
|---|---|---|---|
| `AuthenticationChallenge` | State machine with 5 states, guards on every transition | Wrong transition allows reuse or skips expiry check | Unit tests for every method and guard |
| `AuthenticationChallengePolicy` | Creates challenges with lifetime and attempt limits | Weak defaults weaken security | Unit test default values and challenge creation |
| `ReturnUrlPolicy` | Accepts/rejects URL strings | Open redirect vulnerability if logic fails | Unit tests for protocol-relative, backslash, external URLs |
| `LoginAttemptPolicy` | Threshold check for failed attempts | Off-by-one allows extra login attempts | Unit test boundary values |
| `VerificationCodePolicy` | Format validation of OTP codes | Accepting non-numeric or wrong-length codes weakens OTP | Unit test format edge cases |
| `ChallengeCompletionHelper` | Shared OTP completion, Arabic digit normalization | Incorrect normalization rejects valid codes; missing failure registration allows retry after exhaustion | Unit test normalization, failure flow, completion flow |
| `AuthenticationProblemHttpMapper` | Maps 16 failure reasons to HTTP status codes | Wrong status leaks internal state or misleads clients | Unit test all 16 mappings |
| `AuthenticationEndpointMapper` | Maps domain results to DTOs | Missing fields or wrong enum values break client contract | Unit test all 3 mapping methods |
| `LoginWithPasswordHandler` | 7 `PasswordSignInStatus` branches | Unhandled status returns incorrect result | Unit test every branch |
| `BeginEmailLoginHandler` | User creation, code generation, challenge persistence, send failure rollback | Partial failure leaves inconsistent state | Unit test happy path, validation, send failure |
| Contracts DTOs | Serialization round-trip | Lost fields or type mismatches break API consumers | Round-trip serialization tests |

## 4. Required Unit Tests

| Test | Behavior protected | Target | Input / edge case | Expected result | Requirement reason |
|---|---|---|---|---|---|
| Challenge creation enforces minimum attempts | Challenge cannot be created with maxAttempts < 1 | `AuthenticationChallenge` constructor | `maximumAttempts: 0` | Throws `ArgumentOutOfRangeException` | Domain integrity |
| Challenge expiry detection | Expired challenge is rejected | `AuthenticationChallenge.IsExpired(now)` | `ExpiresAt` in the past | Returns `true` | Prevent use of stale challenges |
| Challenge attempt exhaustion | Exhausted challenge cannot receive attempts | `AuthenticationChallenge.CanAttempt(now)` | `FailedAttempts >= MaximumAttempts` | Returns `false` | Enforce attempt limits |
| Challenge reuse prevention | Completed challenge cannot be attempted again | `AuthenticationChallenge.CanAttempt(now)` with `Status == Completed` | Any timestamp | Returns `false` | Prevent code reuse |
| Challenge completion transitions state | Completing a valid challenge marks it Completed | `AuthenticationChallenge.Complete(now)` | Valid pending challenge | `Status == Completed`, `CompletedAt` set | State machine correctness |
| Challenge failure registration | Failed attempt increments counter | `AuthenticationChallenge.RegisterFailure(now)` | `FailedAttempts` at 0 | `FailedAttempts == 1` | Track failed attempts |
| Challenge cancellation | Cancelled challenge cannot be attempted | `AuthenticationChallenge.Cancel()` then `CanAttempt(now)` | Any state except Completed | `Status == Cancelled`, `CanAttempt` returns `false` | Cleanup on send failure |
| Remaining attempts decrements | Remaining attempts reflects failures | `AuthenticationChallenge.RemainingAttempts` | `MaximumAttempts: 5`, `FailedAttempts: 3` | Returns `2` | UI feedback accuracy |
| Challenge policy default values | Default policy has secure defaults | `AuthenticationChallengePolicy.Default` | — | `Lifetime == 5 min`, `MaximumAttempts == 5` | Security baseline |
| Challenge policy creates with correct purpose | Created challenge has matching purpose and token | `CreatePhoneLoginChallenge(userId, dest)` | Any user/destination | `Purpose == PhoneLogin`, `TokenPurpose` set | Correct challenge routing |
| Login attempt policy exhaustion | Policy detects threshold | `LoginAttemptPolicy.IsExhausted(failed)` | `failedAttempts: 5`, `max: 5` | Returns `true` | Lockout enforcement |
| Login attempt policy below threshold | Below limit is not exhausted | `LoginAttemptPolicy.IsExhausted(failed)` | `failedAttempts: 4`, `max: 5` | Returns `false` | Allow remaining attempts |
| Verification code valid format | Correct length numeric code passes | `VerificationCodePolicy.IsValidFormat(code)` | `"123456"`, `RequiredLength: 6` | Returns `true` | Accept valid OTP |
| Verification code wrong length | Non-matching length fails | `VerificationCodePolicy.IsValidFormat(code)` | `"12345"`, `RequiredLength: 6` | Returns `false` | Reject short codes |
| Verification code non-numeric | Non-digit characters fail | `VerificationCodePolicy.IsValidFormat(code)` | `"12ab34"` | Returns `false` | Reject malformed input |
| Verification code null/empty | Null or empty fails | `VerificationCodePolicy.IsValidFormat(code)` | `null`, `""` | Returns `false` | Guard empty input |
| Return URL accepts safe local path | Relative path is accepted | `ReturnUrlPolicy.IsSafeLocal(url)` | `"/dashboard"` | Returns `true` | Allow normal navigation |
| Return URL rejects protocol-relative | Protocol-relative path is rejected | `ReturnUrlPolicy.IsSafeLocal(url)` | `"//evil.com"` | Returns `false` | Prevent open redirect |
| Return URL rejects backslash-relative | Backslash-relative path is rejected | `ReturnUrlPolicy.IsSafeLocal(url)` | `"\\evil.com"` | Returns `false` | Prevent open redirect |
| Return URL rejects external URL | Full external URL is rejected | `ReturnUrlPolicy.IsSafeLocal(url)` | `"https://evil.com"` | Returns `false` | Prevent external redirect |
| Return URL normalizes or defaults | Null URL returns default | `ReturnUrlPolicy.NormalizeOrDefault(url, default)` | `null`, default `"/"` | Returns `"/"` | Safe fallback |
| UserId Create guards empty | Empty string throws | `AuthenticationUserId.Create("")` | `""` | Throws `ArgumentException` | Value object integrity |
| ChallengeId Create guards empty | Empty string throws | `AuthenticationChallengeId.Create("")` | `""` | Throws `ArgumentException` | Value object integrity |
| Input validator HasText | Whitespace-only returns false | `AuthenticationInputValidator.HasText("  ")` | `"  "` | Returns `false` | Reject blank input |
| Input validator SafeReturnUrl | Null returns default | `AuthenticationInputValidator.SafeReturnUrl(null)` | `null` | Returns `"/"` | Safe fallback |
| Password handler success | Valid credentials return redirect | `LoginWithPasswordHandler.HandleAsync` | Valid login, `PasswordSignInStatus.Succeeded` | `Success(LoginResult)` with redirect URL | Happy path |
| Password handler invalid credentials | Wrong password maps to failure | `LoginWithPasswordHandler.HandleAsync` | `PasswordSignInStatus.InvalidCredentials` | `Failure(InvalidCredentials)` | Error mapping |
| Password handler locked out | Locked account returns locked status | `LoginWithPasswordHandler.HandleAsync` | `PasswordSignInStatus.LockedOut` | `Failure(LockedOut)` | Lockout enforcement |
| Password handler requires 2FA | 2FA required returns additional step | `LoginWithPasswordHandler.HandleAsync` | `PasswordSignInStatus.RequiresTwoFactor` | `Failure(AdditionalStepRequired)` | Multi-factor flow |
| Begin email handler validates input | Empty email returns failure | `BeginEmailLoginHandler.HandleAsync` | `Email: ""` | `Failure(InvalidInput)` | Input validation |
| Begin email handler success | Valid email creates challenge and sends code | `BeginEmailLoginHandler.HandleAsync` | Valid email, user exists | `Success(ChallengeResult)` with challenge ID | Happy path |
| Begin email handler send failure rollback | Failed send cancels challenge | `BeginEmailLoginHandler.HandleAsync` | Code sender throws | Challenge is `Cancelled`, failure returned | Partial failure cleanup |
| Begin phone handler mirrors email | Same pattern as email | `BeginPhoneLoginHandler.HandleAsync` | Valid phone | `Success(ChallengeResult)` | Symmetry with email flow |
| Complete challenge helper rejects expired | Expired challenge returns failure | `ChallengeCompletionHelper.CompleteChallengeAsync` | Challenge past expiry | `Failure(ChallengeExpired)` | Stale code rejection |
| Complete challenge helper rejects exhausted | Exhausted attempts return failure | `ChallengeCompletionHelper.CompleteChallengeAsync` | `FailedAttempts >= MaximumAttempts` | `Failure(ChallengeAttemptsExhausted)` | Attempt limit |
| Complete challenge helper rejects wrong code | Incorrect code registers failure | `ChallengeCompletionHelper.CompleteChallengeAsync` | Wrong verification code | `Failure(InvalidVerificationCode)`, `FailedAttempts` incremented | Code verification |
| Complete challenge helper normalizes Arabic digits | Arabic digit OTP is accepted | `ChallengeCompletionHelper.NormalizeVerificationCode(code)` | Arabic-Indic digits | Normalized to ASCII digits | Locale support |
| Complete challenge helper normalizes Persian digits | Persian digit OTP is accepted | `ChallengeCompletionHelper.NormalizeVerificationCode(code)` | Extended Arabic-Indic digits | Normalized to ASCII digits | Locale support |
| Complete challenge helper success | Valid code completes challenge and signs in | `ChallengeCompletionHelper.CompleteChallengeAsync` | Correct code, pending challenge | `Success(LoginResult)`, challenge `Completed` | Happy path |
| Problem mapper maps all 16 reasons | Each failure reason maps to correct HTTP status | `AuthenticationProblemHttpMapper.ToProblem(failure)` | All 16 `AuthenticationFailureReason` values | Correct status code per reason | Error contract correctness |
| Endpoint mapper challenge result | Domain `ChallengeResult` maps to DTO | `AuthenticationEndpointMapper.ToDto(challengeResult)` | Valid `ChallengeResult` | All fields transferred, enum mapped | DTO fidelity |
| Endpoint mapper login result | Domain `LoginResult` maps to DTO | `AuthenticationEndpointMapper.ToDto(loginResult)` | Valid `LoginResult` | Return URL, UserId, RequiresProfileCompletion transferred | DTO fidelity |
| Endpoint mapper logout result | Domain `LogoutResult` maps to DTO | `AuthenticationEndpointMapper.ToDto(logoutResult)` | Valid `LogoutResult` | SignedOut, RedirectUrl transferred | DTO fidelity |
| Contract serialization redirect DTO | Round-trip preserves all fields | `AuthenticationRedirectResultDto` JSON round-trip | Representative data | All fields preserved | API contract stability |
| Contract serialization challenge DTO | Round-trip preserves all fields | `AuthenticationChallengeDto` JSON round-trip | Representative data | All fields preserved, enum correct | API contract stability |
| Contract serialization session DTO | Round-trip preserves all fields | `AuthenticationSessionDto` JSON round-trip | `IsAuthenticated: true` with user | All fields preserved | API contract stability |
| Contract serialization problem DTO | Round-trip preserves all fields | `AuthenticationProblemDto` JSON round-trip | Representative error | Code, Title, Detail, Status preserved | Error contract stability |

## 5. Suggested Unit Tests

| Test | Behavior protected | Target | Notes |
|---|---|---|---|
| Challenge Restore factory | Reconstitutes challenge from persisted state | `AuthenticationChallenge.Restore(...)` | Ensures persistence round-trip |
| Begin passkey handler success | Maps WebAuthn begin result to ChallengeResult | `BeginPasskeyLoginHandler.HandleAsync` | Depends on mocked `IWebAuthnAuthenticationService` |
| Complete passkey handler success | Passkey completion triggers sign-in | `CompletePasskeyLoginHandler.HandleAsync` | Depends on WebAuthn + SignIn services |
| Complete passkey handler failure | Failed passkey verification returns failure | `CompletePasskeyLoginHandler.HandleAsync` | Invalid credential path |
| Logout handler with logout ID | Signs out and resolves redirect | `LogoutHandler.HandleAsync` | Depends on `IAuthenticationSignInService` + `ILogoutContextService` |
| Logout handler without logout ID | Signs out without redirect lookup | `LogoutHandler.HandleAsync` | `LogoutId: null` path |
| Get session handler | Returns current user info | `GetCurrentAuthenticationSessionHandler.HandleAsync` | Depends on `IAuthenticationUserDirectory` |
| Get logout context handler | Returns context by ID | `GetLogoutContextHandler.HandleAsync` | Depends on `ILogoutContextService` |
| Begin email handler user creation | New user is created during begin flow | `BeginEmailLoginHandler.HandleAsync` | Email not in directory, user auto-created |
| InMemory challenge store CRUD | Save, get, replace work correctly | `InMemoryAuthenticationChallengeStore` | Direct unit test of infrastructure |
| SMS request shield rate limiting | Excess requests within window are blocked | `InMemoryVerificationSmsRequestShield.CanSend` | 4th request in 2-min window returns false |
| SMS request shield window expiry | Requests after window expires are allowed | `InMemoryVerificationSmsRequestShield.CanSend` | Request after 2-min gap returns true |
| Contract enum stability | DTO enums match domain enums 1:1 | `AuthenticationMethodDto`, `AuthenticationChallengePurposeDto` | Prevent accidental drift |

## 6. Required Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Trigger | Expected evidence | Execution profile |
|---|---|---|---|---|---|
| Antiforgery token flow | State-changing request without valid XSRF token is rejected | Running host with antiforgery middleware | GET `/antiforgery`, then POST with/without `X-XSRF-TOKEN` header | 400 without header, success with header; identity not mutated during token retrieval | Semi-automated, pre-release |
| Cookie/session compatibility | Authentication cookie, session, and antiforgery interact correctly across IdentityServer and ASP.NET middleware | Running host with full auth middleware stack | Login → access protected endpoint → logout → access protected endpoint | Session established after login, cleared after logout, antiforgery valid throughout | Manual, pre-release |
| Provider outage returns structured failure | SMS/email/WebAuthn provider timeout or outage produces explicit error, not 500 | Running host with mocked external providers that throw/slow | Trigger login flow with provider set to fail | Response contains `AuthenticationProblemDto` with correct `ProviderFailure` code and 503 status | Semi-automated, nightly |
| Concurrent verification attempts | Two simultaneous completions of the same challenge — one succeeds, one fails | Running host with shared challenge store | Fire two `complete` requests for same challenge ID concurrently | First request succeeds, second returns `ChallengeAlreadyUsed` or `InvalidVerificationCode` | Automated, nightly |

## 7. Suggested Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Reason suggested |
|---|---|---|---|
| Passkey registration and authentication end-to-end | WebAuthn flow works with real browser authenticator | Running host + browser with FIDO2 test authenticator | High-value e2e coverage but requires hardware/browser |
| Multi-tenant session isolation | User session from tenant A does not leak into tenant B | Running host with multi-tenant config | Security-sensitive but complex to set up |
| Rate limiting under load | Password login rate limit triggers after threshold | Running host with load testing tool | Protects against brute force, needs sustained load |

## 8. Deferred or Rejected Tests

| Test/scenario | Decision | Reason |
|---|---|---|
| ASP.NET Identity integration tests | Deferred | Requires real database and IdentityServer configuration |
| Duende IdentityServer logout context | Deferred | Requires Duende runtime, not unit-testable |
| Fido2NetLib WebAuthn assertions | Deferred | Requires cryptographic authenticator hardware |
| React authentication pages | Deferred | Frontend migration not yet owning these artifacts |
| Email/SMS delivery verification | Deferred | Requires real external services, covered by hot scenarios |

## 9. Test Task Derivation

| Task | Source | Type | Requirement | Target suite | Notes |
|---|---|---|---|---|---|
| Domain challenge lifecycle tests | §4 challenge rows | Unit | Required | `Authentication.Tests.Domain` | State machine coverage |
| Domain policy tests | §4 policy rows | Unit | Required | `Authentication.Tests.Domain` | LoginAttempt, VerificationCode, ReturnUrl |
| Handler orchestration tests | §4 handler rows | Unit | Required | `Authentication.Tests.Application` | Mock all abstractions |
| API mapping tests | §4 mapper rows | Unit | Required | `Authentication.Tests.Api` | ProblemHttpMapper, EndpointMapper |
| Contract serialization tests | §4 contract rows | Contract | Required | `Authentication.Tests.Contracts` | Round-trip all DTOs |
| ChallengeCompletionHelper tests | §4 helper rows | Unit | Required | `Authentication.Tests.Application` | Arabic/Persian digit normalization |
| Infrastructure store tests | §5 InMemory store | Unit | Suggested | `Authentication.Tests.Infrastructure` | Direct store CRUD |
| SMS shield tests | §5 shield rows | Unit | Suggested | `Authentication.Tests.Infrastructure` | Rate-limit windowing |

## 10. Existing Coverage / Gaps

No test code exists in the working tree. All tests are created fresh based on this plan.

**Coverage targets by layer:**

| Layer | Classes under test | Planned test classes | Coverage approach |
|---|---|---|---|
| Domain | 7 testable types | 4 (Challenge, Policies, ReturnUrl, ValueObjects) | Pure unit, no mocks |
| Application | 10 handlers + 1 helper + 1 validator | 5 (PasswordHandler, BeginHandlers, CompletionHelper, Validator, HandlerGroup) | Mock abstractions |
| Api/Mapping | 2 mapper classes | 2 (ProblemHttpMapper, EndpointMapper) | Pure unit |
| Contracts | 7 request + 7 response + 2 error types | 1 (SerializationTests covering all) | Round-trip JSON |

## 11. CI / Execution Guidance

| Suite | Execution mode | Command/procedure | Required environment |
|---|---|---|---|
| Domain unit tests | Automated, every PR | `dotnet test tests/Modules/Authentication.Tests/ --filter "Category=Domain"` | None (pure logic) |
| Application unit tests | Automated, every PR | `dotnet test tests/Modules/Authentication.Tests/ --filter "Category=Application"` | None (mocked dependencies) |
| Api mapping tests | Automated, every PR | `dotnet test tests/Modules/Authentication.Tests/ --filter "Category=Api"` | None |
| Contract serialization tests | Automated, every PR | `dotnet test tests/Modules/Authentication.Tests/ --filter "Category=Contract"` | None |
| Full test suite | Automated, every PR | `dotnet test tests/Modules/Authentication.Tests/` | None |
| Hot scenarios | Semi-automated, pre-release | Manual execution against running host with test config | Host with auth, mock providers |
| Architecture boundary check | Automated, every PR | `python3 tools/architecture/validate_interface_boundaries.py` | None |

## 12. Traceability

| Test area | Linked ADR | Linked journey stage | Linked code path |
|---|---|---|---|
| Challenge lifecycle | ADR-0003 (isolation) | — | `Domain/Challenges/AuthenticationChallenge.cs` |
| Return URL policy | — | — | `Domain/ReturnUrls/ReturnUrlPolicy.cs` |
| Handler orchestration | ADR-0004 (protected endpoints) | — | `Application/Handlers/*.cs` |
| API mapping | ADR-0005 (OpenAPI conventions) | — | `Api/Mapping/*.cs` |
| Contract serialization | ADR-0003 (frontend-safe contracts) | — | `Contracts/Requests/*.cs`, `Contracts/Responses/*.cs` |
| ProblemDetails mapping | ADR-0005 (OpenAPI conventions) | — | `Api/Mapping/AuthenticationProblemHttpMapper.cs` |
| Antiforgery flow | ADR-0004 (protected endpoints) | — | `Api/Endpoints/AntiforgeryEndpoints.cs` |
