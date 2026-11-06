# Sign-In Host Runbook

How the shared sign-in host is configured in production, how to check it, and
how to switch it off. The design is in
[ADR-0018](../architecture/decisions/0018-shared-sign-in-host.md), amended by
[ADR-0020](../architecture/decisions/0020-platform-home-and-app-aware-workspaces.md).

## What it needs

People sign in at `auth.vitarish.com` and continue to platform home, where
they can explicitly open a workspace or create one. The ingress serves the
sign-in address:

- DNS: the `*.vitarish.com` A record
  ([Ingress and DNS](../hosts/server-deployment-bundles.md#ingress-and-dns)).
- Proxy: the `PRODUCTION_SIGN_IN_HOST` site in `src/Hosts/deployment/Caddyfile`
  sends it to the owner Web app, and `/api/*` to Main. Every other
  `*.vitarish.com` host redirects to the landing page.
- Workspace creation from home needs onboarding on; see the
  [clinic onboarding runbook](clinic-onboarding.md).

## Settings

One value in the server's `/opt/vitarish/deployment/.env.shared`:

| Setting | Example | Used as |
| --- | --- | --- |
| `VITARISH_PRODUCTION_SIGN_IN_SUBDOMAIN` | `auth` | Main `Vitarish__SignInUrl`, the owner Web app's `VITARISH_SIGN_IN_URL` and the Caddy sign-in site, all as `https://<subdomain>.<VITARISH_PRODUCTION_SITE_DOMAIN>` |

- The setting is required. Use a subdomain no platform app already uses. On
  start, Main binds that host to the platform-owned sign-in tenant.

## Password and contact code

A password account with two-factor enabled receives its sign-in code by SMS to
its confirmed phone, or by email to its confirmed email when no confirmed phone
is available. Both the password and code are required. Protect the destination
inbox or phone independently, and verify email delivery before relying on an
email-only account for a production smoke check. If neither contact is
confirmed, the account cannot complete this sign-in. See the
[Authentication documentation](../platform/modules/authentication/README.md#password-sign-in-with-an-additional-code).

## Check it

1. Run the production workflow (`workflow_dispatch` on `main`) with Main
   selected after changing the value, so it restarts with it, and Caddy when
   the Caddyfile changed.
2. Check it with accounts that have zero, one, and two clinics:
   - Sign in on the sign-in host. Each account reaches platform home without
     automatically opening a clinic or starting onboarding.
   - Home lists the available workspaces; **Open** enters the selected clinic.
   - The sign-in host is signed out after its handoff. Under the default
     single-session policy, opening a clinic replaces the home session; a
     policy permitting parallel sessions can retain it.
   - `https://<clinic host>/api/modules/parties/sign-in/clinics` answers 404:
     the compatibility endpoint exists only on the sign-in host.
   - `https://<clinic host>/login` redirects to the sign-in host's `/login`.

## Switch it off

1. Set the sign-in host's indicator on the **Vitarish Sign-in** tenant to
   `Disabled`. The setting is required, so this is the switch. Platform
   administration has no control for an indicator's status yet, so a platform
   administrator sends it through `PUT /api/modules/parties/tenant-indicators/{id}`
   (`PartiesUpdateTenantIndicator`).

The sign-in host then resolves to no clinic. Clinic sign-in pages still
redirect there, so switching it off also needs a release that unsets the owner
Web app's `VITARISH_SIGN_IN_URL`; people then sign in on their clinic's own
host. Nothing in clinic data needs to be undone.

## Limits and where to look

- Handoff links from the sign-in host follow the onboarding rules: single use,
  60 seconds, for one user and one host. Refusals are audited as authentication
  attempts, and the person can return to the shared sign-in host to try again.
- A logo is readable by anyone at `/api/modules/parties/public/logo` on its
  clinic's host, limited to 60 requests a minute per client IP; beyond that the
  API answers 429.
