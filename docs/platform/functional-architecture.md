# Platform functional architecture

This document owns the platform's functional model. The home, app catalog and
journey described below follow [ADR-0020](../architecture/decisions/0020-platform-home-and-app-aware-workspaces.md); they are not all implemented yet.

## Platform and apps

Vitarish is a general-purpose business platform. Apps provide experiences for
specific needs; Darman is the clinic app. A workspace is one tenant of one app,
identified by `Tenant.AppId`. A person may have workspaces in different apps.

## Hosts

- `vitarish.com` is the public front door and hosts the signed-in platform home
  at `/home`.
- The authentication host (`auth.vitarish.com`) owns sign-in, personal account
  security and the durable browser session under [ADR-0021](../architecture/decisions/0021-central-browser-authentication.md).
  Every app, including platform administration, enters through Auth.
- Home and workspaces hold host-only linked sessions in the same browser
  family. Opening a workspace preserves home and consumes no additional browser
  slot. Signing out on any linked app ends that family.
- The onboarding host creates a workspace through the existing setup.
- The administration host is for platform administration.

## People and destinations

- Anonymous people use the public front door; sign-in starts on the sign-in
  host. An anonymous visit to the sign-in host root goes to `/login`.
- A signed-in person with no intent goes to platform home, whether they have
  zero, one or many workspaces. The home lists their workspaces by app.
- A signed-in person with an intent goes to its destination: a validated
  return path on its host, a workspace they choose, or onboarding when they
  choose to create a workspace. A return path is relative and validated for
  its destination host.

Never default a person into an app: do not automatically open a single
workspace or send someone with no workspaces to onboarding. Creating a
workspace is an explicit action.

## App catalog

The proposed `PlatformApp` catalog, owned by Parties Application, records each
app's `AppId`, availability (`Live` or `Soon`), self-service profile and whether
its tenants are user workspaces. Apps register their own entries. The directory,
home agent and creation choices use this catalog. The existing clinic
registration uses `SelfServiceTenantProfile`; the proposed catalog incorporates
or references that profile.

The platform-owned `SignInTenant`, `OnboardingTenant`, `PlatformAdminTenant`
and `LandingTenant` support platform hosts; they are not user workspaces.
