# ADR-0006 Implementation Plan: Platform Journey Routing

Date: 2026-06-29  
Status: Proposed implementation plan  
Related decision: ADR-0006 — Standardize Platform Journey Routing  
Target repo path: `docs/platform/modules/platform/plans/0006-implement-platform-journey-routing.md`

## 1. Objective

Implement ADR-0006 by introducing one platform-wide `Journey` capability that resolves the next valid destination for an authenticated user.

The final behavior must support this path:

```txt
visitor -> authentication/session -> journey resolution -> mandatory gates -> final destination
```

The goal is not merely to redirect after login. The goal is to remove scattered routing decisions from Authentication, frontend pages, settings pages, profile pages, and app dashboards.

## 2. Decision Constraints

The implementation must respect these ADR constraints:

1. Authentication authenticates only.
2. Journey owns post-authentication destination priority.
3. Users/Profile owns profile completion policy.
4. Frontend follows the backend-resolved destination.
5. Unsafe return URLs are rejected centrally.
6. Mandatory profile completion uses `/account/profile/complete`, not a general settings route.
7. The first blocking requirement wins.
8. Existing authentication contracts must remain migration-safe until clients are updated.

## 3. Non-Goals

Do not implement these in the first delivery:

1. Full risk engine.
2. Full consent management system if it does not already exist.
3. Full workspace management redesign.
4. External identity provider implementation.
5. Passkey enrollment UX beyond preserving the Journey slot for it.
6. Large frontend redesign.
7. Removal of old compatibility fields before generated clients migrate.

## 4. Target Shape

Introduce a platform capability named `Journey`.

Recommended backend structure:

```txt
src/Modules/Journey/
  Domain/
  Application/
  Contracts/
  Api/
  Infrastructure/
```

Recommended frontend structure:

```txt
src/Hosts/Web/src/features/journey/
  hooks/
  routing/
  components/
```

Journey becomes the single owner of this priority:

```txt
1. Account recovery or disabled account
2. Suspicious login or required reauthentication
3. Required contact confirmation
4. Required profile completion
5. Required legal consent
6. Required workspace or app selection
7. Safe requested return URL
8. Last used app or workspace dashboard
9. User home
10. Global dashboard
```

For the first version, account recovery and suspicious-login handling may remain delegated to Authentication. Journey must still reserve their priority slots so the model does not need to be redesigned later.

## 5. Delivery Strategy

Use four slices.

```txt
Slice 1: Backend Journey core
Slice 2: Authentication integration
Slice 3: API contract + OpenAPI + generated frontend client
Slice 4: Frontend journey boundary and page routing
```

Do not begin frontend migration until the backend contract and OpenAPI output are stable.

## 6. Slice 1 — Backend Journey Core

### 6.1 Create Journey module shell

Create the module folders and projects following the repo’s existing module conventions.

Expected artifacts:

```txt
src/Modules/Journey/Domain
src/Modules/Journey/Application
src/Modules/Journey/Contracts
src/Modules/Journey/Api
src/Modules/Journey/Infrastructure
```

If creating new projects is heavier than the current solution structure allows, start with the smallest compatible shape under the existing platform/module structure, but keep namespaces and responsibilities aligned with `Journey`.

### 6.2 Add Journey domain model

Create domain concepts:

```txt
JourneyStep
JourneyGateKind
JourneyGate
JourneyDecision
JourneyDestinationKind
JourneyPolicy
```

Required gate kinds:

```txt
ContactConfirmation
ProfileCompletion
RequiredConsent
WorkspaceSelection
AccountRecovery
SuspiciousLogin
PasskeyEnrollmentPrompt
MfaEnrollmentPrompt
```

Rules:

1. Blocking gates stop normal app access.
2. Non-blocking prompts must not block navigation.
3. The first blocking gate wins.
4. Final fallback must always produce a safe destination.

### 6.3 Add Journey application abstractions

Create application-level abstractions:

```txt
IJourneyResolver
IJourneyProfileReader
IJourneyConsentReader
IJourneyWorkspaceReader
IJourneyReturnUrlPolicy
IJourneyDestinationResolver
```

Responsibilities:

| Artifact | Responsibility |
|---|---|
| `IJourneyResolver` | Resolve the next destination from context and gates. |
| `IJourneyProfileReader` | Read profile/contact completion state without exposing Users internals. |
| `IJourneyConsentReader` | Read required-consent state. |
| `IJourneyWorkspaceReader` | Read active/last workspace or app context. |
| `IJourneyReturnUrlPolicy` | Normalize and validate return URLs. |
| `IJourneyDestinationResolver` | Choose home/dashboard fallback destinations. |

### 6.4 Add Journey request/result models

Application models:

```txt
JourneyRequest
JourneyDecision
JourneyGate
JourneyProfileSnapshot
JourneyConsentSnapshot
JourneyWorkspaceSnapshot
```

`JourneyRequest` must carry enough context to resolve post-authentication and existing-session flows:

```txt
UserId
RequestedReturnUrl
CurrentPath
AppKey
WorkspaceId
IsAfterLogin
```

### 6.5 Implement Journey resolver

Resolver behavior:

1. Read profile/contact snapshot.
2. Evaluate account/contact blocking state.
3. Evaluate profile completion.
4. Evaluate required consent.
5. Evaluate workspace/app selection.
6. Normalize requested return URL.
7. Resolve app/workspace dashboard.
8. Resolve profile/home.
9. Fall back to global dashboard.

Acceptance criteria:

1. The resolver returns `/account/contact/confirm` when contact confirmation is required.
2. The resolver returns `/account/profile/complete` when profile is incomplete.
3. The resolver returns `/account/consents` when consent is required.
4. The resolver returns `/workspaces/select` when app/workspace selection is required.
5. The resolver returns safe `returnUrl` only after all blocking gates pass.
6. The resolver ignores unsafe `returnUrl`.
7. The resolver always returns a valid fallback destination.

## 7. Slice 2 — Users/Profile Ownership

### 7.1 Move profile completion logic out of Authentication

Current primitive logic must not remain in Authentication as the source of truth.

Create a policy under Users/Profile:

```txt
IProfileCompletionPolicy
ProfileCompletionPolicy
ProfileCompletionResult
ProfileCompletionRequirement
```

Baseline profile requirements:

```txt
FirstName
LastName
DisplayName
PreferredLanguage
TimeZone
At least one confirmed primary contact: email or phone
```

If the current data model does not yet contain all baseline fields, split requirements:

```txt
RequiredNow
RequiredLater
```

Do not block implementation because of missing future fields. Document each missing field as a follow-up task.

### 7.2 Add profile snapshot reader

Implement `IJourneyProfileReader` as an infrastructure adapter over the existing Users/Identity data.

Rules:

1. Journey Application depends on abstractions.
2. Journey Infrastructure may depend on Users/Identity infrastructure where allowed by repo boundaries.
3. Authentication must not query Users/Profile internals for completion logic.

Acceptance criteria:

1. Journey can evaluate profile completion through `IJourneyProfileReader`.
2. Authentication no longer owns profile completion logic.
3. Profile completion policy has dedicated tests.

## 8. Slice 3 — Return URL Safety

### 8.1 Centralize safe return URL policy

Create one return URL policy under Journey or shared domain if an existing safe policy already exists.

Rejected examples:

```txt
https://external.example
//external.example
\external.example
/%5cexternal.example
%2f%2fexternal.example
```

Accepted examples:

```txt
/dashboard
/home
/account/profile/complete
/workspaces/select
/apps/{appKey}/dashboard
```

Rules:

1. Only local platform paths are valid.
2. External URLs are invalid.
3. Protocol-relative URLs are invalid.
4. Backslash-relative URLs are invalid.
5. Encoded bypass attempts are invalid.
6. Invalid return URLs are ignored, not repaired into unsafe destinations.

Acceptance criteria:

1. All return URL decisions use one policy.
2. Authentication does not implement a separate return URL rule.
3. Tests cover external, protocol-relative, backslash, encoded, null, blank, and valid local paths.

## 9. Slice 4 — Authentication Integration

### 9.1 Keep contracts migration-safe

Do not remove existing fields immediately.

Keep transitional compatibility:

```txt
ReturnUrl
RequiresProfileCompletion
```

Add or derive Journey decision depending on current contract style.

Recommended compatibility rule:

```txt
ReturnUrl = Journey.DestinationUrl
RequiresProfileCompletion = Journey has blocking ProfileCompletion gate
```

### 9.2 Update login handlers

Update password, email OTP, phone OTP, passkey, and any challenge-completion handlers so successful authentication delegates to Journey.

Required rule:

```txt
Successful authentication -> create/renew session -> resolve Journey -> return Journey result
```

Authentication failure paths must not call Journey.

Acceptance criteria:

1. Successful password login resolves Journey.
2. Successful email challenge completion resolves Journey.
3. Successful phone challenge completion resolves Journey.
4. Successful passkey completion resolves Journey if passkey flow exists.
5. Invalid credentials, expired challenge, locked account, and provider failure do not resolve Journey.
6. Old `ReturnUrl` remains populated from Journey destination during migration.
7. Old `RequiresProfileCompletion` remains derived from Journey gates during migration.

## 10. Slice 5 — Journey API Contract

### 10.1 Add current Journey endpoint

Add endpoint:

```txt
GET /api/journey/current?returnUrl={returnUrl}
```

Purpose:

```txt
Resolve the current authenticated user's Journey from session/context.
```

Stable OpenAPI conventions:

```txt
Tag: Journey
OperationId: getCurrentJourney
Schemas:
  JourneyDecisionDto
  JourneyGateDto
  JourneyStepDto
  JourneyGateKindDto
```

Response behavior:

1. Authenticated user returns Journey decision.
2. Unauthenticated user returns an authentication-required problem or a login destination, depending on existing platform convention.
3. Unsafe return URL is ignored.
4. Blocking gate is represented explicitly.

### 10.2 Add DTOs

Contracts:

```txt
JourneyDecisionDto
JourneyGateDto
JourneyStepDto
JourneyGateKindDto
```

Do not expose domain entities directly.

Acceptance criteria:

1. Contract serialization tests pass.
2. OpenAPI emits stable schema names.
3. Generated frontend client includes Journey endpoint.
4. Generated code lives under generated API folder only and is not manually edited.

## 11. Slice 6 — Frontend Integration

### 11.1 Add Journey feature wrapper

Create frontend feature hooks around generated client hooks:

```txt
useCurrentJourney
resolvePostLoginNavigation
```

Rules:

1. Pages do not call raw generated API hooks directly.
2. Feature hooks translate generated API details into app navigation behavior.
3. Login/challenge pages prefer Journey destination over legacy return URL.

Migration behavior:

```txt
destination = result.journey?.destinationUrl ?? result.returnUrl ?? "/dashboard"
```

### 11.2 Add Journey gate boundary

Create one private-layout boundary:

```txt
JourneyGateBoundary
```

Behavior:

1. Fetch current Journey.
2. If blocking, redirect to `destinationUrl`.
3. If not blocking, render children.
4. Avoid redirect loops when already on the required gate route.
5. Show loading state during resolution.
6. Show safe fallback on contract or network failure.

### 11.3 Add or update mandatory routes

Required routes:

```txt
/account/profile/complete
/account/contact/confirm
/account/consents
/workspaces/select
```

If some routes do not exist yet, create minimal placeholder pages with clear TODOs and safe continuation behavior.

Acceptance criteria:

1. Login success navigates to Journey destination.
2. Existing session entering a protected layout resolves Journey.
3. Incomplete profile redirects to `/account/profile/complete`.
4. Settings page is not used as a mandatory gate.
5. No frontend page duplicates Journey priority.

## 12. Slice 7 — Tests

### 12.1 Backend unit test suggestions (docs/platform/<module>/tests)

Journey resolver tests:

```txt
ContactConfirmation beats ProfileCompletion
ProfileCompletion beats Consent
Consent beats WorkspaceSelection
WorkspaceSelection beats ReturnUrl
Safe ReturnUrl wins after gates pass
Unsafe ReturnUrl is ignored
Dashboard fallback is returned when no better destination exists
```

Return URL policy tests:

```txt
Reject external absolute URL
Reject protocol-relative URL
Reject backslash-relative URL
Reject encoded protocol-relative URL
Accept local absolute path
Default safely on null/blank
```

Profile policy tests:

```txt
Complete profile passes
Missing first name fails
Missing last name fails
Missing display name fails when required
Missing preferred language fails when required
Missing time zone fails when required
No confirmed contact fails
```

Authentication tests:

```txt
Successful login resolves Journey
Failed login does not resolve Journey
RequiresProfileCompletion compatibility is derived from Journey
ReturnUrl compatibility is derived from Journey
```

Contract tests:

```txt
JourneyDecisionDto serializes and deserializes
JourneyGateDto serializes and deserializes
Enum names remain stable
Authentication redirect contract remains backward compatible
```

### 12.2 Frontend tests

```txt
Login success navigates to Journey destination
Login fallback navigates to legacy returnUrl if Journey missing
Private layout redirects blocking gate
Private layout renders content when no blocking gate exists
Boundary does not loop on current gate route
Profile completion save re-resolves Journey
```

## 13. Rollout Plan

### Stage 1 — Passive Journey

Add backend Journey resolver and endpoint without changing login behavior.

Goal:

```txt
GET /api/journey/current works and tests pass.
```

### Stage 2 — Authentication Compatibility

Update authentication results to include or derive Journey while preserving old fields.

Goal:

```txt
Existing clients continue working.
New clients can use Journey.
```

### Stage 3 — Frontend Adoption

Update frontend login and protected layouts to follow Journey.

Goal:

```txt
Frontend no longer infers post-login routing.
```

### Stage 4 — Remove Hardcoded Settings Redirect

Replace mandatory `/Account/Settings` redirects with `/account/profile/complete`.

Goal:

```txt
Settings is voluntary. Profile completion is a dedicated gate.
```

### Stage 5 — Deprecate Legacy Fields

After generated clients and consumers are migrated, mark legacy redirect/profile flags as deprecated.

Goal:

```txt
Journey becomes the only public routing model.
```

## 14. Suggested Work Items

### Work Item 1 — Journey core

Create Journey module, domain types, resolver, snapshots, and tests.

Output:

```txt
Journey module exists and resolver tests pass.
```

### Work Item 2 — Profile completion policy

Create Users/Profile completion policy and Journey profile reader.

Output:

```txt
Profile completeness is no longer owned by Authentication.
```

### Work Item 3 — Safe return URL policy

Centralize safe return URL validation and tests.

Output:

```txt
All post-auth return URL handling uses one policy.
```

### Work Item 4 — Journey API

Expose current Journey endpoint and DTOs.

Output:

```txt
OpenAPI emits stable Journey contract.
```

### Work Item 5 — Authentication migration

Update successful authentication flows to resolve Journey.

Output:

```txt
Auth result returns Journey-compatible destination.
```

### Work Item 6 — Frontend migration

Add Journey feature hook and gate boundary.

Output:

```txt
Frontend follows Journey destination and stops duplicating priority.
```

### Work Item 7 — Route cleanup

Create mandatory completion routes and remove settings-as-gate behavior.

Output:

```txt
/profile completion gate is explicit and purpose-specific.
```

## 15. Review Checklist

Use this checklist before opening the implementation PR:

```txt
[ ] ADR-0006 is referenced in PR description.
[ ] Journey is the only owner of destination priority.
[ ] Authentication does not hardcode profile/settings/workspace/dashboard destinations.
[ ] Profile completion rules live in Users/Profile.
[ ] Return URL validation is centralized.
[ ] Mandatory completion route is /account/profile/complete.
[ ] Frontend uses Journey destination.
[ ] Generated API client was regenerated, not manually edited.
[ ] Unit tests cover gate priority.
[ ] Unit tests cover unsafe return URLs.
[ ] Contract tests cover Journey DTOs.
[ ] Migration compatibility is preserved.
```

## 16. Stop Conditions

Stop and redesign if any of these happen:

1. Authentication directly checks profile fields to decide final destination.
2. Frontend duplicates Journey priority.
3. Multiple modules implement competing return URL validation.
4. `/account/settings` remains a mandatory completion gate.
5. Journey depends directly on UI or generated frontend contracts.
6. Domain/application code imports infrastructure or framework-specific routing details.
7. A blocking gate is represented only as a boolean instead of an explicit gate decision.

## 17. Definition of Done

ADR-0006 is fully implemented when:

1. Journey resolves current authenticated-user destination.
2. All successful authentication flows delegate destination resolution to Journey.
3. Mandatory gates are explicit.
4. Profile completion policy is owned by Users/Profile.
5. Unsafe return URLs are rejected centrally.
6. Frontend private layout uses one Journey gate boundary.
7. Login/challenge pages navigate to the resolved Journey destination.
8. `/account/profile/complete` replaces settings as the mandatory completion route.
9. OpenAPI includes stable Journey operation and schemas.
10. Tests prove priority, safety, compatibility, and fallback behavior.
