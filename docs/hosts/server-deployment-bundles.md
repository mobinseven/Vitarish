# Production deployment

`.github/workflows/production.yml` is the single production CI/CD workflow. It
uses job-level path filters while the workflow itself remains unfiltered, so a
required check always reaches a completed state. Pull requests to `main` and
pushes to `main` run checks for affected consumers and cannot publish or
deploy; draft pull requests skip checks until marked ready, and a newer push or
PR update cancels in-flight checks. A release is a manual dispatch of the
workflow on `main`. It selects the changes since the commit of the newest
successful `production` GitHub deployment, runs the affected checks, publishes
only affected images in parallel as
`ghcr.io/<owner>/vitarish/<app>:<full-commit-sha>`, resolves the registry digests
published by that run, then deploys those immutable references only after every
check passed. A failed release does not move that base, so the next release
selects its changes again. Without a successful deployment that is an ancestor
of the dispatched commit, a release runs the full .NET, Web, security, and
Compose checks and publishes and deploys every service. Releases are never
cancelled by later pushes. There are no scheduled runs.

The workflow maps isolated Web app changes to that app, shared Web changes to
all Web apps, .NET changes to Main and the .NET suite, and API contract changes
to Main plus all Web apps. Unknown source paths select the broad checks and
image set. Production or ingress Compose changes select the production
services affected by that configuration. Test Compose changes run security
and example-render checks without publishing or touching production. A test or
security failure prevents SSH deployment; published images remain immutable
commit-SHA tags and are never deployed unless every check passed.

## Production GitHub Environment

Create the GitHub Environment named `production` and add these required
secrets. They are available only to the deploy job:

| Secret | Value |
| --- | --- |
| `PRODUCTION_SSH_HOST` | Production server address from the trusted operator inventory. |
| `PRODUCTION_SSH_USER` | `root`; the deployment updates root-owned protected configuration under `/opt/vitarish/deployment`. |
| `PRODUCTION_SSH_PRIVATE_KEY` | Private key for that account. |
| `PRODUCTION_SSH_KNOWN_HOSTS` | Pinned `known_hosts` entry for the server. |

The job uses strict host-key checking. It does not receive the server's GHCR
credential or application environment values. Pull requests use read-only
GitHub permissions. Fork pull requests do not receive private package
credentials.

## Server preparation and protected configuration

The deployment uses one Ubuntu host for the ingress and both Compose
environments. Internal HTTP links are allowed only because the communicating
containers share that host. Caddy is the only service that publishes TCP 80
and 443. Main, databases, Redis, AIStor, Elasticsearch, and the agent runtime
remain private. Test and production use separate Compose projects, networks,
volumes, credentials, and application images.

Prepare Docker Engine, Compose, and the host's GHCR read login once from the
operator workstation. Trust the SSH host key before running this command:

```powershell
pwsh -NoProfile -File src/Hosts/scripts/Install-UbuntuServer.ps1 -SshTarget root@203.0.113.10
```

`Setup-UbuntuServer.sh` installs only missing Docker Engine and Compose
prerequisites on Ubuntu 20.04 or 24.04. `Prepare-UbuntuRelease.sh` retains the
existing registry bootstrap: it checks root's protected
`/root/.docker/config.json`, or reads a root-owned mode-600 token from
`/root/.config/vitarish/ghcr-token` when `-RegistryUser` is supplied. Do not
put a registry token in the repository, workflow, or command line. The
installer copies only these bootstrap scripts; it does not deploy an
application release.

Keep these operator-owned files on the server under
`/opt/vitarish/deployment`, owned by root and mode `600`:

- `.env.shared` for shared image references, hostnames, ingress credentials,
  proxy subnet/address pairs, the shared response model, and each
  environment's site domain (`VITARISH_<ENV>_SITE_DOMAIN`, the domain all its
  hosts share), which scopes the locale cookie so a language choice holds on
  every host.
- `.env.test` and `.env.production` for their environment's images and runtime
  credentials.
- Optional `.env.production.mediatr` for protected production MediatR values.
- `minio.license` for the licensed AIStor service.
- `brand-fonts/AhangVariableGX.woff` (folder mode `700`) for the licensed Ahang
  brand font. `.env.shared` names the folder as `VITARISH_BRAND_FONT_DIR`; the
  ingress mounts it read-only and serves it at `/fonts/ahang/AhangVariable.woff`
  on the owner, platform-admin, and landing hosts; see
  [Web brand font](web/brand-font.md).

The workflow does not replace these files. Place valid digest-pinned Caddy and
production application image references in them before the first deployment;
unselected images are started from these protected references. The workflow
updates only selected production image references after health and HTTPS probes
pass. Keep test and production
credentials independent. Never print, commit, or upload these files as CI
artifacts.

The Compose bundles use PostgreSQL 18 or later with the persistent volume at
`/var/lib/postgresql`. Do not start PostgreSQL 18 against a PostgreSQL 17 or
older data volume. Plan `pg_upgrade` or a logical dump/restore into a new
volume, verify application reads and writes and a backup, then retain the old
volume offline until recovery is no longer needed. Main owns its database
migrations. Application rollback leaves migration history and additive
database state in place; do not apply a migration `Down` operation as part of
release recovery.

For existing OSS MinIO data, stop Main and every other writer and stop MinIO
before copying its volume to a new AIStor volume. Keep the original offline
until AIStor data and application checks pass. Never run MinIO and AIStor
against the same volume. Compose updates do not remove database, object-store,
search, Redis, or ingress volumes.

## Ingress and DNS

Configure DNS-only A records for the exact application hosts and for
`*.e2e.vitarish.com`, `*.darman.e2e.vitarish.com`, `*.vitarish.com`, and
`*.darman.vitarish.com` to the public ingress IPv4 address. Remove stale A and
AAAA records. Set matching production host values in `.env.shared` and
`.env.production`. Caddy uses the Cloudflare DNS challenge; its token should
have Zone read and DNS edit access for the required zone. Choose non-overlapping
proxy subnets that do not conflict with the host LAN, VPN, or other Docker
networks. Main trusts only the fixed Caddy IPv4 address on its environment
proxy network.

Self-service clinic onboarding uses the `*.darman.vitarish.com` host and is off
until its settings are set; see the
[clinic onboarding runbook](../runbooks/clinic-onboarding.md). The shared
sign-in host, for example `auth.vitarish.com`, uses the `*.vitarish.com` host
and is off until its setting is set; see the
[sign-in host runbook](../runbooks/sign-in-host.md).

The repository's examples use deliberately invalid image and credential
values. They are for Compose rendering only; replace them in the protected
operator files before bringing services up. The security baseline validates
committed topology and examples. The production job additionally runs
`docker compose config --quiet` on the server with the actual `.env.shared`,
`.env.test`, and `.env.production` files before pulling or restarting a
service.

## Workflow deployment and recovery

For each changed production service, the serialized deploy job transfers the
tracked Compose and Caddy configuration and a release plan. The publish jobs
record their image digests; the deploy job verifies each digest against its SHA
tag in GHCR and writes a temporary env file with only those digest references.
The server validates all Compose bundles against its protected env files before
restarting services and pulls the selected digests. On a prepared but empty
host, it starts Caddy to create the proxy networks, then PostgreSQL, Redis,
AIStor, Elasticsearch, and the missing application services from the protected
image references. On an existing host it recreates selected services and leaves
unaffected running services in place. Compose waits for container health; it
never removes volumes or runs a second migration authority.

After container health passes, the job probes Main's existing
`/health/ready` endpoint over the configured production HTTPS host and probes
the affected production HTTPS route. A Caddy or shared Compose change probes
all production routes. The probes require valid TLS and a successful or
redirect response.

Previous image references and tracked configuration are copied before any
active file is replaced. The rollback trap is installed before replacement.
If validation, pull, container health, or an HTTPS probe fails, the job restores
the prior protected image references and configuration and restarts previously
running affected services. On an empty host, it stops the attempted application
and ingress services while retaining initialized data volumes for diagnosis.
The deploy job reports failure even when rollback succeeds; inspect the job
output and production health before retrying. Database migration history is
retained and may require application compatibility review before rollback.

After probes pass, the server writes a root-owned `.workflow-deployed.json`
with the workflow commit, run ID, selected services, and hashes of the deployed
configuration and protected environment files. This identifies a
configuration-only release even when it publishes no new image. The browser
evidence command checks that record, the current file hashes, and each running
container's exact Compose image reference before capture. It also asserts the
ingress-owned NSMAIL-1 response headers (`Strict-Transport-Security`,
`Cache-Control`, `Pragma`, `X-Content-Type-Options`, `X-Frame-Options`, and
`X-XSS-Protection`) on a document, an API, and a `/_next/static/` asset
response, and confirms the immutable asset keeps its upstream long-lived
`Cache-Control`. The first successful
production run and inspected host evidence must establish that the selected
digests were pulled and that container plus HTTPS checks passed. A local
architecture check or Compose render is not deployment evidence. Keep the
workflow run URL, release-plan artifact, commit SHA, changed service set, and
resulting container health as release evidence.

The same command captures transport and hardening evidence for every configured
production hostname in the run directory and records it in `manifest.json`:
the TLS protocol and cipher scan (`tls-scan.png`), the certificate presented by
the TLS 1.3 handshake (`certificate.png`), the plain HTTP to HTTPS redirect
(`http-redirect.png`), the ingress method guard for TRACE and CONNECT
(`unsafe-methods.png`), a TCP connect scan of the public ingress address
(`port-scan.png`), and server time synchronisation read over the SSH target
(`time-sync.png`). The checks expect TLS 1.0 and 1.1 plus the listed weak TLS
1.2 ciphers to be rejected, TLS 1.2 and 1.3 to be accepted, the certificate to
be authorized by the system trust store, hostname-matched, and valid for at
least 14 more days, a redirect to the same host over HTTPS, HTTP 405 for
unsafe methods, only TCP 22, 80, and 443 open, and `NTPSynchronized=yes` with a
clock difference under 5 seconds from the runner. When the runner's TLS client
cannot offer a legacy protocol or weak cipher, the table records it as not
testable from this client instead of claiming rejection.

## Checks and two-host feedback

Run the repository security baseline and its positive/negative fixtures with:

```powershell
dotnet run --project tools/architecture -- security-baseline
dotnet run --project tools/architecture -- self-test
```

Render the committed examples without starting containers:

```powershell
docker compose --env-file src/Hosts/deployment/.env.shared.example -f src/Hosts/deployment/docker-compose.ingress.yml config --quiet
docker compose --env-file src/Hosts/deployment/.env.shared.example --env-file src/Hosts/deployment/.env.test.example -f src/Hosts/deployment/docker-compose.test.yml config --quiet
docker compose --env-file src/Hosts/deployment/.env.shared.example --env-file src/Hosts/deployment/.env.production.example -f src/Hosts/deployment/docker-compose.production.yml config --quiet
```

Host-composition changes also require the real two-host flow from
`src/Hosts/Web`: `npm run test:e2e:two-host`. Capture its screenshot at a new
path under `src/Hosts/Web/artifacts/two-host-e2e/` and inspect it for the
authenticated user flow, loading state, and rendered content.

## Backups and reset

Back up test and production state independently. Store backups separately from
the Git worktree and retain the deployed commit SHA and image references with
each backup. Restore into disposable environment-local volumes first and
verify representative application reads and writes before promoting them.
Never copy production state to test without an approved sanitization process.

Test volume removal is destructive and is not part of deployment. If an
operator intentionally resets test data, confirm the test project and volume
names before removal; never run a test reset command against production.

The architecture command proves repository configuration properties. DNS,
ACME issuance, private registry access, browser behavior, backup success, and
the deployed host's security behavior require runtime or operator evidence.
