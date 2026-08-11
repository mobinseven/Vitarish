# Remove Interfaces.Accounts — Implementation Plan

Date: 2026-06-28
Status: Draft
Scope: Full removal of `src/Interfaces/Interfaces.Accounts/`
Depends on: ADR-0003 (accepted)

---

## 1. Inventory — What Lives Where Today

### Services in Interfaces.Accounts

| Service | Current Dependencies | Target Layer | Target Module |
|---|---|---|---|
| `UserService` (implements `IUserService`) | `IUsersService`, `IUserProfileService`, `IDistributedCache`, `IEmailAuthenticationService`, `ISmsAuthenticationService` | Application (caching removed; each consumer calls backend API directly) | N/A — service dissolved |
| `SmsAuthenticationService` (implements `ISmsAuthenticationService`) | `UserManager<User>`, `ICommunicationCommandQueue` | Application | Authentication |
| `EmailAuthenticationService` (implements `IEmailAuthenticationService`) | `UserManager<User>`, `ICommunicationCommandQueue` | Application | Authentication |
| `PersonalAccessTokenService` (implements `IPersonalAccessTokenService`) | `IPersonalAccessTokenGenerator` | Infrastructure | Users |
| `WebAuthnInteropService` (implements `IWebAuthnInterop`) | `IJSRuntime` | **Deleted** — Blazor-specific | N/A |
| `IdentityRevalidatingAuthenticationStateProvider` | `AuthenticationStateProvider`, `IHttpContextAccessor` | **Deleted** — Blazor Server specific | N/A |
| `ReturnUrlManager` | `IDistributedCache`, `IHttpContextAccessor` | **Deleted** — cookie-based redirect managed by backend | N/A |
| `UserProfileCultureInfoProvider` (implements `IUserProfileCultureInfoProvider`) | `IUserService`, `IHttpContextAccessor`, `IJSRuntime` | **Deleted** — culture resolved from profile via API | N/A |
| `InMemoryVerificationSmsRequestShield` (implements `IVerificationSmsRequestShield`) | `TimeProvider`, `Options<VerificationSmsShieldOptions>` | Infrastructure | Authentication |

### Abstractions

| Abstraction | Current Consumers | Target |
|---|---|---|
| `IUserService` | Main host: `PageBase`, `MainLayout`, `SimpleLayout`, all Account pages, `IdentityCommonBase` | **Dissolved** — Main host uses `Users.Client` HTTP calls directly, or a thin local service wrapping the HTTP client |
| `IWebAuthnInterop` | Main host passkey pages | **Dissolved** — Web host uses WebAuthn JS API directly via `lib/webauthn.ts`; Main host uses server-side WebAuthn endpoints |
| `IUserProfileCultureInfoProvider` | Main host `PageBase` | **Dissolved** — culture resolved from profile via Users API |
| `IVerificationSmsRequestShield` | Main host phone change form | **Dissolved** — rate limiting moved to Authentication API endpoint (server-side) |

### Blazor Components (all deleted — replicated in Web host)

| Component | Web Host Equivalent |
|---|---|
| `PasswordLoginForm` | `LoginForm.tsx` + `usePasswordLogin` |
| `PhoneLoginForm` | `PhoneOtpForm.tsx` + `usePhoneLogin` |
| `EmailLoginForm` | `EmailOtpForm.tsx` + `useEmailLogin` |
| `WebAuthnLoginForm` | `PasskeyButton.tsx` + `usePasskeyLogin` |
| `PasswordChangeForm` | **New** — `app/account/password/page.tsx` |
| `PhoneChangeForm` | **New** — `app/account/phone/page.tsx` |
| `EmailChangeForm` | **New** — `app/account/email/page.tsx` |
| `PhoneVerificationSend/Verify` | Folded into phone change flow |
| `EmailVerificationSend/Verify` | Folded into email change flow |
| `CultureSelector` | **New** — `components/account/CultureSelector.tsx` |
| `ProfileRoleEditor` | **New** — `components/account/ProfileRoleEditor.tsx` (read-only display) |
| `StatusMessage` | `StatusMessage.tsx` (already exists) |
| `AccountLayout` | Account layout wrapper |

### Models (all deleted)

| Model | Web Host Equivalent |
|---|---|
| `PasswordLoginModel` | `PasswordLoginRequest` (generated) |
| `SendSmsCodeModel` | Folded into phone change flow |
| `VerifySmsCodeModel` | Folded into phone change flow |
| `PasswordChangeModel` | **New** contract DTO |
| `PhoneChangeVerifyModel` | **New** contract DTO |
| `EmailChangeVerifyModel` | **New** contract DTO |
| `PersonalAccessTokenModel` | **New** contract DTO |
| `WebAuthnCredentialDto` | Folded into passkey endpoints |

---

## 2. New Backend API Endpoints Needed

### 2a. Users Module — Profile Management Endpoints

Add to `src/Modules/Users/Contracts/Routes/UsersApiRoutes.cs`:

```csharp
public const string Profile = "/{userId}/profile";
public const string Culture = "/{userId}/culture";
public const string Theme = "/{userId}/theme";
```

New contracts in `src/Modules/Users/Contracts/Requests/`:
- `UpdateUserProfileRequest.cs` — `{ firstName, lastName }`
- `SetUserCultureRequest.cs` — `{ culture }`
- `SetUserThemeRequest.cs` — `{ theme }`

New contracts in `src/Modules/Users/Contracts/Responses/`:
- `UserProfileResponse.cs` — `{ userId, firstName, lastName, email, phoneNumber, culture, theme }`

New endpoints in `src/Modules/Users/Api/`:
- `PUT /api/modules/users/{userId}/profile` → calls `IUsersService.UpdateUserAsync`
- `GET /api/modules/users/{userId}/profile` → returns profile response
- `PUT /api/modules/users/{userId}/culture` → calls `IUserProfileService.UpdateProfileAsync`
- `GET /api/modules/users/{userId}/culture` → returns culture string
- `PUT /api/modules/users/{userId}/theme` → calls `IUserProfileService.UpdateProfileAsync`
- `GET /api/modules/users/{userId}/theme` → returns theme string

New queries in `src/Modules/Users/Application/`:
- `GetUserProfileQuery.cs` + handler
- `GetUserCultureQuery.cs` + handler
- `GetUserThemeQuery.cs` + handler

New commands in `src/Modules/Users/Application/`:
- `UpdateUserProfileCommand.cs` (already exists as `UpdateUserProfileCommand`)
- `SetUserCultureCommand.cs` + handler
- `SetUserThemeCommand.cs` + handler

### 2b. Users Module — Personal Access Token Endpoints

Add to `src/Modules/Users/Contracts/Routes/UsersApiRoutes.cs`:

```csharp
public const string PatBase = "/{userId}/tokens";
public const string PatCreate = "/{userId}/tokens";
public const string PatList = "/{userId}/tokens";
public const string PatRevoke = "/{userId}/tokens/{token}";
```

Move `IPersonalAccessTokenService` from Interfaces.Accounts to `src/Modules/Users/Infrastructure/Service/PersonalAccessTokenService.cs`.

New contracts:
- `CreatePersonalAccessTokenRequest.cs` — `{ lifetimeDays?, scopes? }`
- `PersonalAccessTokenResponse.cs` — `{ token, createdAt }`
- `PersonalAccessTokenListResponse.cs` — `{ tokens: string[] }`

New endpoints in `src/Modules/Users/Api/`:
- `POST /api/modules/users/{userId}/tokens` → create PAT
- `GET /api/modules/users/{userId}/tokens` → list PATs
- `DELETE /api/modules/users/{userId}/tokens/{token}` → revoke PAT

### 2c. Authentication Module — Account Verification Endpoints

Add to `src/Modules/Authentication/Contracts/Routes/AuthenticationRoutes.cs`:

```csharp
public const string BeginChangePhone = "/account/phone/begin";
public const string CompleteChangePhone = "/account/phone/complete";
public const string BeginChangeEmail = "/account/email/begin";
public const string CompleteChangeEmail = "/account/email/complete";
public const string ChangePassword = "/account/password";
```

New contracts in `src/Modules/Authentication/Contracts/Requests/`:
- `BeginChangePhoneRequest.cs` — `{ phoneNumber }`
- `CompleteChangePhoneRequest.cs` — `{ challengeId, code }`
- `BeginChangeEmailRequest.cs` — `{ email }`
- `CompleteChangeEmailRequest.cs` — `{ challengeId, code }`
- `ChangePasswordRequest.cs` — `{ currentPassword, newPassword }`

New contracts in `src/Modules/Authentication/Contracts/Responses/`:
- Reuse `AuthenticationChallengeDto` for begin responses

New application commands + handlers:
- `BeginChangePhoneCommand.cs` + handler
- `CompleteChangePhoneCommand.cs` + handler
- `BeginChangeEmailCommand.cs` + handler
- `CompleteChangeEmailCommand.cs` + handler
- `ChangePasswordCommand.cs` + handler

New endpoints in `src/Modules/Authentication/Api/Endpoints/`:
- `AccountPhoneChangeEndpoints.cs`
- `AccountEmailChangeEndpoints.cs`
- `AccountPasswordChangeEndpoints.cs`

### 2d. WebAuthn — Credential Management Endpoints

Add to `src/Modules/Users/Contracts/Routes/UsersApiRoutes.cs`:

```csharp
public const string WebAuthnCredentials = "/{userId}/webauthn";
public const string WebAuthnBeginRegister = "/{userId}/webauthn/begin-register";
public const string WebAuthnCompleteRegister = "/{userId}/webauthn/complete-register";
public const string WebAuthnDelete = "/{userId}/webauthn/{credentialId}";
```

The existing `IWebAuthnService` in `Users.Application` already handles WebAuthn operations. New endpoints expose them over HTTP.

New endpoints in `src/Modules/Users/Api/`:
- `GET /api/modules/users/{userId}/webauthn` → list credentials
- `POST /api/modules/users/{userId}/webauthn/begin-register` → begin registration
- `POST /api/modules/users/{userId}/webauthn/complete-register` → complete registration
- `DELETE /api/modules/users/{userId}/webauthn/{credentialId}` → delete credential

---

## 3. Orval Client Generation Updates

Update `src/Hosts/Web/src/api/client/orval.config.ts` to add a second output target:

```typescript
users: {
  input: {
    target: "http://localhost:5000/openapi/users.json",
  },
  output: {
    target: "./src/api/generated/users/users.ts",
    schemas: "./src/api/generated/users/model",
    client: "react-query",
    override: {
      mutator: {
        path: "./src/api/client/apiFetch.ts",
        name: "apiFetch",
      },
    },
  },
},
```

This generates React Query hooks for all Users endpoints (profile, PATs, WebAuthn, etc.).

The existing `authentication` target automatically picks up the new account-change endpoints when the Authentication OpenAPI spec is regenerated.

---

## 4. Web Host — New Pages, Hooks, and Components

### 4a. New Pages

| Route | File | Purpose |
|---|---|---|
| `/account` | `app/account/page.tsx` | Account settings hub (mirrors `Settings.razor`) |
| `/account/name` | `app/account/name/page.tsx` | Edit first/last name |
| `/account/email` | `app/account/email/page.tsx` | Change email (two-step: begin → verify code) |
| `/account/phone` | `app/account/phone/page.tsx` | Change phone (two-step: begin → verify code) |
| `/account/password` | `app/account/password/page.tsx` | Change password |
| `/account/passkeys` | `app/account/passkeys/page.tsx` | Manage WebAuthn credentials |
| `/account/theme` | `app/account/theme/page.tsx` | Change theme/appearance |
| `/account/culture` | `app/account/culture/page.tsx` | Change language/culture |

### 4b. New Hooks

| Hook | File | Purpose |
|---|---|---|
| `useUserProfile` | `hooks/account/useUserProfile.ts` | Query hook for `GET /api/modules/users/{userId}/profile` |
| `useUpdateProfile` | `hooks/account/useUpdateProfile.ts` | Mutation for `PUT /api/modules/users/{userId}/profile` |
| `useChangePassword` | `hooks/account/useChangePassword.ts` | Mutation for `POST /api/modules/authentication/account/password` |
| `useChangePhone` | `hooks/account/useChangePhone.ts` | Two-step: `beginChangePhone` → `completeChangePhone` |
| `useChangeEmail` | `hooks/account/useChangeEmail.ts` | Two-step: `beginChangeEmail` → `completeChangeEmail` |
| `usePasskeyManagement` | `hooks/account/usePasskeyManagement.ts` | List, register, delete WebAuthn credentials |
| `useTheme` | `hooks/account/useTheme.ts` | Get/set theme preference |
| `useCulture` | `hooks/account/useCulture.ts` | Get/set culture preference |
| `usePersonalAccessTokens` | `hooks/account/usePersonalAccessTokens.ts` | List/create/revoke PATs |

### 4c. New Components

| Component | File | Purpose |
|---|---|---|
| `AccountSidebar` | `components/account/AccountSidebar.tsx` | Navigation sidebar for account settings |
| `AccountLayout` | `app/account/layout.tsx` | Layout wrapper with sidebar + auth guard |
| `NameEditForm` | `components/account/NameEditForm.tsx` | First/last name form |
| `PasswordChangeForm` | `components/account/PasswordChangeForm.tsx` | Current + new password form |
| `PhoneChangeFlow` | `components/account/PhoneChangeFlow.tsx` | Two-step phone change (OTP) |
| `EmailChangeFlow` | `components/account/EmailChangeFlow.tsx` | Two-step email change (OTP) |
| `PasskeyList` | `components/account/PasskeyList.tsx` | List + register + delete passkeys |
| `ThemeSelector` | `components/account/ThemeSelector.tsx` | Theme picker (light/dark/system) |
| `CultureSelector` | `components/account/CultureSelector.tsx` | Language dropdown |
| `PatManager` | `components/account/PatManager.tsx` | PAT list + create + revoke |

### 4d. Auth Guard for Account Routes

Add middleware or layout-level auth check:
```typescript
// app/account/layout.tsx
// Redirect to /login if not authenticated
```

---

## 5. Main Host Reference Updates

### 5a. Replace IUserService Usage

The Main host uses `IUserService` for three purposes:
1. **Culture resolution** (`PageBase`) — replace with direct HTTP call to Users API
2. **Theme loading** (`MainLayout`, `SimpleLayout`) — replace with direct HTTP call to Users API
3. **User data loading** (`IdentityCommonBase`) — replace with direct HTTP call to Users API

**Strategy**: Create a thin `MainHttpUserService` in `src/Hosts/Main/Services/` that wraps `IHttpClientFactory` and calls the Users API endpoints. This preserves the `IUserService` interface contract within Main host without any dependency on Interfaces.Accounts.

```csharp
// src/Hosts/Main/Services/MainHttpUserService.cs
// Implements IUserService (a local copy of the interface, NOT from Interfaces.Accounts)
// Makes HTTP calls to /api/modules/users/{userId}/profile
// Caches with IDistributedCache (same caching behavior as before)
```

**Alternatively** (simpler): Since `IUserService` is only used in 3 layout/base components for culture and theme, inline the HTTP calls directly:

```csharp
// In PageBase.cs — replace:
//   string? culture = await UserService.GetCultureAsync(userId);
// With:
//   var response = await httpClient.GetAsync($"/api/modules/users/{userId}/culture");
//   string? culture = response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
```

**Recommended approach**: Inline HTTP calls for the 3 specific use cases (culture, theme, user profile). No need for a full `IUserService` replacement.

### 5b. Replace Base Class Hierarchy

| Current | Target |
|---|---|
| `IdentityCommonBase` (from Interfaces.Accounts) | Create `Main.Components.IdentityCommonBase` in `src/Hosts/Main/Components/` |
| `IdentityBase` (from Interfaces.Accounts) | Create `Main.Components.IdentityBase` in `src/Hosts/Main/Components/` |
| `LoginBase` (from Interfaces.Accounts) | Create `Main.Components.LoginBase` in `src/Hosts/Main/Components/` |

These new base classes should:
- Use `IRequestContext` instead of `IHttpContextAccessor` + Duende extensions
- Remove dependencies on `IUserService`, `IEmailAuthenticationService`, `ISmsAuthenticationService`
- Keep only what Main host actually needs (User resolution, return URL, auth state)

### 5c. Replace Identity Pages

The Main host Identity pages (`PasswordLogin.razor`, `PhoneLogin.razor`, etc.) currently use `LoginBase` and Blazor components from Interfaces.Accounts. These need to be rewritten to:

1. Use the new local base classes
2. Submit forms to the backend Authentication API endpoints (not through Blazor component forms)
3. Use the same challenge-based flow that Web host uses

**Specific pages to rewrite**:
- `Pages/Identity/PasswordLogin.razor` + `PasswordLoginSubmit.cshtml.cs`
- `Pages/Identity/PhoneLogin.razor` + `PhoneLoginSubmit.cshtml.cs`
- `Pages/Identity/EmailLogin.razor` + `EmailLoginSubmit.cshtml.cs`
- `Pages/Identity/PasskeyLogin.razor`
- `Pages/Identity/Logout.razor`

### 5d. Replace Account Management Pages

The Main host Account pages (`Pages/Accounts/*.razor`) currently use `IdentityCommonBase` and Blazor components from Interfaces.Accounts. These need to be rewritten to:

1. Use the new local `IdentityCommonBase`
2. Call Users API endpoints via `IHttpClientFactory` or a local service
3. Use the same API contracts that Web host uses

**Specific pages to rewrite**:
- `Pages/Accounts/Settings.razor` — remove `CultureSelector`, `ProfileRoleEditor` from Interfaces.Accounts
- `Pages/Accounts/Name.razor` — call `PUT /api/modules/users/{userId}/profile`
- `Pages/Accounts/Password.razor` — call `POST /api/modules/authentication/account/password`
- `Pages/Accounts/Phone.razor` — call begin/complete phone change endpoints
- `Pages/Accounts/Email.razor` — call begin/complete email change endpoints
- `Pages/Accounts/Passkeys.razor` — call WebAuthn credential management endpoints
- `Pages/Accounts/Theme.razor` — call `PUT /api/modules/users/{userId}/theme`

### 5e. DI Registration Updates

In `src/Hosts/Main/Program.cs`:
- Remove `builder.Services.AddVitarishInterfacesAggregator()` (which transitively pulls Interfaces.Accounts)
- Add direct registrations for services Main host needs
- Register `IHttpClientFactory` (likely already registered)
- Register new `Main.Components.IdentityCommonBase` base class services

### 5f. Project Reference Updates

In `src/Hosts/Main/Main.csproj`:
- Remove `<ProjectReference Include="..\..\Interfaces\Interfaces.Accounts\Interfaces.Accounts.csproj" />`
- Add `<ProjectReference Include="..\..\Modules\Users\Users.Client\Users.Client.csproj" />`
- Add `<ProjectReference Include="..\..\Modules\Authentication\Authentication.Contracts\Authentication.Contracts.csproj" />`

### 5g. Global Usings and _Imports.razor Updates

- Remove `using Vitarish.Interfaces.Accounts.Abstractions` from `Pages/GlobalUsings.cs`
- Remove `using Vitarish.Interfaces.Accounts.Models` from `Pages/GlobalUsings.cs`
- Remove `using Vitarish.Interfaces.Accounts.Services` from `Pages/GlobalUsings.cs`
- Remove `using Vitarish.Interfaces.Accounts.Components` from `Pages/_Imports.razor`
- Remove `using Vitarish.Interfaces.Accounts.Attributes` from `Pages/_Imports.razor`
- Remove `using Vitarish.Interfaces.Accounts.Models` from `Components/_Imports.razor`

---

## 6. Interfaces.Aggregator Cleanup

`src/Interfaces/Interfaces.Aggregator/Interfaces.Aggregator.csproj`:
- Remove `<ProjectReference Include="..\Interfaces.Accounts\Interfaces.Accounts.csproj" />`
- If Aggregator has zero source files and only serves as a composition root for Interface libraries, consider whether it still needs to exist or can be replaced by direct host references

---

## 7. Phased Execution Order

### Phase 1: Backend API Endpoints (no frontend changes)

**Goal**: Expose all missing functionality over HTTP so both hosts can consume it.

1. Add Users module profile endpoints (GET/PUT profile, culture, theme)
2. Add Users module PAT endpoints (create, list, revoke)
3. Add Users module WebAuthn credential management endpoints (list, register, delete)
4. Add Authentication module account-change endpoints (change password, begin/complete phone change, begin/complete email change)
5. Move `InMemoryVerificationSmsRequestShield` to `Authentication.Infrastructure` (server-side rate limiting)
6. Move `PersonalAccessTokenService` to `Users.Infrastructure`
7. Verify all endpoints work via API tests

### Phase 2: Web Host Account Management Pages

**Goal**: Replicate all account management features in Web host.

1. Add Orval config for Users module
2. Regenerate OpenAPI clients for both Authentication and Users modules
3. Create account layout with auth guard (`app/account/layout.tsx`)
4. Build account settings hub page (`app/account/page.tsx`)
5. Build profile management (name edit, culture, theme)
6. Build password change flow
7. Build phone change flow (two-step OTP)
8. Build email change flow (two-step OTP)
9. Build passkey management (list, register, delete)
10. Build PAT management (list, create, revoke)
11. Add tests for all new hooks and components

### Phase 3: Main Host Migration

**Goal**: Main host no longer depends on Interfaces.Accounts.

1. Create new `IdentityCommonBase`, `IdentityBase`, `LoginBase` in `src/Hosts/Main/Components/`
2. Create `MainHttpUserService` or inline HTTP calls for culture/theme/profile
3. Rewrite Identity pages (login flows) to use backend API
4. Rewrite Account pages (settings, name, password, phone, email, passkeys, theme)
5. Update `Program.cs` DI registrations
6. Update `Main.csproj` project references
7. Update `GlobalUsings.cs` and `_Imports.razor` files
8. Verify all Main host pages work

### Phase 4: Cleanup

**Goal**: Remove Interfaces.Accounts entirely.

1. Verify no references to Interfaces.Accounts remain in any `.csproj`
2. Verify no `using Vitarish.Interfaces.Accounts` statements remain
3. Delete `src/Interfaces/Interfaces.Accounts/` directory
4. Update `Interfaces.Aggregator.csproj` to remove Interfaces.Accounts reference
5. Run full solution build
6. Review changed project references against `docs/architecture.md`
7. Update documentation

---

## 8. Risk Assessment

| Risk | Mitigation |
|---|---|
| Main host login flow breaks during migration | Phase 3 rewrites pages in place; keep old pages as fallback until new ones are verified |
| Cached data loss when UserService is dissolved | Users API endpoints can be cached client-side with `IDistributedCache` in Main host |
| Blazor Server auth state breaks | `IdentityRevalidatingAuthenticationStateProvider` is replaced by cookie-based auth (same mechanism Web host uses) |
| SMS rate limiting moves from client to server | `InMemoryVerificationSmsRequestShield` moves to `Authentication.Infrastructure`; behavior preserved |
| PAT in-memory store is not persistent | `PersonalAccessTokenService` uses `ConcurrentDictionary` — this is existing behavior, not a regression |

---

## 9. Files Affected (Summary)

### New files to create:
- `src/Modules/Users/Contracts/Requests/UpdateUserProfileRequest.cs`
- `src/Modules/Users/Contracts/Requests/SetUserCultureRequest.cs`
- `src/Modules/Users/Contracts/Requests/SetUserThemeRequest.cs`
- `src/Modules/Users/Contracts/Responses/UserProfileResponse.cs`
- `src/Modules/Users/Api/Endpoints/ProfileEndpoints.cs`
- `src/Modules/Users/Api/Endpoints/PatEndpoints.cs`
- `src/Modules/Users/Api/Endpoints/WebAuthnEndpoints.cs`
- `src/Modules/Authentication/Contracts/Requests/BeginChangePhoneRequest.cs`
- `src/Modules/Authentication/Contracts/Requests/CompleteChangePhoneRequest.cs`
- `src/Modules/Authentication/Contracts/Requests/BeginChangeEmailRequest.cs`
- `src/Modules/Authentication/Contracts/Requests/CompleteChangeEmailRequest.cs`
- `src/Modules/Authentication/Contracts/Requests/ChangePasswordRequest.cs`
- `src/Modules/Authentication/Api/Endpoints/AccountPhoneChangeEndpoints.cs`
- `src/Modules/Authentication/Api/Endpoints/AccountEmailChangeEndpoints.cs`
- `src/Modules/Authentication/Api/Endpoints/AccountPasswordChangeEndpoints.cs`
- `src/Hosts/Main/Components/IdentityCommonBase.cs` (local replacement)
- `src/Hosts/Main/Components/IdentityBase.cs` (local replacement)
- `src/Hosts/Main/Components/LoginBase.cs` (local replacement)
- `src/Hosts/Web/src/app/account/layout.tsx`
- `src/Hosts/Web/src/app/account/page.tsx`
- `src/Hosts/Web/src/app/account/name/page.tsx`
- `src/Hosts/Web/src/app/account/email/page.tsx`
- `src/Hosts/Web/src/app/account/phone/page.tsx`
- `src/Hosts/Web/src/app/account/password/page.tsx`
- `src/Hosts/Web/src/app/account/passkeys/page.tsx`
- `src/Hosts/Web/src/app/account/theme/page.tsx`
- `src/Hosts/Web/src/app/account/culture/page.tsx`
- `src/Hosts/Web/src/hooks/account/` (all hooks listed in §4b)
- `src/Hosts/Web/src/components/account/` (all components listed in §4c)

### Files to modify:
- `src/Modules/Users/Contracts/Routes/UsersApiRoutes.cs`
- `src/Modules/Users/Api/UsersApi.cs`
- `src/Modules/Authentication/Contracts/Routes/AuthenticationRoutes.cs`
- `src/Hosts/Main/Main.csproj`
- `src/Hosts/Main/Program.cs`
- `src/Hosts/Main/Components/PageBase.cs`
- `src/Hosts/Main/Components/MainLayout.razor.cs`
- `src/Hosts/Main/Components/SimpleLayout.razor.cs`
- `src/Hosts/Main/Pages/Identity/*.razor` (all login pages)
- `src/Hosts/Main/Pages/Accounts/*.razor` (all account pages)
- `src/Hosts/Main/Pages/GlobalUsings.cs`
- `src/Hosts/Main/Pages/_Imports.razor`
- `src/Hosts/Main/Components/_Imports.razor`
- `src/Interfaces/Interfaces.Aggregator/Interfaces.Aggregator.csproj`
- `src/Hosts/Web/src/api/client/orval.config.ts`

### Files to delete:
- `src/Interfaces/Interfaces.Accounts/` (entire directory)
