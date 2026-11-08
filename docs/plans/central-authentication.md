# Central authentication implementation

Status: implementation authorized; release incomplete.

## Resumed failure correction checklist

Root, Hosts, Web, browser-test and docs instructions re-read on 2026-11-08.
The worktree starts clean at 56471fa8c; unrelated landing work remains stashed.
Claude Opus 5.5 medium consultation completed read-only after the earlier high
reviews. Its remaining hypotheses require source/trace confirmation.

- Preserve single owners, dependency direction, read-only queries, explicit
  failures, retries/idempotency, parameterization and contract compatibility.
- Preserve secrets, host/subject/tenant authorization, antiforgery, WebAuthn,
  TLS and rate limits; do not relax assertions to hide unexpected behavior.
- No generated or migration edits in this correction; production RP ID remains
  unchanged. Test-only RP configuration follows the actual central host.
- Reuse typed routes, operation/configuration owners, fixture values and
  defined localization resources; audit all added quoted values and complete
  tracked/untracked diff. Keep responsibility-based names.
- Preserve Web layout/color/theme/fonts, localized icons/tooltips/dates,
  security-state ownership and shared API transport where applicable.
- Add focused guard coverage, retain matrix markers and strict denials,
  validate collection/affected checks, then collective real-host E2E with
  inspected fresh proof and changed screens before release.
- Deploy exact validated revision, verify runtime evidence, restore unrelated
  stash and clean owned artifacts. All unfinished gates remain unproven.

Trace confirms administrator /handoff is intercepted by its anonymous access
guard before redemption: repeated 401 session, /login, continuation complete,
/handoff, with no redeem request. Permit only the existing handoff page through
the presentation guard; the server retains ticket and administrator authority.
The guard also preserves the requested path/query through central sign-in.
Three focused guard tests pass, including anonymous protected content and
signed-in non-administrator refusal. Owner/admin typechecks, full browser-spec
typecheck, 87-test collection, localization/layout and app/shared/API boundary
checks pass. The browser-spec typecheck additionally caught a missing Routes
import in clinic onboarding; restored the source-owned import. Named-value
review covers every correction and the new untracked test: routes/query keys
are typed owners, resource labels exist, credential-operation inventory derives
from endpoint filters, and synthetic messages/framework selectors are local.
No backend, public contract, generated schema or production configuration
changed in this correction. The new collective E2E is running; its runtime
passkey and central session evidence remain unproven until results are read.

Owner correction: do not stop at a completed slice. Continue until the update is
deployed; the owner reports no active server sessions. E2E remains deferred until
all implementation and faster checks are ready, with necessary fixes/reruns
already authorized.

Auth owns interactive sign-in, recovery and personal security across all Web
apps. A central browser session links host-only app sessions. Ordinary sign-out
ends that family, preserving other permitted browsers. Legacy sessions remain
standalone until next sign-in. Pending verification survives focus changes and
reload without storing passwords or entered codes.

## Owners and ordered slices

1. Shared auth presentation: preserve the mounted form during refetch.
2. Authentication: durable session links, handoff binding and family lifecycle.
3. Authentication/Parties: validated app continuations and central entry.
4. Authentication/Users/Web: pending challenge recovery and personal security.
5. Hosts: configuration, generated contracts, collective validation and release.

Reuse existing owners and host composition. Do not introduce another identity
framework or parent-domain cookie. Parties owns clinic membership; each app
retains authorization. Keep onboarding role assignment's session-end boundary.
Use additive generated migrations and regenerate clients from authoritative
OpenAPI. Rolling back a family-aware backend requires revoking linked sessions
first; keep additive schema. Never infer relationships between legacy sessions.

## Instruction checklist and evidence

- [ ] Re-read root and scoped instructions before edits and final audit; review
  every tracked/untracked path and preserve unrelated landing work.
- [ ] One owner per state/rule/value; inward dependencies, cohesive boundaries,
  no speculative framework, no business rules in composition or DTOs.
- [ ] Queries remain read-only; validate input and external data; define
  cancellation, failures, duplicates, concurrency and compensation.
- [ ] Preserve authentication, tenant/subject authorization, antiforgery,
  origin/TLS trust, redaction, parameterized data access and audit dispositions.
- [ ] Preserve password/denial policy, durable history/notices, idle limits,
  browser concurrency and explicit acknowledgement. Never claim MFA merely
  from available methods. Use trusted normalized IP only.
- [ ] Version contracts additively; generate migrations and clients, preserve
  existing data, document mixed versions and rollback; no manual generated edits.
- [ ] Review every added quoted value against its authoritative owner; keep
  responsibility-based durable names. No duplicated contract literals.
- [ ] Preserve Web accessibility, localization, shared transport, layout,
  color, icons, tooltips, theme/font ownership where changed.
- [ ] Meaningful owner and direct-boundary tests, deterministic clocks,
  migration/concurrency/dependency failure coverage; no weakened diagnostics.
- [ ] Endpoint metadata/architecture validators and relevant builds pass.
- [ ] One collective final two-host E2E run after fast checks; fix/rerun only
  when needed. Inspect fresh proof/screenshots, retain only useful evidence.
- [ ] Release exact integrated revision through production workflow; separate
  local, CI, migration, release, HTTPS and browser evidence.

Initial focus fix: 105 shared-auth tests and auth typecheck passed. E2E deferred
until implementation is ready. No release has been performed.

Risks: session concurrency, browser correlation, onboarding role transitions,
passkey origin compatibility and stale cookies after logout. Revision triggers:
an existing equivalent owner, incompatible deployed credentials or a failing
required gate. Excluded: unrelated landing edits, redesign, machine auth changes,
security-policy relaxation. Apply HeroUI skills when constructing changed UI.

Checkpoint resumed: latest 107 shared-auth tests, all affected app/Auth/Journey
typechecks, Main build and OpenAPI endpoint coverage (zero violations) passed.
Owner added a Claude code-review at high effort before E2E; review requested
read-only over tracked and untracked in-scope source. E2E/release remain pending.

## Acceptance and validation ownership

Implementation is authorized for every browser app and production deployment.
Authentication owns challenge/session/continuation state in Domain/Application;
its EF, cookie and Identity adapters implement those ports. Parties owns live
host binding and generic tenant admission; PM provides its own admission port.
Journey owns required personal gates and continuation ordering. Shared Auth
owns form recovery and app adapters own Next routing. Hosting owns the exact
origin, proxy, HTTPS and cookie mechanics. ADR-0021 records the contract migration.

Acceptance scenarios and proof:

- Anonymous background session refetch keeps the same email/code input node.
  Shared-auth interaction tests cover this and pending restoration without a
  second delivery or stale restore overwriting a newer challenge.
- Known/unknown/blocked/failed-delivery recovery has neutral resume output;
  password-OTP resume reveals no phone destination. Authentication owner tests
  cover protected cookie tampering, expiry, completion and privacy boundaries.
- A root and linked apps consume one concurrency slot, app activity renews the
  root, idle expiry cannot resurrect it, logout revokes only its family, and
  wrong-user/ended roots refuse attachment. Registry tests exercise these
  invariants; PostgreSQL tests include persistence/transaction failure paths.
- Continuations and tickets are single use under competing contexts, reject
  wrong browser/host/subject, and respect current access. Independent tab
  cookies are bounded to sixteen and removed on use. A consumed continuation
  is not reopened after an uncertain ticket write: begin again.
- PM's configured origin applies the app-owned administrator/member policy
  even for an existing operator tenant. Role-free patients admission grants
  a session, never clinical tenant or resource authority.
- API tests reject app-local credential entry including case/trailing-slash
  variants. Personal passkey/PAT management also requires Auth. Unbound legacy
  tickets are rejected in central deployments; unconfigured installations
  retain local fallback compatibility.
- Real-host E2E covers Auth-to-clinic and other-app callbacks, one browser root,
  family logout, email tab switching plus reload without another delivery,
  clinic creation returning through Auth, and replay/tenant refusals. Capture
  changed screens in that collective run and inspect fresh proof images.

Run focused dotnet tests for Authentication, Parties, Journey, Users, Main,
Landing.Application and PM Application/API; build Main and the E2E host through
normal composition. Run canonical npm API generation after Main's OpenAPI
build, affected workspace typechecks, shared-auth tests, localization/layout
and app/shared/API-boundary validators. Run openapi-endpoints and security-baseline.
Only after these and Claude high-effort review, run test:e2e:two-host from Web.
The owner's single collective E2E instruction takes precedence over separate
capture runs: the same suite captures the changed authentication surfaces.

There is no data deletion or destructive migration. Existing null-linked
sessions stay independent; no relationships are inferred. New Web requires
new endpoints and must ship with Main. Retain additive schema on rollback;
revoke linked roots before any backend rollback that cannot validate families.
No cross-machine trust or TLS relaxation is introduced. Configuration adds
registered app origins and Auth as an exact CSP connect destination; the demo
is not added to production topology. Generated migrations and clients come
only from their canonical generators.

## Review and current evidence

Claude Code reviewed tracked and untracked in-scope changes at high effort
(session 9d5de8c4-3f92-482d-8828-b7342fd30319), followed by a focused high-effort
review. Reports are retained locally in Web artifacts/central-auth-review.
Recovery privacy, PM admission, path-matching bypass, tab correlation, entry
rate limiting, legacy tickets, cookie cleanup and local fallback findings have
been addressed in source. Follow-up corrections add app-owned PM admission,
preserve existing host bindings, clear completed recovery references, and
bound correlation cookies. Custom-domain background renewal limits are stated
in ADR-0021. Final E2E, full diff/quoted-value audit, CI and release are pending.

Latest completed checks before final gate: 484 Authentication tests with zero
skips against disposable PostgreSQL, 223 Parties tests, 298 Users tests,
23 Landing tests, 31 PM Application tests, 9 PM API tests, 109 shared-auth tests;
all affected app/Auth/Journey typechecks; Main build; OpenAPI and security
baseline with zero violations; Web localization, layout and boundaries passed.
Additional focused checks are running for the final review corrections.


## Mainline integration correction

The first E2E attempt was stopped during Next builds, before browser tests,
after fetching main 99577ebac revealed eight overlapping merged changes.
Integrate platform home, app-aware workspaces and email second-factor/recent
sign-in behavior; Auth still owns the persistent browser family. No-intent
sign-in returns to platform home, with no automatic clinic selection. Correlated
app entry retains explicit intent. Unrelated landing work is preserved in Git
stash ad139a4620889a02bfb51f44b4d6c50bd67ded7a and must be reapplied before handoff.

The generated Authentication snapshot conflicted with main's already-shipped
AuthenticationSessionAndHandoffAuthenticationTimes migration. Preserve all
existing migrations unchanged. Regenerate the snapshot through native EF
scaffolding from the merged authoritative model, excluding only the conflicted
snapshot from the temporary design build. The temporary scaffold migration is
never applied or deployed and its two newly generated migration artifacts are
discarded immediately; retain only EF's regenerated model snapshot. Verify the
combined migration ledger/model on disposable PostgreSQL and normal rebuild.
No generated snapshot is hand-edited, restored or copied to create a baseline.

## Current integration checklist

- Preserve all incoming mainline behavior and unrelated owner changes; inspect tracked, staged and untracked paths.
- Keep authoritative rules/values at their owning boundary and inward dependencies; queries remain read-only, failures explicit and retries/idempotency unchanged.
- Preserve authentication, authorization, tenant, transport, antiforgery, privacy and audit protections; retain recent-authentication timestamps across handoffs.
- Generate clients/specs and EF output canonically; preserve applied migration history, validate additive combined model on disposable PostgreSQL.
- Audit every added quoted value, compatibility/configuration effects and plan-independent names.
- Follow Web localization, icons/tooltips, surface/color/theme/date rules and meaningful test assertions; retain no native confirmation dialogs.
- Run focused tests, merge-level build, architecture/Web gates; Claude high review before final collective real-host E2E and inspected fresh screenshots.
- Deploy exact reviewed release through repository workflow, verify runtime evidence, restore owner landing stash, clean owned temporary artifacts.
All final integration gates are pending until recorded with current results.

Integration validation: Main builds; Authentication 555 (zero skipped, disposable PostgreSQL), Parties 239, Journey 95, Landing 42, Users 307, Main 120 and PM Application 31 tests passed. Shared Auth 112 and shared Journey 37 passed; affected app typechecks and Web layout/localization/app/shared/API boundaries passed. OpenAPI and security baseline report zero violations; EF reports no pending model changes.

Claude's initial and follow-up high-effort reviews completed before E2E. A requested extra integration review stopped at the provider five-hour quota after two turns; it did not review the integration. Local semantic integration audit found and corrected duplicated return query propagation, recent-authentication handoff plumbing, platform-home SSO/test assumptions and obsolete workspace navigation.

Added-line quoted-value review covers tracked/staged and untracked source. Routes use AuthenticationRoutes/AuthenticationWebRoutes and app route aliases; operation/audit/claim values use their typed owners; generated output remains generator-owned. Frontend mirrors of browser query/path contracts are maintained at shared Auth and backend Authentication Domain, synchronized by the documented handoff contract and exact-value tests. Resource keys resolve shared localization; imports, CSS/framework attributes, format specifiers, URI delimiters, diagnostics and test-local synthetic values are local. New fixture usernames resolve the E2E seeder. Final E2E/release remain pending.

Final preparation corrections: solution CI and the separate E2E build exposed a fixture local-variable collision; corrected without changing fixture authority or assertions, and E2E builds successfully. A source audit then found the general Journey return policy rejecting Auth continuation queries. Journey now delegates those queries only on Auth to Authentication's destination policy; ordinary app return validation is unchanged. Profile completion never appends itself as its own return destination. Journey regression suite passes 98 tests, including strict general-policy refusal and profile continuation/self-return cases. These backend changes precede the runner's backend build; no browser scenario has run yet.

The profile gate also drops its own page as a post-completion return intent, so a no-intent new account continues home after saving. The final E2E backend build succeeded after this correction; its Journey Application assembly hash matches the freshly tested owner assembly (D85B97BECE9F394576F5C4E4C22161B8A68D999E1A5474447574A4894D8CC418). No frontend behavior changed during that preparation.

Preparation/CI corrections: the Knowledge fixture now uses its own member account and the tenant-role command; a platform administrator is explicitly refused Knowledge membership in E2E, while the legitimate member shares a browser with Patients. The runner verifies backend startup and Playwright collection before building Web apps. Collection resolves fixture source through the existing CommonJS-compatible path pattern and lists 87 scenarios in 26 files successfully. CI's full .NET job passed. Its shared Web job exposed a missing child-component export in the account-security unit mock: updated that isolation fixture without changing history/notice assertions. The two existing account-security tests and a new browser-session failure/duplicate-submit/success-refresh interaction test pass. No browser scenario has yet executed.
