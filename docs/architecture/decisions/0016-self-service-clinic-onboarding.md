# ADR-0016: Self-Service Clinic Onboarding and the One-Time Handoff Link

Date: 2026-10-21
Status: Accepted
Scope: Parties onboarding, Authentication sign-in and handoff, Journey gates, the private-practice-owner Web app, and production ingress
Owner: Architecture
Supersedes:
Superseded by:
Related: issue #906; PRs #909, #910, #911, #912, #913, #914, #916, #921; issues #915, #917, #918

## Context

Until now a platform administrator created every clinic. Issue #906 lets a
person create their own clinic without support: sign up with a phone number,
create the clinic, land on it signed in, and set it up.

Several existing boundaries are involved:

- A clinic is resolved from its host name, and session cookies are host-only,
  so a session on one host means nothing on another.
- Users and Authentication `AGENTS.md` forbid assigning a role while the user
  has an active session, and a session needs a role.
- Opening the administrator's tenant setup to the public would allow privilege
  escalation and leave partial data on failure.
- Origin trust becomes dynamic: any new clinic host is a trusted origin.

## Decision

1. **Addresses.** Each clinic gets its own host, `<subdomain>.<base domain>`:
   `*.clinic.vitarish.com` in production and `<subdomain>.localhost` locally and
   in E2E. The base domain is `Vitarish:TenantBaseDomain`. Subdomains are 3–30
   characters, lowercase letters, digits and inner hyphens, and some names are
   reserved.
2. **A dedicated onboarding host.** `Vitarish:OnboardingUrl` names one host
   under the base domain. Startup binds it to a platform-owned onboarding tenant
   (`OnboardingTenant`), which holds no practice data. The login page links to
   it when the Web app's `VITARISH_ONBOARDING_URL` is set.
3. **Role-free sessions only there.** Sign-in admits a user without a role only
   on the onboarding host (`IRoleFreeSessionPolicy`). That session can use
   account endpoints and clinic creation; every tenant endpoint still refuses it.
4. **Journey gates.** The profile gate comes first, then a `TenantCreation` gate
   that sends a signed-in user on the onboarding host to `/setup`.
5. **Creation.** `POST /api/modules/parties/onboarding/tenants` with an
   `Idempotency-Key`. The creator comes from the session, never the request
   body, and needs a verified phone. Limits are 3 clinics per creator per day
   and 10 requests per IP per hour. The same creator and key always derive the
   same tenant and owner ids, so a replay returns the clinic already created. A
   taken address answers 409. The store writes the Parties rows, roles and owner
   all together or not at all. Outcomes are security-audited. Category comes
   from a fixed server-owned list; business details are optional; the logo and
   photos come after creation, because files are stored per clinic.
6. **Session end at creation.** Creating a clinic ends the creator's sessions
   (audited as *replaced by new session*) after every validation and the Parties
   write have succeeded, and only then assigns the owner role. This keeps the
   role-assignment rule without an exception. (Owner decision on #906: keep this
   design.)
7. **One-time handoff link.** Creation returns a token. It is stored as a hash,
   works once, for 60 seconds, only for its creator and only on the new clinic's
   host. The Web app carries it in the URL fragment (never sent to servers or in
   `Referer`) to `/handoff`, which redeems it through `/handoff/redeem` and
   starts a normal session on the new host. Every invalid, expired, replayed or
   foreign token gets the same refusal, and the creator then signs in normally.
8. **Feature flag.** `Vitarish:Features:TenantOnboarding` gates the endpoints.
   They answer 404 unless the flag is on, the base domain is set and the app
   offers a self-service profile.

## Consequences

- People can create and enter a clinic without support. The walkthrough
  (`src/Hosts/Web/tests/e2e/clinic-onboarding-walkthrough.spec.ts`) proves the
  flow and its refusals on the two-host E2E stack.
- Production needs a wildcard DNS record, a wildcard TLS certificate (DNS-01) and
  a wildcard ingress host for the clinic domain; see
  `docs/runbooks/clinic-onboarding.md`.
- The creator signs in again on the onboarding host if they come back, because
  creation ended that session.
- Open creation invites address squatting; the per-creator and per-IP limits and
  the reserved names reduce it but do not remove it.
- The handoff link is another way to sign in. It is single-use, short-lived and
  host-bound, and its refusals are uniform.
- Rollback: set the flag to false and clear the onboarding URL. Clinics already
  created keep working. The onboarding migrations only add the handoff-ticket
  and business-profile tables and a nullable logo-prompt column, so older code
  ignores them.

## Alternatives Considered

### Option 1: Dedicated onboarding host with a handoff link (chosen)

Keeps cookies host-only and clinic resolution unchanged, and confines role-free
sessions to a tenant with no data.

### Option 2: Open the administrator's `/tenant-setup` to the public

Rejected: it allows privilege escalation and leaves partial data on failure.

### Option 3: A domain-wide session cookie

Rejected: it would share sessions between clinics.

### Option 4: A shared host with a clinic picker

Rejected for now: it needs a rewrite of how clinics are resolved.

### Option 5: A shared `TenantUser` role on the onboarding tenant

Rejected after review: it would give every sign-up a role that tenant
endpoints accept.

### Option 6: Keep the creator's onboarding session through creation

Rejected: it needs an exception to the rule that roles are not assigned during an
active session.

## Validation / Enforcement

- Parties, Authentication and Journey tests cover limits, replay, address
  conflicts, the role-free policy and every handoff refusal, including a Postgres
  concurrency test for redemption.
- The two-host E2E walkthrough runs in CI with the rest of the suite.
- `docs/security/authorization-matrix.md` lists each new operation.

## Notes

- Open: the business listing data model (#917) should be settled before the
  listing is shown publicly.
- Known gap: entity audit entries for the new clinic's rows are dropped because
  they belong to a different tenant than the request (#918).
- Clinic settings editing and photos: #915.
