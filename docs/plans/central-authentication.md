# Central authentication implementation

Status: implementation authorized; release incomplete.

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
origin, proxy, HTTPS and cookie mechanics. ADR-0020 records the contract migration.

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
in ADR-0020. Final E2E, full diff/quoted-value audit, CI and release are pending.

Latest completed checks before final gate: 484 Authentication tests with zero
skips against disposable PostgreSQL, 223 Parties tests, 298 Users tests,
23 Landing tests, 31 PM Application tests, 9 PM API tests, 109 shared-auth tests;
all affected app/Auth/Journey typechecks; Main build; OpenAPI and security
baseline with zero violations; Web localization, layout and boundaries passed.
Additional focused checks are running for the final review corrections.
