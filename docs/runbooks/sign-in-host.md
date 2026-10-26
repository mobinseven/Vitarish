# Sign-In Host Runbook

How the shared sign-in host is configured in production, how to check it, and
how to switch it off. The design is in
[ADR-0018](../architecture/decisions/0018-shared-sign-in-host.md).

## What it needs

People sign in at one address, `auth.vitarish.com`, and continue signed in to
one of their clinics at `<subdomain>.darman.vitarish.com`. The ingress serves
that address:

- DNS: the `*.vitarish.com` A record
  ([Ingress and DNS](../hosts/server-deployment-bundles.md#ingress-and-dns)).
- Proxy: the `PRODUCTION_SIGN_IN_HOST` site in `src/Hosts/deployment/Caddyfile`
  sends it to the owner Web app, and `/api/*` to Main. Every other
  `*.vitarish.com` host redirects to the landing page.
- CSP: `img-src` allows the clinic wildcard hosts, where the clinic list loads
  each clinic's public logo (`src/Hosts/deployment/docker-compose.ingress.yml`).
- Clinic creation from the sign-in host also needs onboarding on; see the
  [clinic onboarding runbook](clinic-onboarding.md). Without it the list offers
  no **Create a new clinic**.

## Settings

One value in the server's `/opt/vitarish/deployment/.env.shared`:

| Setting | Example | Used as |
| --- | --- | --- |
| `VITARISH_PRODUCTION_SIGN_IN_SUBDOMAIN` | `auth` | Main `Vitarish__SignInUrl` and the Caddy sign-in site, both as `https://<subdomain>.<VITARISH_PRODUCTION_SITE_DOMAIN>` |

- The setting is required. Use a subdomain no platform app already uses. On
  start, Main binds that host to the platform-owned sign-in tenant.

## Check it

1. Run the production workflow (`workflow_dispatch` on `main`) with Main
   selected after changing the value, so it restarts with it, and Caddy when
   the Caddyfile changed.
2. Check it with an account that has two clinics:
   - Sign in on the sign-in host; **Choose a clinic** lists both, each with its
     address and its logo, or its initials when it has none.
   - **Open** on one lands signed in on that clinic's `/home`.
   - With one clinic, sign-in continues to it by itself.
   - `https://<clinic host>/api/modules/parties/sign-in/clinics` answers 404:
     the list exists only on the sign-in host.

## Switch it off

1. Set the sign-in host's indicator on the **Vitarish Sign-in** tenant to
   `Disabled`. The setting is required, so this is the switch. Platform
   administration has no control for an indicator's status yet, so a platform
   administrator sends it through `PUT /api/modules/parties/tenant-indicators/{id}`
   (`PartiesUpdateTenantIndicator`).

The sign-in host then resolves to no clinic, and people sign in on their
clinic's own host. Nothing in clinic data needs to be undone.

## Limits and where to look

- Handoff links from the sign-in host follow the onboarding rules: single use,
  60 seconds, for one user and one host. Refusals are audited as authentication
  attempts, and the person can sign in on the clinic directly.
- A logo is readable by anyone at `/api/modules/parties/public/logo` on its
  clinic's host, limited to 60 requests a minute per client IP; beyond that the
  API answers 429.
