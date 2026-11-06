# ADR-0020: Platform home and app-aware workspaces

Date: 2026-11-06
Status: Proposed
Scope: Platform home, app catalog, workspace directory, sign-in and onboarding journeys
Owner: Architecture
Supersedes:
Superseded by:
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
   host keeps no session after handoff, preserving ADR-0018 decision 8.
4. **Host isolation.** Cookies remain host-only; no cookie domain is widened.
   Handoff targets are chosen by the server. Return paths are relative and
   validated for the destination host. Leaving home for a workspace retains the
   home session; workspace sign-out does not sign out home. Creating a workspace
   retains the existing onboarding creation and session-ending behavior.
5. **Home agent memory.** The home agent has no memory. ADR-0017 section 6 is
   unchanged.

## Consequences

- A clinic user takes an additional click from home to the workspace. People
  with no workspaces can stay at home until they choose to create one.
- The catalog and directory become the source for app-aware workspace choices.
  Existing clinic list contracts need additive app information; generated
  clients must be regenerated from their source. No schema migration is expected.
- Existing handoff limits and target authorization remain in force. The home
  session and workspace session are separate host-only sessions.
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
- Verify zero and one workspace journeys, retained home session, sign-in root,
  return intent and platform-tenant exclusion in two-host E2E.
- Verify sign-in and password recovery on the deployed hosts after release.

## Notes

- The landing conversation runtime path has not been verified in production.
- Revisit when a second app ships, home agent memory is wanted, or in-app
  workspace switching is designed.
