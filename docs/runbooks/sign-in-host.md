# Sign-In Host Runbook

How the shared sign-in host is configured in production, how to check it, and
how to switch it off. The design is in
[ADR-0021](../architecture/decisions/0021-central-browser-authentication.md).

## What it needs

People sign in at one address, `auth.vitarish.com`, and continue signed in to
platform home when no app intent exists, or their explicitly requested app. Clinic hosts use
`<subdomain>.darman.vitarish.com`. The ingress serves
that address:

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
| `VITARISH_PRODUCTION_SIGN_IN_SUBDOMAIN` | `auth` | Main `Vitarish__SignInUrl`, all browser apps' `VITARISH_SIGN_IN_URL` and the Caddy sign-in site, all as `https://<subdomain>.<VITARISH_PRODUCTION_SITE_DOMAIN>` |

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

1. Run the production workflow (`workflow_dispatch` on `main`) after the
   integrated revision passes its gates. The workflow selects affected services;
   central-auth changes require Main and the affected browser apps together.
2. Check it with an account that has two clinics:
   - Sign in on Auth without app intent; platform home lists both workspaces.
   - **Open** on one lands signed in on that clinic's `/home`.
   - With one clinic, sign-in still reaches home and waits for an explicit choice.
   - `https://<clinic host>/api/modules/parties/sign-in/clinics` answers 404:
     the list answers only on the sign-in host and on the clinic front page
     (the clinic base domain itself), where it lists only clinics.
   - Signed in on the clinic front page, the page offers "Open <clinic>" for
     each of your clinics, and `/home` or `/assistant` there return to that
     list instead of a "no access" screen.
   - `https://<clinic host>/login` redirects to the sign-in host's `/login`.
   - After a clinic opens, Auth stays signed in. Opening another authorized app
     installs its linked session without asking for credentials again.
   - On an email-code login, switch to another tab and return, then reload:
     code entry remains and no second message is sent.
   - Auth's account/security surface lists one browser despite app visits.
   - Logout in one app makes Auth and every linked app return 401 on their
     session endpoint; another permitted browser remains signed in.

## Rollback and host settings

Deploy Main and affected Web apps together. Do not disable the Auth indicator
while apps still redirect there. Before rolling back to a backend that predates
linked sessions, revoke linked browser roots through the current owner; keep
the additive schema. Rolling back only Web or removing only its environment
value does not re-enable app-local credential endpoints while Main still has a
configured central host.

Main also receives `Vitarish:PatientsUrl` and `Vitarish:ProjectManagementUrl`
from the existing production host variables. `Vitarish:KnowledgeQaUrl` is
optional and does not add a demo production service. Registered application
origins must be HTTPS in production and match their active host bindings.
WebAuthn keeps the existing relying-party domain and explicitly permits Auth's
HTTPS origin; existing credentials must match that relying-party domain.

## Limits and where to look

- Browser continuations expire after ten minutes and require the initiating
  app browser correlation at handoff redemption. Handoff links are single use,
  60 seconds, for one user and one host. Refusals are audited as authentication
  attempts, and the person can return to the shared sign-in host to try again.
- A logo is readable by anyone at `/api/modules/parties/public/logo` on its
  clinic's host, limited to 60 requests a minute per client IP; beyond that the
  API answers 429.
