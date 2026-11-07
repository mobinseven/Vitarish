# ADR-0021: Central browser authentication

Date: 2026-11-07
Status: Accepted
Owner: Authentication; Parties owns host registration and tenant access
Supersedes: ADR-0018 decisions 7 and 8 and their rollback procedure; ADR-0020's session-ending handoff behavior

## Decision

Auth owns interactive browser sign-in, recovery, contact/password changes,
passkey management, access history and browser-session management. All browser
apps enter through Auth. Administrative account/policy management and clinical
or project authorization remain with their existing owners.

ADR-0020's platform home and app-aware workspace catalog remain authoritative.
Sign-in without app intent returns to platform home, without selecting a lone
workspace or starting onboarding automatically. Explicit app intent survives
the correlated continuation. Personal account management returns to Auth.

Authentication owns a durable central browser session. Each app receives a
host-only cookie referencing a linked session; no parent-domain cookie is used.
Only roots consume concurrent-browser slots and receive parallel-session
notices. App activity updates the family root. Explicit logout on any linked
app revokes that root and its descendants transactionally, preserving other
independently permitted roots. Re-entering an app reuses its active child.

Entry starts with an app-origin continuation, ten-minute lifetime, opaque token
and host-only HttpOnly browser-correlation cookie. Auth rechecks the registered
origin and subject's access, follows mandatory personal Journey gates, consumes
the continuation once and issues a sixty-second single-use handoff. Redemption
requires the target host, matching browser correlation and active central root.
Separate short-lived correlation cookies allow concurrent entry tabs.
Fragments carry handoff tickets and are removed before redemption. Failures do
not fall back to app-local credential collection; start a new entry.

Pending email/phone/password-OTP/recovery challenges remain server-owned. A
protected host-only HttpOnly cookie stores only the challenge reference and
safe return context. Reading it neither resends delivery nor acknowledges
history. Expired, consumed or exhausted challenges do not resume. Passwords and
entered codes are not persisted. Background session refresh keeps the mounted
login form until the initial authentication decision changes.

Creating a clinic still ends prior sessions before role assignment. Its
single-use receipt returns to Auth and requires the same prior protected cookie
before establishing a fresh root with the new roles. The clinic then enters
through the ordinary correlated continuation.

## Compatibility and operations

The generated CentralBrowserSessions and AuthenticationOnboardingContinuation
migrations add nullable link/receipt fields and a continuation table. Existing
rows remain standalone roots; do not infer families for legacy sessions. No
clinical data is rewritten. Main and every affected Web app deploy together.
New Web requires the new continuation endpoints. Keep additive schema on
rollback and revoke linked roots before rolling back to a backend that cannot
validate them; old code would otherwise treat app children independently.

When central Auth is configured, legacy Parties POST handoff operations return
404 and unbound legacy tickets cannot be issued or redeemed. This is an
intentional central-authentication contract migration; deploy the first-party
Web clients together. Without a configured central host, these routes retain
their previous semantics. Authentication/Parties own that compatibility window;
remove the legacy routes only in a versioned change after all installations use
continuations. Landing's onboarding response adds HandoffUrl; central deployments
return that login entry and an empty legacy Token, while unconfigured
installations retain the legacy token response. The setup draft remains owned
by Parties and is read after correlated onboarding entry.

Main registers patients, PM and optional knowledge hosts from typed
BrowserApplicationHostOptions. Patients is a platform tenant with no clinical
data and allows a subject session without a clinic role. PM uses its application-owned platform-admin or tenant-member policy; knowledge requires
explicit tenant roles; platform administration requires its global policy.
The knowledge demo remains outside the current production deployment topology.

## Alternatives and risks

Parent-domain cookies were rejected because they widen cookie trust to every
subdomain. Independent app sessions were rejected because they consume extra
browser slots and leave Auth signed in after app logout. Ending Auth on every
handoff was rejected because it prevents persistent SSO. A second identity
framework was unnecessary: reuse the durable registry and existing handoff.

Critical proof covers root concurrency/idle expiry, cross-browser and
cross-tenant rejection, token replay, app callbacks, pending challenge reload,
onboarding transitions, and family logout. Credential-dependent flows must use
Auth's allowed WebAuthn origin with the existing relying-party domain. Required
validation is the scoped owner tests, endpoint validators and the full real-host
Web E2E feedback workflow with inspected screenshots. Production health alone
does not prove authenticated browser behavior.

Auth cookie renewal follows successful authenticated app query activity through
an opaque credentialed GET to Auth, with no referrer, tenant header or response
read. Requests are throttled and timeout; network failures are logged without navigation
or local credential fallback. This uses the normal cookie validator, never
extends a revoked root, and does not run an idle heartbeat. Production CSP
permits exactly the configured Auth origin. Supported production apps share
one HTTPS site; local .localhost origins have different browser SameSite rules.

Continuation response loss or partial issuance failure consumes the request
and requires a fresh entry; do not reopen it after an uncertain ticket write.
Personal Journey dependency failures block central continuations rather than
falling back past mandatory gates. Pending recovery presentation remains neutral
until cookie expiry, even for unknown, blocked, failed-delivery or used server
challenges; only the completion endpoint decides the actual outcome.

Custom domains outside Auth's HTTPS site retain top-level SSO entry but cannot
renew a Lax Auth cookie in the background. The supported production topology
uses the configured vitarish.com site. Do not claim cross-site silent renewal.
Existing operator tenant bindings are preserved: PM admission uses its configured
origin and app-owned policy; patients uses its configured portal host while all
clinical resource and tenant-access checks remain in force.
