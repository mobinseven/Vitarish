# Authentication & Login Journey

## 1. Context

- App: Platform-wide (all apps)
- Module: Base (Identity)
- Capability: Authentication, session management
- Related apps: All apps require authentication
- Related modules: Parties (tenant resolution), Users (user lookup)
- Product / service: Vitarish authentication system
- Journey type: Current state with gaps
- Scenario: User authenticates to access a Vitarish workspace.
- User goal: Sign in securely and reach the correct tenant context.
- Business goal: Authenticate users reliably, protect tenant data boundaries, support multiple authentication methods.
- Scope start: User reaches a protected route or navigates to login.
- Scope end: User is authenticated, tenant context is resolved, and the home page or requested page loads.
- Primary persona: Any authenticated user (owner, tenant user, platform admin)
- Evidence sources: Static code review of authentication pages, identity components, host configuration
- Confidence level: Medium

## 2. Persona Snapshot

**Name / segment:** Any Vitarish user

**Situation:** Needs to access a tenant workspace or perform actions that require authentication.

**Motivation:** Reach their work quickly with minimal friction while maintaining security.

**Key needs:** Multiple login options, clear error messages, fast OTP delivery, reliable session persistence.

**Constraints:** May not have all credential types (e.g., no phone for SMS OTP, no password set). May be on shared or mobile devices.

**Success means:** User authenticates on the first attempt and reaches their intended destination without confusion.

## 3. Journey Summary

**Narrative:** The user arrives at a protected route and is redirected to login. They choose from four authentication methods: phone OTP, email OTP, password, or passkey. After successful authentication, the system resolves their tenant context via claims transformation and redirects them to the home page or their originally requested destination.

**Emotional arc:** Start: goal-oriented (wanting access) → Middle: focused (entering credentials) → End: relieved (access granted) or frustrated (access denied)

**Biggest friction point:** Four separate login pages with no clear default; users must navigate between methods to find the one they can use.

**Highest-value opportunity:** Smart default selection based on user's configured credentials, and a unified login surface that adapts to available methods.

## 4. Journey Stages

### Stage 1: Discovery & Entry

**User goal:** Reach the login page.

**User actions:** Navigate to a protected route; get redirected to login.

**Touchpoints / channels:** Browser URL, redirect from `Routes.razor` via `RedirectToUnauthorized`.

**User thoughts / questions:** "Why was I redirected? Where do I log in?"

**Emotions:** Expectant, 3, because login is a routine gate.

**Pain points:** No clear explanation of why the redirect happened; the originally requested path is stored but not communicated.

**Needs:** Smooth redirect with return URL preservation.

**Opportunities:** Show a brief "Please sign in to continue" message with the return URL context.

**Success metrics:** Redirect-to-login success rate; return URL preservation rate.

**Evidence:** Static code review assumption.

### Stage 2: Method Selection

**User goal:** Choose the authentication method they can use.

**User actions:** View login page; optionally switch between phone, email, password, or passkey methods via cross-links.

**Touchpoints / channels:** `/Account/Login` (phone, default), `/Account/Login/Email`, `/Account/Login/Password`, `/Account/Login/Passkey`.

**User thoughts / questions:** "Which method do I have set up? Which is fastest?"

**Emotions:** Neutral, 3, because options are visible but scattered.

**Pain points:** Four separate pages with no unified view; users must guess which method they configured; no visual indicator of which methods are available to them.

**Needs:** Clear method comparison; indication of which methods the user has configured.

**Opportunities:** Unified login page showing all available methods; auto-detect passkey support; remember last-used method.

**Success metrics:** Login method selection time; cross-link click rate (indicating confusion).

**Evidence:** Static code review assumption.

### Stage 3: Credential Entry

**User goal:** Enter credentials correctly on the first attempt.

**User actions:** Enter phone number / email / password / passkey username; submit.

**Touchpoints / channels:** `PhoneLoginForm`, `EmailLoginForm`, `PasswordLoginForm`, `WebAuthnLoginForm`.

**User thoughts / questions:** "Did I enter the right number? Is the OTP coming?"

**Emotions:** Focused, 3, for standard entry; Anxious, 2, if OTP delivery is slow.

**Pain points:** Phone OTP requires two steps (send + verify); no autocomplete for phone across browsers; passkey fallback not obvious when browser lacks support.

**Needs:** Fast OTP delivery; clear field labels; browser autocomplete support.

**Success metrics:** First-attempt success rate; OTP resend rate; passkey fallback usage.

**Evidence:** Static code review assumption.

### Stage 4: OTP Verification (Phone/Email)

**User goal:** Complete verification with the OTP code.

**User actions:** Receive OTP; enter 6-digit code; submit.

**Touchpoints / channels:** `PhoneVerificationVerify`, `EmailVerificationVerify`.

**User thoughts / questions:** "Did the code arrive? Is it expired? Can I resend?"

**Emotions:** Impatient, 2, if delivery is slow; Satisfied, 4, on quick verification.

**Pain points:** No countdown timer for OTP expiry; no visual indication of delivery status; no auto-submit on full code entry.

**Needs:** Delivery confirmation, expiry indication, resend with cooldown.

**Success metrics:** OTP verification success rate; resend frequency; time-to-verify.

**Evidence:** Static code review assumption.

### Stage 5: Authentication Result

**User goal:** Confirm successful login or understand failure.

**User actions:** Observe success redirect or error message.

**Touchpoints / channels:** `SuccessfulLogin.cshtml`, `NotAuthorized.razor`, `StatusMessage.razor`.

**User thoughts / questions:** "Am I logged in? Why did it fail?"

**Emotions:** Relieved, 4, on success; Frustrated, 2, on failure with unclear message.

**Pain points:** Generic error messages; no indication of which credential was wrong; `SuccessfulLogin` uses a no-layout page that may flash.

**Needs:** Clear success/failure feedback; specific error guidance.

**Success metrics:** Successful login rate; error-to-retry conversion; support tickets for login issues.

**Evidence:** Static code review assumption.

### Stage 6: Post-Auth Redirect

**User goal:** Reach the intended destination.

**User actions:** Observe automatic redirect to home or originally requested page.

**Touchpoints / channels:** Home page, originally requested route.

**User thoughts / questions:** "Am I in the right tenant? Why am I on the home page instead of where I was going?"

**Emotions:** Satisfied, 4, if destination is correct; Confused, 2, if redirected to home instead of original destination.

**Pain points:** Return URL is stored in `ReturnUrlManager` but home page may not honor it; tenant context resolution is asynchronous.

**Needs:** Reliable return-to-original-destination; immediate tenant context.

**Success metrics:** Return-to-original-destination success rate; tenant context resolution time.

**Evidence:** Static code review assumption.

### Stage 7: Logout

**User goal:** End the session securely.

**User actions:** Click logout; confirm; observe sign-out.

**Touchpoints / channels:** `/Account/SignOut`, `Logout.razor`.

**User thoughts / questions:** "Am I fully logged out? Is my session destroyed?"

**Emotions:** Satisfied, 4, on clean logout; Anxious, 2, if logout seems incomplete.

**Pain points:** No post-logout landing page confirmation; Duende logout iframe may not complete on all browsers.

**Needs:** Clear logout confirmation; session destruction guarantee.

**Success metrics:** Logout completion rate; session invalidation success.

**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points

1. Four separate login pages with no unified surface — occurs at method selection.
2. OTP delivery latency and lack of feedback — occurs during verification.
3. Return URL not reliably honored after login — occurs at post-auth redirect.

### Moments that matter

- **Moment of truth:** First OTP code arrives quickly and verifies cleanly.
- **Moment of friction:** User tries passkey, browser doesn't support it, no clear fallback.
- **Moment of delight:** Login remembers last method and auto-fills credentials.

### Design principles implied by the journey

- Provide one adaptive login surface, not four disconnected pages.
- Give immediate feedback on delivery status and verification progress.
- Always honor the return URL to preserve user intent.

## 6. Implementation Derivation

### Required artifacts

| Artifact | Why it is needed | Journey source | Priority |
|---|---|---|---|
| Unified login surface | Reduces method selection friction; shows available methods based on user config | Stage 2 | High |
| OTP delivery feedback | Shows send status, expiry countdown, resend cooldown | Stage 4 | High |
| Return URL reliability guarantee | Ensures user reaches intended destination after login | Stage 6 | High |
| Login method memory | Remembers last-used method for faster re-entry | Stage 2 | Medium |
| Post-logout confirmation | Confirms session ended; shows re-login option | Stage 7 | Medium |

### Feature slices

| Slice | User value | Includes | Excludes | Acceptance signal |
|---|---|---|---|---|
| Unified login page | User sees all available methods on one page | Adaptive method display, method switching, passkey detection | Account creation, MFA setup | Login method selection time decreases |
| OTP UX improvement | Faster verification with confidence | Delivery status, expiry timer, resend cooldown, auto-submit on 6 digits | New delivery channels | OTP verification time decreases |
| Return URL guarantee | User reaches intended destination | Return URL preservation through all auth flows, tenant context sync | Deep-link pre-auth caching | Return-to-destination success rate > 95% |

### Domain / data implications

- User record must store which auth methods are configured for quick method availability check.
- Login session must carry return URL through the full auth pipeline.
- Tenant context resolution must complete before post-auth redirect.

### UI implications

- Consolidate four login pages into one adaptive surface or improve cross-linking.
- Add OTP delivery status indicator and countdown timer.
- Add post-logout landing page with clear re-login CTA.

### API / service implications

- `IEmailAuthenticationService` and phone authentication service must expose delivery status.
- Claims transformation must complete synchronously before redirect.
- Return URL storage must survive full-page redirects and OTP server-side submits.

### Validation, permissions, and safety

- Rate limit OTP send requests per user/IP.
- Lockout after repeated failed attempts (currently not visible).
- Ensure passkey fallback gracefully degrades when browser lacks support.

### Tests and acceptance criteria

- Each login method completes authentication and resolves tenant context.
- Return URL is honored across all four login methods.
- OTP delivery feedback updates in real time.
- Logout fully destroys session and cookie.

### Metrics

- Login success rate per method.
- OTP resend frequency (indicating delivery issues).
- Return-to-destination success rate.
- Average time from login page to authenticated home.

## 7. Technical Detail Addendum

### Current implementation touchpoints

- `src/Hosts/Main/Pages/Identity/PhoneLogin.razor` — default login route (`/Account/Login`), renders `PhoneLoginForm`.
- `src/Hosts/Main/Pages/Identity/EmailLogin.razor` — `/Account/Login/Email`, renders `EmailLoginForm`.
- `src/Hosts/Main/Pages/Identity/PasswordLogin.razor` — `/Account/Login/Password`, renders `PasswordLoginForm`.
- `src/Hosts/Main/Pages/Identity/PasskeyLogin.razor` — `/Account/Login/Passkey`, renders `WebAuthnLoginForm`.
- `src/Hosts/Main/Pages/Identity/NotAuthorized.razor` — `/Account/Unauthorized`, shows access denied or login required.
- `src/Hosts/Main/Pages/Identity/Logout.razor` — `/Account/SignOut`, logout confirmation.
- `src/Interfaces/Interfaces.Accounts/Components/PhoneLoginForm.razor` — two-phase: send OTP then verify.
- `src/Interfaces/Interfaces.Accounts/Components/EmailLoginForm.razor` — two-phase: send OTP then verify.
- `src/Interfaces/Interfaces.Accounts/Components/PasswordLoginForm.razor` — username + password.
- `src/Interfaces/Interfaces.Accounts/Components/WebAuthnLoginForm.razor` — passkey with browser detection fallback.
- `src/Hosts/Main/Pages/Identity/PhoneLoginSubmit.cshtml` — server-side OTP verification for phone.
- `src/Hosts/Main/Pages/Identity/EmailLoginSubmit.cshtml` — server-side OTP verification for email.
- `src/Hosts/Main/Pages/Identity/PasswordLoginSubmit.cshtml` — server-side password verification.
- `src/Hosts/Main/Pages/Identity/SuccessfulLogin.cshtml` — post-auth redirect.
- `src/Hosts/Main/Authorization/ClaimsTransformation.cs` — enriches principal with PartyScope, PersonalScope, RolesScope.
- `src/Hosts/Main/DependencyInjection/HostAuthenticationExtensions.cs` — cookie auth config (14-day sliding expiry), JWT bearer, FIDO2.

### Issues to address

- Four separate login pages create navigation friction; consolidate into one adaptive surface or improve cross-linking with method availability hints.
- OTP forms post to server-side Razor Pages (`*.cshtml.cs`) which break the Blazor interactive rendering pipeline; consider migrating to Blazor-native form handling.
- `SuccessfulLogin.cshtml` uses a no-layout page that may flash before redirect; add a proper layout or use client-side routing.
- No visible account lockout or rate-limit feedback after repeated failures.
- Return URL is stored via `ReturnUrlManager` but not reliably passed through all four login flows, especially the server-side OTP submit endpoints.
- Passkey login shows "Passkey Not Supported" but no guidance on enabling passkeys or switching methods.
- 14-day sliding cookie expiry is generous; no user-facing session management or active session listing.
