# ADR-0020: Platform home and app-aware workspaces

Date: 2026-11-06
Status: Accepted
Scope: Platform home, app catalog, workspace directory, sign-in and onboarding journeys
Owner: Architecture
Supersedes:
Superseded by: [ADR-0021](0021-central-browser-authentication.md) for session ownership and handoff compatibility; home and workspace selection remain in force
Related: [ADR-0016](0016-self-service-clinic-onboarding.md); [ADR-0017](0017-conversational-front-door-and-shared-agent-interaction.md); [ADR-0018](0018-shared-sign-in-host.md)

## Context

Sign-in currently treats every listed tenant as a clinic, automatically opens a
single clinic and sends a person with none to onboarding. Vitarish is a platform
for apps, and a person may not intend to enter or create a workspace. The
[functional architecture](../../platform/functional-architecture.md) defines
that model.

## Decision

1. **Home on the front-door host.** A signed-in person without a destination
   goes to platform home at `vitarish.com/home`, including people who already
   have workspaces. Home lists their workspaces by app. It never automatically
   opens one or starts onboarding when there are none.
2. **App-aware workspaces.** A proposed Parties Application `PlatformApp`
   catalog identifies workspace apps and their availability. The workspace
   directory excludes platform-owned tenants. Create is an explicit home action
   that enters the existing onboarding setup.
3. **Sign-in continues to home.** This amends ADR-0018 decision 5 and
   ADR-0016's zero-tenant flow: the Journey leads to a continue step on the
   sign-in host, which obtains a server-chosen handoff to the home host. A
   validated return intent may instead lead to its requested path. The sign-in
   host keeps no session after handoff, preserving ADR-0018 decision 8. The
   former sign-in host workspace picker is removed; the clinic sign-in API
   endpoints remain available for compatibility.
4. **Host isolation.** Cookies remain host-only; no cookie domain is widened.
   Handoff targets are chosen by the server. Return paths are relative and
   validated for the destination host. Workspace handoff issuance does not end
   the home session, but redemption follows the existing concurrent-session
   policy: under the default single-session policy, the workspace session
   replaces the home session; where parallel sessions are allowed, home remains
   signed in and workspace sign-out does not sign it out. Creating a workspace
   retains the existing onboarding creation and session-ending behavior.
5. **Home agent memory.** The home agent has no memory. ADR-0017 section 6 is
   unchanged.

## Consequences

- A clinic user takes an additional click from home to the workspace. People
  with no workspaces can stay at home until they choose to create one.
- The catalog and directory become the source for app-aware workspace choices.
  Existing clinic list contracts need additive app information; generated
  clients must be regenerated from their source. No schema migration is expected.
- Existing handoff limits and target authorization remain in force. Home and
  workspace sessions use host-only cookies and remain subject to the
  concurrent-session policy.
- Rollback restores the previous sign-in journey; existing tenants and data
  remain. Changes to handoff and journey behavior must be rolled back together.

## Alternatives Considered

### A domain-wide session cookie

Rejected: it weakens the per-host isolation established by ADR-0016.

### Home on the sign-in host

Rejected: the front-door host would remain unaware of the user's session.

### Automatically open a single workspace

Rejected by the owner: entering an app requires a person's intent.

## Validation / Enforcement

- Verify the catalog and workspace filter, server-chosen handoffs and relative
  return-path validation with focused tests.
- Verify zero and one workspace journeys, session-policy behavior, sign-in root,
  return intent and platform-tenant exclusion in two-host E2E.
- Verify sign-in and password recovery on the deployed hosts after release.

## Notes

- The landing conversation runtime path has not been verified in production.
- Revisit when a second app ships, home agent memory is wanted, or in-app
  workspace switching is designed.

## Amendments

- 2026-11-07, owner decision: the platform home has an **Account** area at
  `/account` on the landing host, reached by an Account button next to Sign
  out. It offers the sign-in settings (password and two-step sign-in), email
  change and phone change, using the same shared account hooks and server
  operations as the clinic app, so a person with no workspace can manage how
  they sign in. Passkeys, name, language and security history stay in the clinic
  app for now. Reason: an account without a clinic had nowhere to set a
  password.
