# ADR-0018: Shared Sign-In Host

Date: 2026-10-23
Status: Accepted
Scope: Parties sign-in host and clinic list, Journey gates, the private-practice-owner Web app, and production settings
Owner: Architecture
Supersedes:
Superseded by:
Related: ADR-0016; issue #900; PRs #939, #940

## Context

Each clinic has its own host (ADR-0016), and a person had to know their
clinic's address to sign in. Issue #900 asks for one sign-in address that takes
each person to their clinic, or lets them choose when they have several.

The same boundaries as ADR-0016 apply:

- A clinic is resolved from its host name, and session cookies are host-only.
- A session needs a tenant, and normally a role in it.
- Moving a signed-in person to another host needs the one-time handoff link.

ADR-0016 rejected "a shared host with a clinic picker" for now, because it
seemed to need a rewrite of how clinics are resolved. The onboarding host showed
it does not: a platform-owned tenant with no practice data can hold the session.

## Decision

1. **A sign-in host with its own tenant.** `Vitarish:SignInUrl`
   (`https://auth.vitarish.com` in production, `http://auth.localhost:3005` in
   E2E) names one host. Startup binds it to the platform-owned `SignInTenant`,
   which holds no practice data. `auth` is a reserved subdomain, so no clinic
   can claim it.
2. **Role-free sessions there too.** `PlatformHostRoleFreeSessionPolicy` admits
   a session without a role on the onboarding host or the sign-in host only. It
   has no access to any tenant's data.
3. **The person's clinics, from their roles.** The clinic list reads the user's
   tenant roles from Users and keeps live tenants with an active primary host.
   It does not use the `mv_UserTenant` view, which a new clinic does not
   refresh.
4. **Sign-in host endpoints.** `/api/modules/parties/sign-in` lists the clinics
   and issues a handoff link to one of them, or to the onboarding host. They
   answer 404 on every other host. The server chooses the target host from the
   user's own clinics or its configuration; the caller only names a tenant id,
   so there is no open redirect, and a clinic that is not theirs gets the same
   404 as one that does not exist.
5. **Journey.** On the sign-in host a signed-in person with a complete profile
   goes to `/workspaces/select`. With one clinic it continues by itself; with
   several the person picks one; with none, or on **Create a new clinic**, it
   continues to the onboarding host.
6. **Public clinic logos.** The list shows each clinic's logo, loaded from the
   clinic's own host at `/api/modules/parties/public/logo`, an anonymous,
   rate-limited `AnonymousBinaryDownload` endpoint. The image is requested
   without a referrer, because the tenant resolver prefers the `Referer` over
   the host and would otherwise read the sign-in host's tenant.

## Consequences

- One sign-in address serves every clinic. The two-host E2E spec
  `src/Hosts/Web/tests/e2e/sign-in-host.spec.ts` proves sign-in there, the
  list with a logo, the landing signed in on the chosen clinic, and two
  refusals.
- The handoff link now also moves people from the sign-in host. It keeps its
  ADR-0016 limits: single use, 60 seconds, bound to its user and target host,
  audited, and redemption still needs access on the target.
- A clinic's logo is public. That is what a logo is for, but it is now readable
  without signing in, and anonymous reads are rate limited, not audited.
- Production needs no new DNS, certificate or ingress site: `auth.vitarish.com`
  is under the existing `*.vitarish.com` site, which already sends traffic to
  the owner Web app and `/api/*` to Main. See `docs/runbooks/sign-in-host.md`.
- Rollback: the binding is stored, so clearing `VITARISH_PRODUCTION_SIGN_IN_URL`
  only stops startup from creating it again. Also set the sign-in host's
  indicator to `Disabled` (see the runbook). The host then resolves to no tenant,
  the sign-in endpoints answer 404, and people sign in on their clinic's own
  host as before. No clinic data or migration is involved.

## Alternatives Considered

### Option 1: A platform-owned sign-in tenant on its own host (chosen)

Reuses the onboarding host's model: host-only cookies, host-based clinic
resolution and the existing handoff link are unchanged.

### Option 2: Make the onboarding host also the sign-in host

Rejected: one host would mean both "create a clinic" and "open my clinic", and
creating a clinic ends the creator's sessions there.

### Option 3: A tenantless platform session

Rejected: every session path assumes a tenant, so it would need changes across
Authentication, auditing and tenant resolution.

### Option 4: Do nothing

People keep signing in on their clinic's own address, which they must remember.

## Validation / Enforcement

- Parties tests cover the clinic list, ownership-only handoff, the onboarding
  handoff, the sign-in host query, the public logo and the role-free policy.
  Journey tests cover the sign-in host gate.
- The two-host E2E spec above runs with the rest of the suite.
- `docs/security/authorization-matrix.md` lists each new operation, and the
  security baseline and OpenAPI checks validate the anonymous logo endpoint.

## Notes

- Clinic hosts' own sign-in pages do not link to the sign-in host yet.
