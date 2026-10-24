# Clinic Onboarding Runbook

How to switch self-service clinic onboarding on and off in production, and how
to check it. The design is in
[ADR-0016](../architecture/decisions/0016-self-service-clinic-onboarding.md).

## What it needs

Each clinic lives at `<subdomain>.clinic.vitarish.com`, and people sign up and
create clinics on one onboarding host under the same domain, for example
`start.clinic.vitarish.com`. The ingress already serves that domain:

- DNS: the `*.clinic.vitarish.com` A record points at the ingress
  ([Ingress and DNS](../hosts/server-deployment-bundles.md#ingress-and-dns)).
- TLS: Caddy gets the wildcard certificate through the Cloudflare DNS-01
  challenge.
- Proxy: the `PRODUCTION_CLINIC_WILDCARD_HOST` site in
  `src/Hosts/deployment/Caddyfile` sends every clinic host, including the
  onboarding host, to the owner Web app, and `/api/*` to Main.
- CSP: `connect-src` allows `https://tiles.openfreemap.org`, where the setup
  page's map loads its styles and tiles, and `img-src` allows the clinic
  wildcard hosts, where the sign-in page loads each clinic's public logo
  (`src/Hosts/deployment/docker-compose.ingress.yml`).
- SMS: sign-up sends a verification code through the production SMS provider,
  so its key, line and verification template must work.

## Settings

All three live in the server's `/opt/vitarish/deployment/.env.shared`:

| Setting | Example | Used as |
| --- | --- | --- |
| `VITARISH_PRODUCTION_TENANT_ONBOARDING` | `true` | Main `Vitarish__Features__TenantOnboarding`; owner Web and landing `VITARISH_TENANT_ONBOARDING` |
| `VITARISH_PRODUCTION_CLINIC_BASE_DOMAIN` | `clinic.vitarish.com` | Main `Vitarish__TenantBaseDomain` |
| `VITARISH_PRODUCTION_ONBOARDING_SUBDOMAIN` | `start` | `https://<subdomain>.<base domain>` as Main `Vitarish__OnboardingUrl`; owner Web and landing `VITARISH_ONBOARDING_URL` |

- The flag is the only switch. The base domain and subdomain are required on
  every deploy, whether onboarding is on or off.
- The base domain is the clinic wildcard host without `*.`.
- Pick a subdomain no clinic already uses; on start, Main binds that host to the
  platform-owned onboarding tenant.
- `.env.shared` files from before this setting have
  `VITARISH_PRODUCTION_ONBOARDING_URL` instead. Replace it with
  `VITARISH_PRODUCTION_ONBOARDING_SUBDOMAIN` (for example `start`) before the
  next production deploy; until then, compose refuses to start.

## Switch it on

1. Set `VITARISH_PRODUCTION_TENANT_ONBOARDING=true` in `.env.shared`.
2. Run the production workflow (`workflow_dispatch` on `main`) with Main and the
   owner Web and landing apps selected, so the containers restart with the new values.
3. Check it:
   - The sign-in page on the public host shows the link to create a clinic.
   - On the onboarding host, sign up with a test phone number, complete the
     profile and reach `/setup`.
   - A taken address is reported as taken; a new one as available.
   - Creating the clinic shows its address, and **Go to my clinic** lands signed
     in on `https://<subdomain>.clinic.vitarish.com/home`.
   - Opening the same handoff link again is refused.
4. Remove the test clinic afterwards from platform administration.

## Switch it off

1. Set `VITARISH_PRODUCTION_TENANT_ONBOARDING=false` in `.env.shared`.
2. Redeploy Main and the owner Web and landing apps.

The creation endpoints and the onboarding handoff then answer 404, and the
"create a clinic" links disappear. Clinics
already created keep working. Nothing in the database needs to be undone.

## Limits and where to look

- A person can create 3 clinics a day; an IP address can send 10 creation
  requests an hour. Beyond that the API answers 429.
- Creation outcomes are in the security audit as
  `parties.tenant.self-service-created`, with the creator, the new clinic and
  the reason for a refusal.
- Handoff refusals are audited as authentication attempts. A creator who was
  refused can sign in on the new clinic normally.
