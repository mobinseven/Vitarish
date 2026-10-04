# Production deployment

`.github/workflows/production.yml` is the single production CI/CD workflow. It
uses job-level path filters while the workflow itself remains unfiltered, so a
required check always reaches a completed state. Pull requests to `main` run
checks for affected consumers and cannot publish or deploy. A push to `main`
runs those checks, publishes only affected images as
`ghcr.io/<owner>/vitarish/<app>:<full-commit-sha>`, resolves the registry digests
published by that run, then deploys those immutable references. Manual dispatch runs the full .NET, Web, security, and
Compose checks; it never publishes or deploys. There are no scheduled runs.

The workflow maps isolated Web app changes to that app, shared Web changes to
all Web apps, .NET changes to Main and the .NET suite, and API contract changes
to Main plus all Web apps. Unknown source paths select the broad checks and
image set. Production or ingress Compose changes select the production
services affected by that configuration. Test Compose changes run security
and example-render checks without publishing or touching production. A test or
security failure prevents publication and SSH access.

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
  proxy subnet/address pairs, and the shared response model.
- `.env.test` and `.env.production` for their environment's images and runtime
  credentials.
- Optional `.env.production.mediatr` for protected production MediatR values.
- `minio.license` for the licensed AIStor service.

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
`*.e2e.vitarish.com`, `*.clinic.e2e.vitarish.com`, `*.vitarish.com`, and
`*.clinic.vitarish.com` to the public ingress IPv4 address. Remove stale A and
AAAA records. Set matching production host values in `.env.shared` and
`.env.production`. Caddy uses the Cloudflare DNS challenge; its token should
have Zone read and DNS edit access for the required zone. Choose non-overlapping
proxy subnets that do not conflict with the host LAN, VPN, or other Docker
networks. Main trusts only the fixed Caddy IPv4 address on its environment
proxy network.

Caddy continues to manage its P-256 ECDSA certificates. It also loads the
separately issued RSA certificate bundle used for RSA-authenticated ECDHE
handshakes. The first deployment provisions that bundle through the Cloudflare
DNS challenge before Caddy can start; if issuance fails or the bundle is
missing, ingress validation fails before traffic is served. Follow the
[RSA certificate renewal runbook](caddy-rsa-certificate-renewal.md) for manual
renewal, validation, reload, and recovery. The RSA bundle is staged and checked
for every configured hostname, a trusted certificate path, CA constraints,
Server Authentication EKU, and CRL status before it becomes active. Caddy also
runs the same checks on the active RSA bundle before starting.

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
container's exact Compose image reference before capture. The first successful
production run and inspected host evidence must establish that the selected
digests were pulled and that container plus HTTPS checks passed. A local
architecture check or Compose render is not deployment evidence. Keep the
workflow run URL, release-plan artifact, commit SHA, changed service set, and
resulting container health as release evidence.

## Checks and two-host feedback

Run the repository security baseline and its positive/negative fixtures with:

```powershell
dotnet run --project tools/architecture -- security-baseline
dotnet run --project tools/architecture -- self-test
```

Render the committed examples without starting containers:

```powershell
docker compose --env-file src/Hosts/deployment/.env.shared.example -f src/Hosts/deployment/docker-compose.ingress.yml --profile certificate-operations --profile validation config --quiet
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
