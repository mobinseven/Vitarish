# PR #542 — Authentication Module Steps 1–7 Directional Commands

Date: 2026-06-28
Repository: `mobinseven/Vitarish`
PR: `#542`
Scope: Authentication module only. Ignore Main host build state unless an Authentication module project cannot build.

## Non-negotiable module constraints

Apply these constraints to every step below.

```txt
Do not create Authentication.Interface.
Keep the module shape:
src/Modules/Authentication/
  Contracts/
  Domain/
  Application/
  Infrastructure/
  Api/
  Module/

Domain must stay pure:
- no EF
- no ASP.NET Identity
- no Duende
- no HttpContext
- no SDKs
- no DTOs
- no UI, Razor, React

Application must orchestrate only:
- no EF
- no UserManager / SignInManager
- no Duende services
- no HttpContext / cookies
- no SMS/email/WebAuthn SDKs
- no Razor/React

Infrastructure owns concrete integrations:
- ASP.NET Identity
- Duende
- SMS/email providers or repo communication boundaries
- WebAuthn/FIDO2 SDK
- distributed cache
- cookies
- concrete request shielding

Api must be thin:
- request DTO -> command/query
- application result -> HTTP result
- no password/OTP/WebAuthn/Duende/storage logic
- no domain entity response bodies

Module is only composition:
- register Application handlers
- register Infrastructure adapters
- map Authentication API endpoints
```

---

# Legacy migration source map — use existing code as evidence, not as a layer shortcut

This is a migration. Before inventing new authentication behavior, mine the existing Accounts implementation and move the behavior into the new Authentication module with the correct boundary. Enhance the new implementation whenever possible.

## Source-of-truth legacy files to inspect first

```bash
# Return URL persistence and open-redirect protection
sed -n '1,180p' src/Interfaces/Interfaces.Accounts/Services/ReturnUrlManager.cs

# Phone OTP generation, sending, verification, and auto-registration behavior
sed -n '1,220p' src/Interfaces/Interfaces.Accounts/Services/SmsAuthenticationService.cs
sed -n '1,180p' src/Interfaces/Interfaces.Accounts/Components/PhoneLoginForm.razor.cs
sed -n '1,180p' src/Hosts/Main/Pages/Identity/PhoneLoginSubmit.cshtml.cs

# Email OTP generation, sending, verification, email body template, and auto-registration behavior
sed -n '1,240p' src/Interfaces/Interfaces.Accounts/Services/EmailAuthenticationService.cs
sed -n '1,180p' src/Interfaces/Interfaces.Accounts/Components/EmailLoginForm.razor.cs
sed -n '1,180p' src/Hosts/Main/Pages/Identity/EmailLoginSubmit.cshtml.cs

# SMS duplicate-request shield
sed -n '1,220p' src/Interfaces/Interfaces.Accounts/Abstractions/IVerificationSmsRequestShield.cs
sed -n '1,240p' src/Interfaces/Interfaces.Accounts/Services/InMemoryVerificationSmsRequestShield.cs

# Password login behavior and result semantics
sed -n '1,180p' src/Hosts/Main/Pages/Identity/PasswordLoginSubmit.cshtml.cs

# Passkey/WebAuthn client-side flow and legacy endpoint contract shape
sed -n '1,220p' src/Interfaces/Interfaces.Accounts/Components/WebAuthnLoginForm.razor.cs

# Duende logout context behavior
sed -n '1,180p' src/Hosts/Main/Pages/Identity/Logout.razor.cs

# Legacy DI registrations that reveal service lifetimes and hidden dependencies
sed -n '1,140p' src/Interfaces/Interfaces.Accounts/DependencyInjection/WebApplicationBuilderExtensions.cs
```

## Migration rule

```txt
Copy behavior, not placement.

Allowed:
- move pure URL validation into Domain;
- move orchestration from Razor/PageModel into Application handlers;
- move UserManager/SignInManager/token generation/Communications/Duende/WebAuthn implementations into Infrastructure;
- move endpoint contract shape into Contracts/Api.

Forbidden:
- do not move Razor/PageModel/Blazor component dependencies into Application;
- do not make Application reference Interfaces.Accounts;
- do not make Application reference Users.Infrastructure, EF, Identity, Duende, HttpContext, Communications SDKs, WebAuthn SDKs, Razor, or React;
- do not keep legacy redirects/status-cookie UI behavior as the API contract; convert it to typed result DTOs and ProblemDetails.
```

## Legacy behavior to preserve unless intentionally changed

```txt
ReturnUrlManager:
- stores return URL in a cookie named Identity.ReturnUrl;
- rejects non-local return URLs;
- supports consume semantics: read value, clear cookie, then redirect;
- cookie is HttpOnly, SameSite Strict, essential, path "/", Secure based on request HTTPS.

Phone login:
- unauthenticated begin finds or creates a user by phone number;
- phone code is generated with UserManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);
- SMS is queued with SmsCodeCommunicationCommand(phoneNumber, code);
- duplicate sends are shielded by VerificationSmsRequestShield keyed by phone login;
- completion verifies provider == TokenOptions.DefaultPhoneProvider;
- completion normalizes submitted code numerals to English numerals before verification;
- successful completion confirms phone number when needed;
- successful completion signs in the user with authentication method "mfa".

Email login:
- unauthenticated begin finds or creates a user by email;
- email code is generated with UserManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
- email is queued with EmailCommunicationCommand(email, "Code", textBody, htmlBody);
- HTML/text email template exists and must be reused or deliberately replaced;
- completion verifies provider == TokenOptions.DefaultEmailProvider;
- completion normalizes submitted code numerals to English numerals before verification;
- successful completion confirms email when needed;
- successful completion signs in the user with authentication method "mfa".

Password login:
- validates request before Identity calls;
- finds user by username in the legacy page, while current module also tries email;
- calls PasswordSignInAsync with remember-me behavior and lockout setting;
- distinguishes success, lockout, not allowed/access denied, invalid credentials, and invalid request;
- legacy UI writes short-lived status cookies; new API must return typed errors instead.

WebAuthn login:
- browser flow is begin -> browser credential prompt -> complete;
- legacy client posts to /api/webauthn/authenticate/begin and /api/webauthn/authenticate/complete;
- new module must expose equivalent behavior through /api/modules/authentication/login/passkey/begin and /complete;
- begin must return browser-usable PublicKeyCredentialRequestOptions JSON, not just a GUID.

Logout:
- uses Duende InteractionService.GetLogoutContextAsync(logoutId);
- respects ShowSignoutPrompt == false by allowing immediate logout;
- SignInManager signs out the current user;
- post-logout redirect URI and sign-out iframe URL come from Duende logout context.
```

## Quick migration diff command set

```bash
# Run from repo root on PR branch.
# Keep these outputs as your checklist while implementing.

rg "class ReturnUrlManager|Identity.ReturnUrl|ConsumeReturnUrl|IsLocalUrl" src/Interfaces/Interfaces.Accounts src/Hosts/Main
rg "GenerateTwoFactorTokenAsync|VerifyTwoFactorTokenAsync|DefaultPhoneProvider|DefaultEmailProvider" src/Interfaces/Interfaces.Accounts src/Hosts/Main
rg "SmsCodeCommunicationCommand|EmailCommunicationCommand|ICommunicationCommandQueue" src/Interfaces src/Modules
rg "VerificationSmsShield|TryAcquireSendSlot|ReleaseSendSlot" src/Interfaces/Interfaces.Accounts src/Modules/Authentication
rg "ConvertToEnglishNumerals|PhoneNumberConfirmed|EmailConfirmed|SignInAsync\(user, true, \"mfa\"\)" src/Hosts/Main src/Interfaces/Interfaces.Accounts src/Modules/Authentication
rg "PasswordSignInAsync|IsLockedOut|IsNotAllowed|AccessDenied|InvalidCredentials" src/Hosts/Main src/Modules/Authentication
rg "webauthn|WebAuthn|Fido|PublicKeyCredential|authenticate/begin|authenticate/complete" src/Interfaces src/Hosts src/Modules
rg "GetLogoutContextAsync|PostLogoutRedirectUri|SignOutIFrameUrl|ShowSignoutPrompt" src/Hosts/Main src/Modules/Authentication
```

---

# Step 1 — Make challenge lifecycle real

## Goal

Make phone, email, and passkey authentication challenges coherent, persisted, stateful, single-use, expiry-aware, and attempt-limited.

Currently, the domain has lifecycle primitives, but the Application completion flow does not load/update challenges. Fix that first because every OTP/passkey flow depends on it.

## Directional command

```txt
Implement a real challenge lifecycle in the Authentication module.
Do not treat a challenge ID as a verifier-specific token payload.
A challenge ID must identify persisted challenge state.
Completion must load the challenge, validate purpose/status/expiry/attempts, verify the submitted proof, persist failure/completion state, and prevent reuse.
Keep Domain pure and keep concrete persistence in Infrastructure.
```

## Files to edit

```txt
src/Modules/Authentication/Domain/Challenges/AuthenticationChallenge.cs
src/Modules/Authentication/Domain/Challenges/AuthenticationChallengeId.cs
src/Modules/Authentication/Domain/Challenges/AuthenticationChallengePolicy.cs
src/Modules/Authentication/Domain/Challenges/AuthenticationChallengePurpose.cs
src/Modules/Authentication/Domain/Challenges/AuthenticationChallengeStatus.cs
src/Modules/Authentication/Application/Abstractions/IAuthenticationChallengeStore.cs
src/Modules/Authentication/Application/Results/AuthenticationFailure.cs
src/Modules/Authentication/Application/Results/ChallengeResult.cs
src/Modules/Authentication/Application/Handlers/LoginWithPasswordHandler.cs
src/Modules/Authentication/Infrastructure/Challenges/DistributedAuthenticationChallengeStore.cs
src/Modules/Authentication/Infrastructure/Challenges/InMemoryAuthenticationChallengeStore.cs
```

## Domain commands

```txt
Update AuthenticationChallenge so it owns lifecycle transitions, not handlers.
Add enough state to support all authentication challenge flows:
- ChallengeId
- Purpose
- UserId, nullable only when the flow truly supports anonymous begin
- Destination, e.g. phone/email/login hint, nullable only when not applicable
- TokenPurpose or verification purpose string used by the Identity token verifier
- CreatedAt
- ExpiresAt
- MaximumAttempts
- FailedAttempts
- Status: Pending, Completed, Expired, Exhausted, Cancelled if needed
- CompletedAt if useful

Do not put the raw OTP/code in Domain.
Do not put UserManager, SignInManager, HttpContext, cookies, or cache in Domain.
```

```txt
Add or strengthen these domain methods:
- bool CanAttempt(DateTimeOffset now)
- AuthenticationDomainError? ValidateAttempt(DateTimeOffset now, AuthenticationChallengePurpose expectedPurpose)
- void RegisterFailure(DateTimeOffset now)
- void Complete(DateTimeOffset now)
- bool IsExpired(DateTimeOffset now)
- bool IsReusable == false after Completed/Expired/Exhausted

RegisterFailure must:
- do nothing unsafe for completed challenges;
- mark Expired if now >= ExpiresAt;
- increment FailedAttempts only for pending, non-expired challenges;
- mark Exhausted when RemainingAttempts reaches 0.

Complete must:
- throw or return a domain error when challenge is expired, completed, or exhausted;
- mark Status = Completed;
- record CompletedAt if the model has the property.
```

```txt
Update AuthenticationChallengePolicy so challenge creation is centralized.
Expose factory-like behavior such as:
- CreatePhoneLoginChallenge(userId, phoneNumber, tokenPurpose, now)
- CreateEmailLoginChallenge(userId, email, tokenPurpose, now)
- CreatePasskeyLoginChallenge(userIdOrLoginHint, challengeReference, now)

Use policy constants/options for:
- default expiry
- maximum attempts
- allowed purposes

Do not hard-code DateTimeOffset.UtcNow inside Domain policy if the caller can pass now.
```


## Existing-code migration guidance for Step 1

```txt
Use the legacy submit page models to define the challenge state you actually need.
The current PR verifier tries to parse challengeId with Split(':', 3), but the migration should not encode Identity provider/user/purpose in the challenge ID.
Instead, persist that metadata in AuthenticationChallenge.
```

```txt
Extract these facts from legacy completion code:
- PhoneLoginSubmit validates Input.Provider == TokenOptions.DefaultPhoneProvider.
- EmailLoginSubmit validates Input.Provider == TokenOptions.DefaultEmailProvider.
- Both submit handlers normalize Input.Code with ConvertToEnglishNumerals() before verification.
- Both fetch the user by submitted UserId before token verification.
- Both confirm PhoneNumber/Email after successful verification.
- Both sign in using SignInManager.SignInAsync(user, true, "mfa").
```

```txt
Translate those legacy facts into new module state:
AuthenticationChallenge must persist:
- UserId: the Identity user ID resolved during begin;
- Destination: phone number or email used during begin;
- Purpose: PhoneLogin, EmailLogin, PasskeyLogin;
- IdentityTokenProvider: TokenOptions.DefaultPhoneProvider or TokenOptions.DefaultEmailProvider for OTP flows;
- IdentityTokenPurpose: keep this separate if the repo uses a custom token purpose string;
- CreatedAt, ExpiresAt, MaximumAttempts, FailedAttempts, Status;
- optional TransportTraceId or DeliveryId only if Communications returns one.
```

```txt
Create a small local normalization utility inside Authentication.Application or Authentication.Domain.Security if it is pure.
Do not reference Vitarish.Modules.Base.Interface.Extensions from Application just to call ConvertToEnglishNumerals(), because that would pull Interface/UI concerns into the module core.
Implement only the needed digit normalization, covering Persian/Arabic digits if the legacy helper did.
```

```txt
After success, the completion handler must call an Infrastructure-backed abstraction that performs the legacy confirmation/sign-in behavior:
- confirm phone for PhoneLogin when not confirmed;
- confirm email for EmailLogin when not confirmed;
- sign in with method "mfa";
- return canonical user ID and profile-completion state.

Do not let Application call IUserPhoneNumberStore, IUserEmailStore, UserManager, or SignInManager directly.
```

## Application abstraction commands

```txt
Update IAuthenticationChallengeStore to support a full lifecycle.
It must be possible to persist state changes after failed or successful completion.
Use one of these shapes:

Option A:
- Task SaveAsync(AuthenticationChallenge challenge, CancellationToken ct)
- Task<AuthenticationChallenge?> GetAsync(AuthenticationChallengeId id, CancellationToken ct)
- Task ReplaceAsync(AuthenticationChallenge challenge, CancellationToken ct)

Option B:
- Task UpsertAsync(AuthenticationChallenge challenge, CancellationToken ct)
- Task<AuthenticationChallenge?> GetAsync(AuthenticationChallengeId id, CancellationToken ct)

Pick the style that best matches repository conventions.
Do not add Infrastructure-specific cache APIs to Application.
```

```txt
Add a ChallengeVerificationResult or equivalent Application result if needed.
Required failure reasons:
- ChallengeNotFound
- ChallengeExpired
- ChallengeAlreadyUsed
- ChallengeAttemptsExhausted
- InvalidVerificationCode
- InvalidChallengePurpose
- InvalidInput
- ProviderFailure

Keep these as Application-level failures based on Domain errors.
```

## Application handler commands

```txt
Delete the current shared Complete implementation that only calls verifier.VerifyAsync(challengeId, code).
Replace it with a lifecycle-aware completion flow:

1. Validate challengeId and code are not empty.
2. Parse AuthenticationChallengeId.
3. Load challenge from IAuthenticationChallengeStore.
4. If missing, return ChallengeNotFound or InvalidCredentials depending on desired information disclosure policy.
5. Validate expected purpose, e.g. PhoneLogin or EmailLogin.
6. Validate challenge.CanAttempt(now).
7. Call the verifier with challenge metadata, not only raw challengeId.
8. If verification fails:
   - challenge.RegisterFailure(now)
   - store.ReplaceAsync or UpsertAsync(challenge)
   - return InvalidVerificationCode or AttemptsExhausted based on updated state
9. If verification succeeds:
   - challenge.Complete(now)
   - persist updated challenge
   - sign in the canonical challenge.UserId
   - resolve redirect
   - return LoginResult
```

```txt
Introduce a clock abstraction only in Application if repository conventions allow it.
Suggested abstraction:
- IAuthenticationClock.UtcNow

If no clock abstraction exists in the repo, keep DateTimeOffset.UtcNow in Application for this PR and make the Domain methods accept now.
Do not put clock service in Domain.
```

## Infrastructure store commands

```txt
Update DistributedAuthenticationChallengeStore snapshot serialization to include every lifecycle field.
The cache key must be namespaced:
- authentication:challenge:{challengeId}

AbsoluteExpiration must match challenge.ExpiresAt.
Do not use raw challengeId alone as the distributed cache key.
```

```txt
Update InMemoryAuthenticationChallengeStore to behave like a stateful store.
It must support get + replace/upsert.
Make it thread-safe using ConcurrentDictionary.
Do not register in-memory store as production default unless explicitly conditioned for development/test.
```

## Acceptance checks

```txt
Search checks:
rg "AuthenticationChallengeHandlerCore.Complete" src/Modules/Authentication
rg "Guid.NewGuid\(\)\.ToString\(\"N\"\)" src/Modules/Authentication/Application/Handlers
rg "Split\(':', 3\)" src/Modules/Authentication
```

Expected:

```txt
- No completion flow depends on parsing challengeId into verifier payload parts.
- Completion loads the challenge from IAuthenticationChallengeStore.
- Failed attempts are persisted.
- Completed challenges are persisted and cannot be reused.
- Expired challenges are rejected.
```

---

# Step 2 — Fix OTP begin/complete end-to-end

## Goal

Make phone and email login actually work from begin to complete.

Begin must resolve the user/destination, generate a verification token/code, persist a challenge, send the code through the correct channel, and return a challenge DTO.

Complete must verify the code against the persisted challenge, sign in the canonical user, and return a correct redirect result.

## Directional command

```txt
Replace the current superficial phone/email challenge handlers with complete OTP flows.
Do not return a challenge unless the code was generated and the send step either succeeded or intentionally used a dev/test sender.
Do not use a challenge ID format that embeds user ID or purpose as colon-separated data.
The challenge store is the source of truth for user, destination, token purpose, expiry, attempts, and status.
```

## Existing-code migration guidance for Step 2

```txt
Use SmsAuthenticationService and EmailAuthenticationService as the behavioral source, but split their mixed responsibilities across the new layers.
Legacy services combine user lookup/registration, token generation, verification, and delivery.
The new module must separate those responsibilities.
```

```txt
Map legacy phone behavior:
Legacy SmsAuthenticationService.GetOrRegisterUser(phoneNumber)
  -> Infrastructure implementation of IAuthenticationUserDirectory.GetOrRegisterByPhoneAsync(phoneNumber, ct)
  -> uses UserManager.Users.SingleOrDefaultAsync(u => u.PhoneNumber == phoneNumber)
  -> creates User, SetUserNameAsync(phoneNumber), SetPhoneNumberAsync(phoneNumber), CreateAsync(user) only if this auto-registration rule is still desired.

Legacy SmsAuthenticationService.SendCode(user, phoneNumber)
  -> Infrastructure implementation of IAuthenticationCodeIssuer.GeneratePhoneLoginCodeAsync(userId, ct)
  -> uses UserManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider)
  -> Application then calls IPhoneAuthenticationCodeSender.SendAsync(phoneNumber, code, PhoneLogin, ct).

Legacy SmsAuthenticationService.VerifyCode(user, code)
  -> Infrastructure implementation of IAuthenticationCodeVerifier.VerifyAsync(challenge.UserId, TokenOptions.DefaultPhoneProvider, normalizedCode, ct)
  -> uses UserManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider, normalizedCode).
```

```txt
Map legacy email behavior:
Legacy EmailAuthenticationService.GetOrRegisterUser(email)
  -> Infrastructure implementation of IAuthenticationUserDirectory.GetOrRegisterByEmailAsync(email, ct)
  -> uses UserManager.Users.SingleOrDefaultAsync(u => u.Email == email)
  -> creates User, SetUserNameAsync(email), SetEmailAsync(email), CreateAsync(user) only if this auto-registration rule is still desired.

Legacy EmailAuthenticationService.SendCode(user, email)
  -> Infrastructure implementation of IAuthenticationCodeIssuer.GenerateEmailLoginCodeAsync(userId, ct)
  -> uses UserManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider)
  -> Application then calls IEmailAuthenticationCodeSender.SendAsync(email, code, EmailLogin, ct).

Legacy EmailAuthenticationService.VerifyCode(user, code)
  -> Infrastructure implementation of IAuthenticationCodeVerifier.VerifyAsync(challenge.UserId, TokenOptions.DefaultEmailProvider, normalizedCode, ct)
  -> uses UserManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, normalizedCode).
```

```txt
Decide and document the auto-registration rule before implementation.
The legacy code auto-creates a user for unknown phone/email login begins.
Keep it only if authentication-by-phone/email is intentionally a registration path.
If this is no longer desired, replace GetOrRegisterByPhone/Email with FindByPhone/Email and return a uniform InvalidCredentials or ChallengeAcceptedButNoDelivery response to avoid user enumeration.
```

```txt
Do not return the generated OTP in the API response.
The response must include only challenge metadata safe for the browser:
- ChallengeId
- Purpose/Method
- ExpiresAt
- RemainingAttempts if acceptable
- DestinationHint masked if useful
```

```txt
The complete handler must not trust any user ID, provider, or destination from the client.
The client only submits challengeId + code + optional returnUrl.
Load user/provider/destination from persisted challenge state.
```

## Files to edit

```txt
src/Modules/Authentication/Application/Commands/BeginPhoneLoginCommand.cs
src/Modules/Authentication/Application/Commands/BeginEmailLoginCommand.cs
src/Modules/Authentication/Application/Commands/CompletePhoneLoginCommand.cs
src/Modules/Authentication/Application/Commands/CompleteEmailLoginCommand.cs
src/Modules/Authentication/Application/Handlers/BeginPhoneLoginHandler.cs
src/Modules/Authentication/Application/Handlers/BeginEmailLoginHandler.cs
src/Modules/Authentication/Application/Handlers/CompletePhoneLoginHandler.cs
src/Modules/Authentication/Application/Handlers/CompleteEmailLoginHandler.cs
src/Modules/Authentication/Application/Abstractions/IAuthenticationUserDirectory.cs
src/Modules/Authentication/Application/Abstractions/IAuthenticationCodeVerifier.cs
src/Modules/Authentication/Application/Abstractions/IAuthenticationCodeSender.cs
src/Modules/Authentication/Application/Abstractions/IAuthenticationSignInService.cs
src/Modules/Authentication/Application/Results/LoginResult.cs
src/Modules/Authentication/Application/Results/ChallengeResult.cs
src/Modules/Authentication/Infrastructure/Identity/AspNetIdentityCodeVerifier.cs
src/Modules/Authentication/Infrastructure/Identity/AspNetIdentityAuthenticationUserDirectory.cs
src/Modules/Authentication/Infrastructure/Identity/AspNetIdentitySignInService.cs
```

## Application abstraction commands

```txt
Update IAuthenticationUserDirectory so Application can resolve users without knowing Identity.
It should support the minimum needed queries:
- FindByUserNameOrEmailAsync(string login, CancellationToken ct)
- FindByEmailAsync(string email, CancellationToken ct)
- FindByPhoneNumberAsync(string phoneNumber, CancellationToken ct)
- GetCurrentAsync(CancellationToken ct)

Return AuthenticationUserDescriptor with:
- UserId
- UserName if useful
- DisplayName
- Email
- PhoneNumber
- RequiresProfileCompletion
- IsLockedOut if available and needed
- IsAllowedToSignIn if available and needed

Keep descriptor in Application abstractions or Results, not Contracts, unless repository convention says otherwise.
```

```txt
Split token generation from token verification if needed.
The current IAuthenticationCodeVerifier only verifies.
Begin also needs to generate a token/code.
Use either:

Option A:
- IAuthenticationCodeService.GenerateAsync(userId, purpose, channel, ct)
- IAuthenticationCodeService.VerifyAsync(userId, purpose, code, ct)

Option B:
- IAuthenticationCodeGenerator.GenerateAsync(...)
- IAuthenticationCodeVerifier.VerifyAsync(...)

Do not make Application call UserManager directly.
```

```txt
Fix IAuthenticationCodeSender so channel-specific routing is explicit.
Use one of these forms:

Preferred:
- IPhoneAuthenticationCodeSender.SendAsync(phoneNumber, code, purpose, ct)
- IEmailAuthenticationCodeSender.SendAsync(email, code, purpose, ct)

Acceptable:
- IAuthenticationCodeSender.SendAsync(AuthenticationMethod method, string destination, string code, AuthenticationChallengePurpose purpose, ct)

Do not register only EmailAuthenticationCodeSender as IAuthenticationCodeSender while phone flow uses the same abstraction.
```

## Begin phone commands

```txt
Implement BeginPhoneLoginHandler as a real flow:

1. Validate PhoneNumber is not empty.
2. Normalize phone number only if a repo-approved phone normalizer exists.
3. Resolve user through IAuthenticationUserDirectory.FindByPhoneNumberAsync.
4. If not found, return a structured failure.
   Prefer a privacy-preserving response only if the existing auth UX requires account enumeration resistance.
   Regardless, be explicit in Application failure type.
5. Generate token/code using Identity-backed code generator abstraction.
6. Create AuthenticationChallenge with:
   - purpose = PhoneLogin
   - userId = resolved canonical user id
   - destination = normalized phone number
   - tokenPurpose = Identity token purpose used by verifier
   - expiresAt = now + configured expiry
   - maxAttempts = configured max attempts
7. Save challenge.
8. Send code through phone sender.
9. If send fails:
   - either mark challenge cancelled/failed or remove/expire it if the store supports that;
   - return ProviderFailure with safe message.
10. Return ChallengeResult with challenge id, purpose, expiry, and remaining attempts.
```

## Begin email commands

```txt
Implement BeginEmailLoginHandler as a real flow:

1. Validate Email is not empty and has a safe email shape.
2. Normalize email consistently with existing repo conventions.
3. Resolve user through IAuthenticationUserDirectory.FindByEmailAsync.
4. If not found, return structured failure.
5. Generate token/code through Identity-backed generator abstraction.
6. Create AuthenticationChallenge with:
   - purpose = EmailLogin
   - userId = canonical user id
   - destination = normalized email
   - tokenPurpose = Identity token purpose used by verifier
   - expiresAt = now + configured expiry
   - maxAttempts = configured max attempts
7. Save challenge.
8. Send code through email sender.
9. On send failure, return ProviderFailure and avoid leaving a usable challenge if possible.
10. Return ChallengeResult.
```

## Complete phone/email commands

```txt
Implement CompletePhoneLoginHandler and CompleteEmailLoginHandler separately or through a shared private service.
The shared service must accept expected purpose and channel.

Completion algorithm:

1. Validate ChallengeId and Code.
2. Load challenge.
3. Validate challenge purpose matches the endpoint.
4. Validate challenge is pending and not expired/exhausted/completed.
5. Verify code using stored challenge.UserId and challenge.TokenPurpose.
6. On invalid code:
   - RegisterFailure(now)
   - persist challenge
   - return InvalidVerificationCode, or AttemptsExhausted if now exhausted.
7. On valid code:
   - Complete(now)
   - persist challenge
   - SignInAsync(challenge.UserId, rememberMe if supported by command/request)
   - resolve redirect using return URL and profile completion state
   - return LoginResult with canonical user id.
```

## Identity adapter commands

```txt
Update AspNetIdentityCodeVerifier so it does not parse challengeId.
It must verify using userId + tokenPurpose + code.
Suggested signature:
- VerifyAsync(string userId, string tokenPurpose, string code, CancellationToken ct)

If you add a code generator:
- GenerateAsync(string userId, string tokenPurpose, CancellationToken ct)

Implementation commands:
1. Find User by userId.
2. Return failure if missing.
3. Generate/Verify with UserManager and TokenOptions.DefaultProvider unless legacy code uses another provider.
4. Keep token purpose stable and named, e.g. Authentication:PhoneLogin or Authentication:EmailLogin.
```

## Acceptance checks

```txt
rg "Split\(':', 3\)" src/Modules/Authentication
rg "BeginPhoneLoginHandler\(IAuthenticationChallengeStore store\)" src/Modules/Authentication
rg "BeginEmailLoginHandler\(IAuthenticationChallengeStore store\)" src/Modules/Authentication
rg "Task.CompletedTask" src/Modules/Authentication/Infrastructure/Email src/Modules/Authentication/Infrastructure/Sms
```

Expected:

```txt
- Begin phone/email uses user directory, token generation, challenge store, and sender.
- Complete phone/email loads and mutates stored challenge state.
- Challenge ID is opaque and does not encode verifier data.
- Canonical user ID is used for sign-in and LoginResult.
```

---

# Step 3 — Replace no-op SMS/email adapters

## Goal

Move real SMS/email authentication code sending behavior into Authentication.Infrastructure and wire it correctly.

Current sender classes are no-op. That means begin phone/email can pretend to work while no user receives any code.

## Directional command

```txt
Replace no-op EmailAuthenticationCodeSender and SmsAuthenticationCodeSender with real Infrastructure adapters.
Use existing repository communication boundaries or legacy Accounts behavior.
Do not put provider SDK or Communications module dependencies in Application.
Make sender failures explicit and mappable to ProblemDetails.
```

## Files to inspect first

Run these repository searches before editing:

```bash
rg "SmsAuthenticationService|EmailAuthenticationService|VerificationSmsRequestShield|Send.*Sms|Send.*Email|IEmail|ISms|ICommunication|Communications" src/Interfaces src/Modules src/Libraries
rg "UserManager.*Generate|GenerateUserTokenAsync|VerifyUserTokenAsync|EmailToken|Phone" src/Interfaces src/Modules
rg "IAuthenticationCodeSender|EmailAuthenticationCodeSender|SmsAuthenticationCodeSender" src/Modules/Authentication
```

Use the findings to choose the existing repo boundary. Do not invent a new provider if one already exists.

## Existing-code migration guidance for Step 3

```txt
Use the legacy Communications queue calls directly as the first implementation target unless a newer Communications module boundary exists.
Search first; do not invent a new delivery provider.
```

```txt
Legacy SMS delivery source:
SmsAuthenticationService.SendCode(user, phonenumber)
- validates user and phone number;
- generates a phone token with TokenOptions.DefaultPhoneProvider;
- queues SmsCodeCommunicationCommand(phonenumber, code) through ICommunicationCommandQueue.

New SMS adapter target:
SmsAuthenticationCodeSender must inject the same repo communication boundary, probably ICommunicationCommandQueue.
It must enqueue SmsCodeCommunicationCommand(phoneNumber, code).
It must not generate the token if token generation has been moved to IAuthenticationCodeIssuer.
```

```txt
Legacy email delivery source:
EmailAuthenticationService.SendCode(user, email)
- validates user and email;
- generates an email token with TokenOptions.DefaultEmailProvider;
- builds HTML/text body with GetAuthenticationCodeEmail(user.UserName, code);
- queues EmailCommunicationCommand(email, "Code", textBody, htmlBody).

New email adapter target:
EmailAuthenticationCodeSender must inject ICommunicationCommandQueue.
It must reuse or move the GetAuthenticationCodeEmail template logic into Infrastructure/Email, e.g. AuthenticationCodeEmailTemplate.
It must enqueue EmailCommunicationCommand(email, subject, textBody, htmlBody).
```

```txt
Move the SMS shield almost directly from legacy code:
- IVerificationSmsRequestShield -> Authentication.Infrastructure/Sms/VerificationSmsRequestShield or Application abstraction if Application must decide cooldown;
- InMemoryVerificationSmsRequestShield -> Authentication.Infrastructure/Sms/InMemoryVerificationSmsRequestShield;
- VerificationSmsShieldOptions -> Authentication.Infrastructure/Options/VerificationShieldOptions.

Preserve behavior:
- key is required;
- cooldown returns a shielded decision with remaining cooldown;
- expired requests are cleaned periodically;
- capacity is bounded by VerificationSmsMaximumRecentRequestCount;
- release slot when provider send fails.
```

```txt
Registration command:
Do not register only EmailAuthenticationCodeSender for IAuthenticationCodeSender.
The current PR registration makes phone delivery unreachable if a single sender abstraction is used.
Use explicit channel senders or a dispatcher that depends on both concrete senders.
```

## Files to edit

```txt
src/Modules/Authentication/Application/Abstractions/IAuthenticationCodeSender.cs
src/Modules/Authentication/Infrastructure/Email/EmailAuthenticationCodeSender.cs
src/Modules/Authentication/Infrastructure/Sms/SmsAuthenticationCodeSender.cs
src/Modules/Authentication/Infrastructure/Sms/VerificationSmsRequestShield.cs
src/Modules/Authentication/Infrastructure/Sms/InMemoryVerificationSmsRequestShield.cs
src/Modules/Authentication/Infrastructure/Options/VerificationShieldOptions.cs
src/Modules/Authentication/Infrastructure/DependencyInjection/AuthenticationInfrastructureServiceCollectionExtensions.cs
Directory.Packages.props if new package references are required
src/Modules/Authentication/Infrastructure/Authentication.Infrastructure.csproj if existing module references are required
```

## Sender abstraction commands

```txt
Prefer separate channel abstractions:

Application/Abstractions/IPhoneAuthenticationCodeSender.cs
Application/Abstractions/IEmailAuthenticationCodeSender.cs

Define:
- Task SendAsync(string phoneNumber, string code, AuthenticationChallengePurpose purpose, CancellationToken ct)
- Task SendAsync(string email, string code, AuthenticationChallengePurpose purpose, CancellationToken ct)

If you keep one abstraction, include channel/method in the method signature and implement a dispatcher.
```

```txt
Do not swallow provider exceptions silently.
Wrap provider errors into an Application failure at the handler boundary.
If sender abstraction throws infrastructure exceptions, catch them in the Application handler and return ProviderFailure.
If repository conventions prefer Result types, return a sender result instead of throwing.
```

## SMS adapter commands

```txt
Implement SmsAuthenticationCodeSender using the existing SMS/Communications boundary.

Required behavior:
1. Validate destination phone number is not empty.
2. Apply VerificationSmsRequestShield before sending.
3. Format the authentication message using existing localization/template conventions if available.
4. Send the OTP through the existing SMS service/boundary.
5. Propagate cancellation token.
6. Return explicit failure or throw a typed exception when provider fails.
7. Do not log the OTP/code in production logs.
8. Do not store the OTP in the SMS sender.
```

```txt
Update VerificationSmsRequestShield:
- enforce per-phone cooldown;
- enforce per-phone and possibly per-IP rate limits if available;
- use options from VerificationShieldOptions;
- use distributed cache or an existing repo throttling service if available;
- keep InMemoryVerificationSmsRequestShield as dev/test only.
```

## Email adapter commands

```txt
Implement EmailAuthenticationCodeSender using the existing email/Communications boundary.

Required behavior:
1. Validate destination email is not empty.
2. Format subject/body using existing repo conventions.
3. Send OTP through the existing email service/boundary.
4. Propagate cancellation token.
5. Return explicit failure or throw a typed exception when provider fails.
6. Do not log the OTP/code in production logs.
7. Do not store the OTP in the email sender.
```

## DI commands

```txt
Fix AuthenticationInfrastructureServiceCollectionExtensions registrations.

If using separate abstractions:
- services.AddScoped<IPhoneAuthenticationCodeSender, SmsAuthenticationCodeSender>();
- services.AddScoped<IEmailAuthenticationCodeSender, EmailAuthenticationCodeSender>();

If using a dispatcher:
- services.AddScoped<SmsAuthenticationCodeSender>();
- services.AddScoped<EmailAuthenticationCodeSender>();
- services.AddScoped<IAuthenticationCodeSender, AuthenticationCodeSenderDispatcher>();

Do not leave SMS unreachable.
Do not register EmailAuthenticationCodeSender as the only IAuthenticationCodeSender if phone flow uses the same interface.
```

## Project reference commands

```txt
If Infrastructure needs Communications.Application abstractions, add the ProjectReference to Authentication.Infrastructure only.
Never add Communications references to Authentication.Application or Authentication.Domain.

Example shape:
< ProjectReference Include="..\..\Communications\Application\Communications.Application.csproj" />

Use the exact existing project name/path from the repo.
Do not guess; verify with ls/rg first.
```

## Acceptance checks

```bash
rg "Task.CompletedTask" src/Modules/Authentication/Infrastructure/Email src/Modules/Authentication/Infrastructure/Sms
rg "AddScoped<IAuthenticationCodeSender, EmailAuthenticationCodeSender>" src/Modules/Authentication
rg "Communications|Sms|Email" src/Modules/Authentication/Application src/Modules/Authentication/Domain
```

Expected:

```txt
- No no-op sender remains.
- SMS and email are both reachable from the correct begin handlers.
- Provider failure is converted to structured Application failure.
- Application and Domain remain free of concrete provider dependencies.
```

---

# Step 4 — Implement real WebAuthn adapter

## Existing-code migration guidance for Step 4

```txt
Use WebAuthnLoginForm as the client-flow source.
It shows the required browser contract:
1. POST begin with username/login hint.
2. Receive PublicKeyCredentialRequestOptions-like JSON.
3. Browser calls navigator.credentials.get through IWebAuthnInterop.
4. POST credential response to complete.
5. On success, redirect to consumed return URL or root.
```

```txt
New endpoint mapping:
Legacy client path /api/webauthn/authenticate/begin
  -> POST /api/modules/authentication/login/passkey/begin

Legacy client path /api/webauthn/authenticate/complete
  -> POST /api/modules/authentication/login/passkey/complete
```

```txt
The current WebAuthnAuthenticationService returning Guid.NewGuid().ToString("N") is not enough.
Begin must return actual browser-usable credential request options.
Complete must validate the credential assertion and return canonical user ID.
```

```txt
If the repo already has WebAuthn/FIDO2 server services outside Interfaces.Accounts, reuse them in Authentication.Infrastructure.
If not, create a narrow Infrastructure adapter around the chosen FIDO2/WebAuthn library and keep its request/response JSON serialization there or in Contracts.
Application should only see:
- ChallengeId or PasskeyChallengeReference;
- UserId/login hint;
- success/failure;
- canonical authenticated user ID.
```

```txt
Do not port IWebAuthnInterop into the backend module.
IWebAuthnInterop is browser/client interop and belongs to Phase 2 client/UI migration.
Only server-side WebAuthn option generation and assertion validation belong to Authentication.Infrastructure.
```


## Goal

Replace the placeholder passkey service with a real WebAuthn/FIDO2 authentication flow.

The begin endpoint must produce server authentication options and persist challenge state. The complete endpoint must validate the browser credential response and resolve the authenticated user ID.

## Directional command

```txt
Replace WebAuthnAuthenticationService placeholder behavior with real WebAuthn/FIDO2 server integration in Infrastructure.
Do not fake passkey completion.
Do not return random GUIDs as WebAuthn options.
Do not always return null from CompleteAsync.
Preserve challenge persistence, expiry, and replay prevention.
```

## Files to inspect first

Run:

```bash
rg "WebAuthn|Fido2|Passkey|Credential|Assertion|MakeAssertion|GetAssertion|CompletePasskey|BeginPasskey" src/Interfaces src/Modules src/Libraries
rg "Fido2|Fido2.AspNet|WebAuthn" Directory.Packages.props src/**/*.csproj
```

Reuse existing legacy Accounts behavior if present.

## Files to edit

```txt
src/Modules/Authentication/Application/Abstractions/IWebAuthnAuthenticationService.cs
src/Modules/Authentication/Application/Handlers/BeginPasskeyLoginHandler.cs
src/Modules/Authentication/Application/Handlers/CompletePasskeyLoginHandler.cs
src/Modules/Authentication/Infrastructure/WebAuthn/WebAuthnAuthenticationService.cs
src/Modules/Authentication/Infrastructure/DependencyInjection/AuthenticationInfrastructureServiceCollectionExtensions.cs
src/Modules/Authentication/Infrastructure/Options/AuthenticationChallengeOptions.cs
src/Modules/Authentication/Contracts/Requests/BeginPasskeyAuthenticationRequest.cs
src/Modules/Authentication/Contracts/Requests/CompletePasskeyAuthenticationRequest.cs
src/Modules/Authentication/Contracts/Responses/AuthenticationChallengeDto.cs
```

## Contract commands

```txt
Ensure passkey begin can return enough browser-facing data.
A passkey begin response probably needs more than challenge id + expiry.
Choose a stable DTO field such as:
- public string OptionsJson { get; init; }

or create a specific DTO:
- BeginPasskeyAuthenticationResponse / PasskeyAuthenticationOptionsDto

Do not leak server-only credential records.
Keep transport-safe JSON.
```

```txt
Ensure passkey complete request contains the browser credential response.
Current CompletePasskeyAuthenticationRequest.CredentialJson is acceptable if the frontend will send raw WebAuthn JSON.
Validate size and emptiness in Application.
```

## Application commands

```txt
Update IWebAuthnAuthenticationService to model real begin/complete behavior.
Suggested shape:

- Task<WebAuthnBeginResult> BeginAsync(string? userNameOrEmail, CancellationToken ct)
- Task<WebAuthnCompleteResult> CompleteAsync(string challengeId, string credentialJson, CancellationToken ct)

WebAuthnBeginResult should include:
- ChallengeId
- OptionsJson
- ExpiresAt
- RemainingAttempts

WebAuthnCompleteResult should include:
- bool Succeeded
- string? UserId
- AuthenticationFailure? Failure

Keep SDK-specific types out of Application.
```

```txt
Update BeginPasskeyLoginHandler:
1. Validate optional login hint if present.
2. Call IWebAuthnAuthenticationService.BeginAsync.
3. Return ChallengeResult or a passkey-specific result containing OptionsJson.
4. Do not generate DateTimeOffset.UtcNow.AddMinutes(5) in handler if service/store owns passkey challenge state.
```

```txt
Update CompletePasskeyLoginHandler:
1. Validate challengeId and credentialJson.
2. Call IWebAuthnAuthenticationService.CompleteAsync.
3. On failure, return structured AuthenticationFailure.
4. On success, sign in canonical user ID with IAuthenticationSignInService.
5. Resolve redirect using IAuthenticationRedirectResolver and profile completion state if available.
6. Return LoginResult.
```

## Infrastructure commands

```txt
Implement WebAuthnAuthenticationService using the repo's existing FIDO2/WebAuthn server integration.

Begin behavior:
1. Resolve user or allowed credentials if login hint is supplied.
2. Generate assertion/authentication options using the WebAuthn/FIDO2 server SDK.
3. Persist WebAuthn challenge/options reference through IAuthenticationChallengeStore or a dedicated Infrastructure cache store behind Application abstraction.
4. Return browser-safe options JSON and opaque challengeId.
5. Set expiry consistent with AuthenticationChallengeOptions.
```

```txt
Complete behavior:
1. Load stored WebAuthn challenge/options by challengeId.
2. Reject missing/expired/completed/exhausted challenge.
3. Deserialize credentialJson safely.
4. Validate assertion with WebAuthn/FIDO2 SDK.
5. Verify credential belongs to an existing user.
6. Update signature counter or credential state if repository has credential storage.
7. Mark challenge completed and persist it.
8. Return canonical user ID.
9. On validation failure, register failed attempt and persist it.
```

```txt
Do not use InMemory-only WebAuthn challenge persistence for production.
If no durable credential store exists yet, explicitly return NotImplemented/ProviderUnavailable and keep endpoint from pretending success.
However, the Phase 1 target requires real passkey behavior, so prefer porting the legacy implementation.
```

## Failure mapping commands

```txt
Map these passkey failures distinctly:
- InvalidInput
- ChallengeNotFound
- ChallengeExpired
- ChallengeAlreadyUsed
- ChallengeAttemptsExhausted
- UnknownCredential
- InvalidCredential
- ProviderFailure
```

## Acceptance checks

```bash
rg "Guid.NewGuid\(\)\.ToString\(\"N\"\)" src/Modules/Authentication/Infrastructure/WebAuthn src/Modules/Authentication/Application/Handlers
rg "Task.FromResult<string\?>\(null\)" src/Modules/Authentication/Infrastructure/WebAuthn
rg "OptionsJson|Passkey|WebAuthn" src/Modules/Authentication/Contracts src/Modules/Authentication/Application src/Modules/Authentication/Infrastructure
```

Expected:

```txt
- Passkey begin returns browser-usable WebAuthn options.
- Passkey complete validates credential JSON through real WebAuthn/FIDO2 logic.
- Challenge state prevents replay/reuse.
- Successful completion returns canonical user ID and signs in.
```

---

# Step 5 — Implement real Duende logout context

## Existing-code migration guidance for Step 5

```txt
Use the legacy Logout.razor.cs behavior as the source.
It uses InteractionService.GetLogoutContextAsync(logoutId), reads ShowSignoutPrompt, PostLogoutRedirectUri, and SignOutIFrameUrl, then signs out through SignInManager.
```

```txt
New DuendeLogoutContextService behavior:
- inject IIdentityServerInteractionService or the repo's existing Duende interaction abstraction;
- call GetLogoutContextAsync(logoutId);
- map null context to a safe LogoutContext result;
- map ClientName if available;
- map PostLogoutRedirectUri;
- map SignOutIFrameUrl if Contracts supports it, or add it to LogoutContextDto if clients need front-channel logout;
- map ShowLogoutPrompt from ShowSignoutPrompt.
```

```txt
New LogoutHandler behavior:
- query logout context first when logoutId is supplied;
- sign out current user through IAuthenticationSignInService;
- return redirect/sign-out iframe info from the real Duende context;
- do not return command.ReturnUrl blindly unless it passes ReturnUrlPolicy and no Duende post-logout redirect is available.
```

```txt
Do not keep the placeholder behavior that returns new LogoutContext(logoutId, null, null, true).
That loses Duende client logout metadata and cannot support federated/front-channel logout correctly.
```


## Goal

Replace synthetic logout context behavior with Duende-backed logout context resolution.

## Directional command

```txt
Replace DuendeLogoutContextService placeholder with a real Infrastructure adapter backed by Duende IdentityServer interaction/logout services.
Do not return synthetic logout context as final behavior.
Preserve client name, post-logout redirect URI, logout prompt semantics, and safe return URL handling.
```

## Files to inspect first

Run:

```bash
rg "GetLogoutContextAsync|IIdentityServerInteractionService|LogoutId|PostLogoutRedirectUri|ShowSignoutPrompt|Duende" src/Interfaces src/Hosts src/Modules src/Libraries
rg "Duende.IdentityServer" Directory.Packages.props src/**/*.csproj
```

Reuse existing legacy Accounts logout behavior if present.

## Files to edit

```txt
src/Modules/Authentication/Application/Abstractions/ILogoutContextService.cs
src/Modules/Authentication/Application/Handlers/GetLogoutContextHandler.cs
src/Modules/Authentication/Application/Handlers/LogoutHandler.cs
src/Modules/Authentication/Infrastructure/Logout/DuendeLogoutContextService.cs
src/Modules/Authentication/Infrastructure/Identity/AspNetIdentitySignInService.cs
src/Modules/Authentication/Infrastructure/DependencyInjection/AuthenticationInfrastructureServiceCollectionExtensions.cs
src/Modules/Authentication/Api/Endpoints/LogoutEndpoints.cs
src/Modules/Authentication/Contracts/Responses/LogoutContextDto.cs
src/Modules/Authentication/Contracts/Responses/LogoutResultDto.cs
```

## Application commands

```txt
Keep ILogoutContextService in Application as an abstraction.
It should return a simple Application model, not Duende types.
Required fields:
- LogoutId
- ClientName
- PostLogoutRedirectUri
- ShowLogoutPrompt
- maybe SignOutIFrameUrl if legacy behavior needs it
```

```txt
Update GetLogoutContextHandler:
1. Accept logoutId.
2. Call ILogoutContextService.GetAsync.
3. Return success with context or a structured failure if logoutId is invalid and repo conventions require failure.
4. Do not call Duende directly.
```

```txt
Update LogoutHandler:
1. Sign out through IAuthenticationSignInService.
2. Resolve post-logout redirect safely.
3. Prefer Duende postLogoutRedirectUri when logout context permits it.
4. Fall back to normalized local returnUrl or `/`.
5. Return LogoutResult with signedOut + redirect URL.
```

## Infrastructure commands

```txt
Implement DuendeLogoutContextService:
1. Inject IIdentityServerInteractionService or the existing repo Duende abstraction.
2. If logoutId is null/empty, return a context that tells UI whether prompt is needed based on existing behavior.
3. If logoutId is present, call GetLogoutContextAsync(logoutId).
4. Map Duende logout context to Application LogoutContext.
5. Preserve:
   - ClientName
   - PostLogoutRedirectUri
   - ShowSignoutPrompt / ShowLogoutPrompt
6. Do not expose Duende types outside Infrastructure.
```

```txt
Ensure Authentication.Infrastructure.csproj has the correct Duende package/project references only if needed.
Do not add Duende references to Application, Domain, Api, or Contracts.
```

## Api commands

```txt
Update LogoutEndpoints:
- GET /logout/context must return LogoutContextDto with explicit response metadata.
- POST /logout must return LogoutResultDto with explicit response metadata.
- Both must use Application handlers only.
- Do not directly call signInManager, HttpContext.SignOutAsync, or Duende services from Api.
```

## Acceptance checks

```bash
rg "new\(logoutId, null, null, true\)" src/Modules/Authentication
rg "IIdentityServerInteractionService|GetLogoutContextAsync" src/Modules/Authentication
rg "Duende" src/Modules/Authentication/Application src/Modules/Authentication/Domain src/Modules/Authentication/Api src/Modules/Authentication/Contracts
```

Expected:

```txt
- Duende appears only in Infrastructure or allowed composition code.
- Logout context maps real Duende context values.
- Logout result uses safe redirect resolution.
```

---

# Step 6 — Strengthen password login result semantics

## Existing-code migration guidance for Step 6

```txt
Use PasswordLoginSubmit.cshtml.cs as the legacy result-semantics source.
It distinguishes:
- invalid request/model state;
- unknown user / invalid credentials;
- success;
- lockout;
- not allowed/access denied;
- status-cookie UI messages.
```

```txt
New module translation:
- UI status cookie behavior must become typed AuthenticationFailure/ProblemDetails.
- Lockout must become a stable failure code and HTTP mapping, not just false.
- NotAllowed/AccessDenied must become a stable failure code and HTTP mapping.
- Invalid credentials must stay generic enough to avoid user enumeration.
- Success must return canonical user ID and resolved redirect URL.
```

```txt
The current AspNetIdentitySignInService returning bool from PasswordSignInAsync is too narrow.
Replace bool with a result object that preserves Identity SignInResult semantics without leaking Identity types into Application.
Suggested Application result:
- PasswordSignInSucceeded(user descriptor)
- InvalidCredentials
- LockedOut
- NotAllowed
- RequiresTwoFactor if needed later
- ProviderFailure
```

```txt
Legacy PasswordLoginSubmit uses PasswordSignInAsync(user, Input.Password, true, lockoutOnFailure: false).
Current PR uses rememberMe from request and lockoutOnFailure: true.
This may be an intentional change, but it must be explicit.
Pick the rule and document it in the handler/test:
- preserve legacy lockoutOnFailure: false, or
- intentionally enable lockoutOnFailure: true for security.
```


## Goal

Make password login return accurate, canonical, structured results instead of a boolean success/failure.

Current sign-in adapter returns bool; the handler returns `command.UserNameOrEmail` as the user identifier and collapses lockout/not-allowed into invalid credentials.

## Directional command

```txt
Replace bool password sign-in semantics with a rich sign-in result.
The Application layer must distinguish success, invalid credentials, lockout, not allowed, and other sign-in states without referencing ASP.NET Identity.
Successful LoginResult must use the canonical user ID, not the login string supplied by the user.
Profile-completion redirect decisions must be made in Application using user descriptor data.
```

## Files to edit

```txt
src/Modules/Authentication/Application/Abstractions/IAuthenticationSignInService.cs
src/Modules/Authentication/Application/Abstractions/IAuthenticationUserDirectory.cs
src/Modules/Authentication/Application/Handlers/LoginWithPasswordHandler.cs
src/Modules/Authentication/Application/Results/LoginResult.cs
src/Modules/Authentication/Application/Results/AuthenticationFailure.cs
src/Modules/Authentication/Domain/Security/AuthenticationFailureReason.cs
src/Modules/Authentication/Infrastructure/Identity/AspNetIdentitySignInService.cs
src/Modules/Authentication/Infrastructure/Identity/AspNetIdentityAuthenticationUserDirectory.cs
src/Modules/Authentication/Api/Mapping/AuthenticationEndpointMapper.cs
src/Modules/Authentication/Api/Mapping/AuthenticationProblemHttpMapper.cs
```

## Application result commands

```txt
Create an Application-level sign-in result model.
Suggested type:

public sealed record PasswordSignInResult(
    PasswordSignInStatus Status,
    string? UserId,
    bool RequiresProfileCompletion);

public enum PasswordSignInStatus
{
    Succeeded,
    InvalidCredentials,
    LockedOut,
    NotAllowed,
    RequiresTwoFactor,
    UserNotFound,
    ProviderFailure
}

Do not expose Microsoft.AspNetCore.Identity.SignInResult outside Infrastructure.
```

```txt
Update IAuthenticationSignInService:
- Task<PasswordSignInResult> PasswordSignInAsync(string login, string password, bool rememberMe, CancellationToken ct)
- Task SignInAsync(string userId, bool rememberMe, CancellationToken ct)
- Task SignOutAsync(CancellationToken ct)

If you prefer not to include RequiresProfileCompletion in PasswordSignInResult, resolve the user descriptor separately in LoginWithPasswordHandler after success.
```

## Handler commands

```txt
Rewrite LoginWithPasswordHandler:

1. Validate UserNameOrEmail and Password.
2. Call signInService.PasswordSignInAsync.
3. Switch on PasswordSignInStatus.
4. On Succeeded:
   - ensure UserId is not null;
   - get user descriptor if needed;
   - compute requiresProfileCompletion;
   - resolve redirect using IAuthenticationRedirectResolver.Resolve(returnUrl, requiresProfileCompletion);
   - return LoginResult(canonicalUserId, redirectUrl, requiresProfileCompletion).
5. On LockedOut: return AuthenticationFailureReason.LockedOut.
6. On NotAllowed: return AuthenticationFailureReason.NotAllowed.
7. On RequiresTwoFactor: return AuthenticationFailureReason.AdditionalStepRequired or equivalent.
8. On InvalidCredentials/UserNotFound: return privacy-safe InvalidCredentials unless product rules require distinction.
9. On ProviderFailure: return ProviderFailure.
```

## Infrastructure commands

```txt
Update AspNetIdentitySignInService.PasswordSignInAsync:

1. Validate input.
2. Find user by username or email.
3. If user missing, return UserNotFound or InvalidCredentials according to product policy.
4. Call SignInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true).
5. Map ASP.NET Identity result:
   - result.Succeeded -> Succeeded with user.Id
   - result.IsLockedOut -> LockedOut with user.Id if available
   - result.IsNotAllowed -> NotAllowed with user.Id if available
   - result.RequiresTwoFactor -> RequiresTwoFactor with user.Id if available
   - otherwise -> InvalidCredentials
6. Do not throw for normal credential failures.
7. Throw or return ProviderFailure only for unexpected infrastructure failures.
```

```txt
Update AspNetIdentityAuthenticationUserDirectory.ToDescriptor so RequiresProfileCompletion uses the exact legacy rule.
If legacy Accounts had a profile-completion redirect rule, port it into either:
- Application policy, if pure;
- Infrastructure user descriptor mapping, if based on Identity user fields.
```

## Redirect commands

```txt
Update IAuthenticationRedirectResolver usage:
- Application decides requiresProfileCompletion.
- Infrastructure resolver may use cookie/local URL mechanics.
- Redirect fallback must be safe local URL.

Do not decide profile-completion redirect in Api.
Do not return external returnUrl directly.
```

## Failure mapping commands

```txt
Add AuthenticationFailureReason values as needed:
- InvalidInput
- InvalidCredentials
- LockedOut
- NotAllowed
- AdditionalStepRequired
- Unauthorized
- ProviderFailure

Map these to HTTP status consistently in AuthenticationProblemHttpMapper:
- InvalidInput -> 400
- InvalidCredentials -> 401 or 400 based on repo convention; prefer 401 for auth failure if no enumeration risk issue
- LockedOut -> 423 or 403 based on repo convention
- NotAllowed -> 403
- AdditionalStepRequired -> 409 or 403 based on flow design
- ProviderFailure -> 503 or 500 based on cause
```

## Acceptance checks

```bash
rg "Task<bool> PasswordSignInAsync|bool signedIn" src/Modules/Authentication
rg "command.UserNameOrEmail" src/Modules/Authentication/Application/Handlers/LoginWithPasswordHandler.cs
rg "Microsoft.AspNetCore.Identity.SignInResult" src/Modules/Authentication/Application src/Modules/Authentication/Domain
```

Expected:

```txt
- Password login returns canonical user ID.
- Lockout/not-allowed/two-factor are not collapsed into invalid credentials.
- Application has no Identity dependency.
- Infrastructure maps Identity SignInResult to Application result.
```

---

# Step 7 — Complete API contract metadata

## Existing-code migration guidance for Step 7

```txt
Use legacy Razor/PageModel redirect/status behavior only to infer semantic outcomes.
Do not reproduce UI redirects as API behavior.
Every old redirect destination should become either:
- a redirectUrl field in a success DTO, or
- a stable ProblemDetails error code.
```

```txt
Translate old endpoint/UI outcomes:
- /Account/SuccessfulLogin -> LoginResult.RedirectUrl resolved from ReturnUrlStore/ReturnUrlPolicy.
- /Account/Login/Phone with status cookie -> ProblemDetails InvalidVerificationCode or InvalidInput.
- /Account/Login/Email with status cookie -> ProblemDetails InvalidVerificationCode or InvalidInput.
- /Account/Login/Password with status cookie -> ProblemDetails InvalidCredentials or InvalidInput.
- /Account/Lockout -> ProblemDetails LockedOut, likely 423 Locked or 403 depending repo convention.
- AccessDenied / IsNotAllowed -> ProblemDetails NotAllowed/AccessDenied, usually 403.
```

```txt
OpenAPI contracts must expose the new module endpoints, not legacy paths.
The passkey contract must document browser WebAuthn JSON shapes well enough that the React Phase 2 client can implement the same flow currently hidden inside WebAuthnLoginForm.
```


## Goal

Make the Authentication API contract generation-ready and stable for OpenAPI/client generation.

Endpoints currently have route mappings, names, operation IDs, and summaries, but they lack full response metadata, ProblemDetails metadata, visible rate limiting, antiforgery conventions, and some response shape correctness.

## Directional command

```txt
Complete Authentication.Api endpoint metadata so generated OpenAPI is stable and useful.
Every endpoint must have stable route, name, operationId, tags, success response type metadata, ProblemDetails response metadata, anonymous/protected metadata, rate-limit metadata where required, and antiforgery metadata/convention for state-changing cookie-auth endpoints.
Keep Api thin: do not move auth logic into endpoints.
```

## Files to edit

```txt
src/Modules/Authentication/Api/DependencyInjection/AuthenticationApiEndpointRouteBuilderExtensions.cs
src/Modules/Authentication/Api/Endpoints/AntiforgeryEndpoints.cs
src/Modules/Authentication/Api/Endpoints/CurrentSessionEndpoints.cs
src/Modules/Authentication/Api/Endpoints/PasswordLoginEndpoints.cs
src/Modules/Authentication/Api/Endpoints/PhoneLoginEndpoints.cs
src/Modules/Authentication/Api/Endpoints/EmailLoginEndpoints.cs
src/Modules/Authentication/Api/Endpoints/PasskeyLoginEndpoints.cs
src/Modules/Authentication/Api/Endpoints/LogoutEndpoints.cs
src/Modules/Authentication/Api/Mapping/AuthenticationEndpointMapper.cs
src/Modules/Authentication/Api/Mapping/AuthenticationProblemHttpMapper.cs
src/Modules/Authentication/Contracts/Errors/AuthenticationErrorCode.cs
src/Modules/Authentication/Contracts/Errors/AuthenticationProblemDto.cs
src/Modules/Authentication/Contracts/Responses/AuthenticationSessionDto.cs
src/Modules/Authentication/Contracts/Responses/CurrentAuthenticationUserDto.cs
src/Modules/Authentication/Contracts/Responses/AuthenticationChallengeDto.cs
src/Modules/Authentication/Contracts/Responses/AuthenticationRedirectResultDto.cs
src/Modules/Authentication/Contracts/Responses/LogoutContextDto.cs
src/Modules/Authentication/Contracts/Responses/LogoutResultDto.cs
src/Modules/Authentication/Module/DependencyInjection/AuthenticationModuleServiceCollectionExtensions.cs
```

## Endpoint metadata commands

```txt
For every endpoint, add explicit Produces metadata.
Use the exact repository conventions if helper extensions exist.
If using standard Minimal API extension methods, add:

- .Produces<TSuccess>(StatusCodes.Status200OK)
- .ProducesProblem(StatusCodes.Status400BadRequest)
- .ProducesProblem(StatusCodes.Status401Unauthorized)
- .ProducesProblem(StatusCodes.Status403Forbidden) when applicable
- .ProducesProblem(StatusCodes.Status423Locked) or repo equivalent when lockout is represented
- .ProducesProblem(StatusCodes.Status429TooManyRequests) for rate-limited endpoints
- .ProducesProblem(StatusCodes.Status500InternalServerError) only if repo convention includes it
- .WithOpenApi(... operationId + summary + description)
```

```txt
Keep operation IDs stable and lower camel case.
Suggested operation IDs:
- getAuthenticationSession
- getAuthenticationAntiforgery
- loginWithPassword
- beginPhoneLogin
- completePhoneLogin
- beginEmailLogin
- completeEmailLogin
- beginPasskeyLogin
- completePasskeyLogin
- getAuthenticationLogoutContext
- logoutAuthenticationSession
- getCurrentAuthenticationUser
```

```txt
Keep endpoint names stable.
Suggested names:
- Authentication_Session_Get
- Authentication_Antiforgery_Get
- Authentication_Login_Password
- Authentication_Login_Phone_Begin
- Authentication_Login_Phone_Complete
- Authentication_Login_Email_Begin
- Authentication_Login_Email_Complete
- Authentication_Login_Passkey_Begin
- Authentication_Login_Passkey_Complete
- Authentication_Logout_Context
- Authentication_Logout_Post
- Authentication_Me_Get
```

## Route group commands

```txt
Keep anonymous endpoints in the anonymous module group:
- GET /antiforgery
- POST /login/password
- POST /login/phone/begin
- POST /login/phone/complete
- POST /login/email/begin
- POST /login/email/complete
- POST /login/passkey/begin
- POST /login/passkey/complete

Keep protected endpoints in the protected module group:
- GET /session
- GET /logout/context
- POST /logout
- GET /me

If complete endpoints are intended to be anonymous because they complete login, do not RequireAuthorization on them.
```

## Rate limit commands

```txt
Add rate-limit metadata to all login/challenge endpoints:
- password login
- phone begin
- phone complete
- email begin
- email complete
- passkey begin
- passkey complete

Use existing repository rate-limit policies if present.
Search first:

rg "RequireRateLimiting|AddRateLimiter|RateLimit|rate limit|RateLimiter" src

If policies exist, use their names.
If no policies exist, add named constants in Authentication.Api or Module and register policies in AuthenticationModuleServiceCollectionExtensions.
Prefer policy names like:
- authentication-password-login
- authentication-otp-begin
- authentication-otp-complete
- authentication-passkey

Do not implement custom throttling inside endpoint bodies.
```

## Antiforgery commands

```txt
Ensure antiforgery behavior is explicit and testable.

GET /antiforgery:
- anonymous
- must call IAntiforgery.GetAndStoreTokens
- must return request token and header name X-XSRF-TOKEN
- must not mutate identity

State-changing cookie-auth endpoints:
- either require antiforgery validation through repo/global convention;
- or add explicit antiforgery metadata/filter if Minimal API conventions require it.

Search first:
rg "RequireAntiforgery|ValidateAntiForgery|IAntiforgery|X-XSRF-TOKEN|Antiforgery" src

Do not add custom manual token validation inside every endpoint unless the repo has no better convention.
Prefer endpoint conventions/filters.
```

## `/me` response shape commands

```txt
Fix GET /me response shape.
It must return CurrentAuthenticationUserDto directly, not AuthenticationSessionDto.

Expected behavior:
- authenticated user exists -> 200 CurrentAuthenticationUserDto
- authenticated principal but user cannot be resolved -> 404 or 401 according to repo convention
- unauthenticated -> 401 through protected group

GET /session may continue returning AuthenticationSessionDto.
Do not use the same private method for /session and /me unless it can return different DTO shapes correctly.
```

## ProblemDetails commands

```txt
Make AuthenticationProblemHttpMapper use AuthenticationErrorCode and AuthenticationProblemDto consistently.

Required mapping:
- every AuthenticationFailureReason maps to a stable AuthenticationErrorCode;
- every error maps to a stable HTTP status;
- ProblemDetails.Extensions["code"] must use the stable contract code, not arbitrary enum ToString if enum is not the DTO code;
- include traceId only if repo convention does so;
- do not leak provider exception details.
```

```txt
Update AuthenticationErrorCode with stable frontend-safe values, for example:
- invalid_input
- invalid_credentials
- locked_out
- not_allowed
- unauthorized
- challenge_not_found
- challenge_expired
- challenge_already_used
- challenge_attempts_exhausted
- invalid_verification_code
- provider_failure
- passkey_invalid_credential
- logout_context_unavailable

Use repo naming conventions if different.
```

## OpenAPI commands

```txt
Ensure DTO schemas are stable:
- request/response records must have clear required/nullability behavior;
- avoid anonymous response types except for antiforgery if an explicit DTO is not required;
- prefer explicit AntiforgeryTokenDto instead of anonymous object if OpenAPI schema stability matters.
```

```txt
Replace anonymous antiforgery response with a contract DTO if needed:
Contracts/Responses/AntiforgeryTokenDto.cs
Fields:
- RequestToken
- HeaderName
```

```txt
Run or prepare this verification after implementation:

dotnet build src/Modules/Authentication/Api/Authentication.Api.csproj --no-restore
dotnet build src/Modules/Authentication/Module/Authentication.Module.csproj --no-restore

If the repo has OpenAPI generation validation commands, run the authentication document validation.
```

## Acceptance checks

```bash
rg "Produces<|ProducesProblem|ProducesValidationProblem" src/Modules/Authentication/Api/Endpoints
rg "RequireRateLimiting" src/Modules/Authentication/Api src/Modules/Authentication/Module
rg "X-XSRF-TOKEN" src/Modules/Authentication
rg "new \{ requestToken" src/Modules/Authentication
rg "AuthenticationSessionDto" src/Modules/Authentication/Api/Endpoints/CurrentSessionEndpoints.cs
```

Expected:

```txt
- Every endpoint has explicit success and error response metadata.
- Login/challenge endpoints have rate-limit metadata.
- Antiforgery contract is explicit and stable.
- /me returns CurrentAuthenticationUserDto, not AuthenticationSessionDto.
- ProblemDetails exposes stable AuthenticationErrorCode values.
```


---

# Legacy migration parity checklist

Run this before declaring the migration-assisted implementation complete.

```txt
[ ] Return URL cookie behavior is implemented in Infrastructure and local URL validation is pure.
[ ] Return URL consume/clear semantics are represented where needed for successful login redirect.
[ ] Phone begin preserves or explicitly rejects legacy auto-registration-by-phone behavior.
[ ] Email begin preserves or explicitly rejects legacy auto-registration-by-email behavior.
[ ] Phone token generation uses TokenOptions.DefaultPhoneProvider through Infrastructure.
[ ] Email token generation uses TokenOptions.DefaultEmailProvider through Infrastructure.
[ ] Phone delivery queues SmsCodeCommunicationCommand or the repo's newer SMS command boundary.
[ ] Email delivery queues EmailCommunicationCommand or the repo's newer email command boundary.
[ ] Email body template from legacy EmailAuthenticationService is reused or intentionally replaced.
[ ] SMS duplicate-send shield preserves cooldown, cleanup, capacity, and release-on-send-failure behavior.
[ ] Submitted OTP codes are normalized for Persian/Arabic numerals before verification.
[ ] Phone completion confirms PhoneNumber when verification succeeds.
[ ] Email completion confirms Email when verification succeeds.
[ ] OTP completion signs in using the equivalent of SignInManager.SignInAsync(user, true, "mfa").
[ ] Password login exposes lockout and not-allowed outcomes distinctly.
[ ] Password lockoutOnFailure behavior is intentionally chosen and documented.
[ ] Passkey begin returns real PublicKeyCredentialRequestOptions-compatible JSON.
[ ] Passkey complete validates a real assertion and returns a canonical user ID.
[ ] Duende logout context maps PostLogoutRedirectUri and SignOutIFrameUrl from the real interaction service.
[ ] Legacy UI status-cookie behavior is not copied into Api/Application; it is mapped to ProblemDetails.
[ ] No new Authentication.Application dependency on Interfaces.Accounts, Interfaces.*, Razor, React, Identity, EF, Duende, HttpContext, or Communications provider types.
```

---

# Step 1–7 completion checklist

Use this checklist before claiming these steps are done.

```txt
[ ] Challenge store can save and replace/upsert lifecycle state.
[ ] Completion loads challenge state from store.
[ ] Expired challenges are rejected.
[ ] Completed challenges cannot be reused.
[ ] Failed attempts are persisted.
[ ] Exhausted challenges are rejected.
[ ] Phone begin resolves user, generates token/code, stores challenge, sends SMS.
[ ] Email begin resolves user, generates token/code, stores challenge, sends email.
[ ] Phone/email complete verifies code using stored user/purpose and signs in canonical user.
[ ] SMS sender is not a no-op.
[ ] Email sender is not a no-op.
[ ] SMS and email are both correctly registered and reachable.
[ ] Passkey begin returns real browser-usable WebAuthn options.
[ ] Passkey complete validates real credential JSON and returns canonical user ID.
[ ] Duende logout context uses real Duende interaction/logout context service.
[ ] Password login distinguishes success, invalid credentials, lockout, not allowed, and additional step states.
[ ] Password login returns canonical user ID, not login string.
[ ] Profile-completion redirect decision happens in Application.
[ ] API endpoints have stable names, tags, operationIds, summaries, success metadata, and ProblemDetails metadata.
[ ] Login/challenge endpoints have rate-limit metadata.
[ ] Cookie-auth state-changing endpoints have explicit antiforgery convention/metadata.
[ ] /me returns CurrentAuthenticationUserDto directly.
[ ] Application has no HttpContext, EF, Identity, Duende, SMS/email SDK, WebAuthn SDK, UI, Razor, React.
[ ] Domain remains pure.
[ ] Concrete integrations remain in Infrastructure.
[ ] Authentication.Module project builds.
```
