# Authentication module operating contract

This module owns authentication access policy, challenge/login behavior,
server-owned browser sessions, login history, and security notices. It follows
the parent module contract. Users capabilities and Authentication
Infrastructure implement required ports; Identity client-registration and
Hosting composition are not alternate policy owners.

## Policy and authentication

- **[MUST]** Keep password complexity, failed-attempt threshold/response,
  enabled mechanisms, declared pre-authentication actions, concurrent-session
  bounds, idle-session bounds, and allowed-IP policy in cohesive typed owners.
  Do not build a string-keyed settings bag or scatter defaults across API,
  Identity, Infrastructure, Web, or configuration binding.
- **[MUST]** The minimum password baseline is 8 characters with upper-case,
  lower-case, numeric, and special characters. Account creation, invitation,
  password change, reset, and administrative reset use the same effective
  validator. Administrator configuration may strengthen but not weaken it.
- **[MUST]** Apply failed-attempt policy at every authentication entry point.
  Known and unknown identifiers use the same positive threshold (default 5),
  denial duration (default 15 minutes), response shape, observable timing
  class, and audit outcome. Unknown-identifier tracking must be
  privacy-preserving and scoped to the authoritative authentication realm;
  use tenant scope only when tenant is trusted before authentication. Never
  create a user-discovery oracle.
- **[MUST]** Anonymous/pre-authentication actions are an explicit allowlist
  with written endpoint justification. Adding a route to an anonymous group is
  not sufficient authorization for a new pre-authentication capability.
- **[MUST]** Do not call a release MFA merely because password, OTP, or passkey
  flows coexist. Claim MFA only when the effective policy requires independent
  factors in one authentication.

## Sessions and access history

- **[MUST]** The durable server session registry is authoritative; cookies and
  claims carry only the minimum opaque reference. Session creation, maximum
  enforcement, replacement/rejection, revocation, acknowledgement, and notice
  creation are transactional or have an explicit safe compensation path.
- **[MUST]** Default to one active browser session per user. A configurable
  maximum of 2 or 3 requires a recorded functional need, and every new
  parallel session creates a durable notice for every previously active
  session. Do not wait for the maximum or replacement event to notify.
- **[MUST]** Default idle expiry is 30 minutes. Authorized administrators may
  set global or per-user values only from 1 through 30 minutes. Resolve one
  effective value server-side; cookie/sliding expiration must not outlive it.
- **[MUST]** Missing, revoked, expired, wrong-tenant, disallowed-IP, or
  policy-invalid sessions fail closed on protected cookie requests. Password,
  role, authentication-method, allowed-IP, or material policy changes revoke
  affected sessions before success is confirmed.
- **[MUST]** Persist last successful attempt, last failed attempt, failures
  since prior success, date/time, and acknowledgement state. A query does not
  acknowledge or mutate history; use an explicit authorized command.
- **[MUST]** Consume only the normalized client IP supplied by the trusted host
  boundary. Never parse `Forwarded` or `X-Forwarded-For` in this module.

## Audit, compatibility, and proof

- **[MUST]** Audit authentication/session start and outcomes, policy changes,
  lockout/denial, session creation/replacement/revocation, history
  acknowledgement, and allowed/denied administrative actions. Never include
  passwords, OTPs, tokens, passkey material, request bodies, or raw recovery
  data. A mutation requiring audit confirmation must not report success when
  the audit store is unavailable.
- **[MUST]** Session IDs, policy versions, notice states, current-session DTOs,
  cookies, and persisted history are compatibility boundaries. Change them
  additively with generated migrations/clients and document one-time legacy
  cookie invalidation or mixed-version behavior.
- **[MUST]** Run focused Authentication tests and
  `dotnet run --project tools/architecture -- openapi-endpoints` for endpoint
  changes. Session, cookie, login-history, browser-authentication, or policy
  changes also require the real two-host E2E workflow and screenshot
  inspection from `src/Hosts/Web/AGENTS.md`.
