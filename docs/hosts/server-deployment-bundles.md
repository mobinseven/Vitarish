# Server deployment bundles

The server reference lives under `src/Hosts/deployment`:

- `docker-compose.ingress.yml` owns the only host listeners (TCP 80 and 443), Caddy ACME state, and the shared proxy networks.
- `docker-compose.test.yml` owns the test Main/Web/agent runtime and its PostgreSQL, Redis, MinIO AIStor, and Elasticsearch data.
- `docker-compose.production.yml` owns the production equivalents with separate project names and volumes.
- `.env.shared` owns common image versions, the default intelligence response model, public hostnames, proxy network allocations, and ingress credentials. `.env.test` and `.env.production` own only their environment's application images and runtime credentials.

The bundles assume test and production share one Docker host. Internal HTTP links are valid only because the communicating containers are co-located on that host. No database, Redis, MinIO AIStor, Elasticsearch, Main, or agent-runtime port is published to the host. Each internal proxy network has an operator-selected subnet and a fixed Caddy address; Main trusts only that address for forwarded headers. Web images use same-origin `/api` requests, and Caddy sends those requests to Main on every application hostname. Main and the agent runtime use their environment's egress network for configured providers; the databases and Web applications have no external route. Choose non-overlapping proxy subnets that do not conflict with the server LAN, VPN, or other Docker networks.

The ingress supports wildcard environment routing. Configure DNS records for
`*.e2e.vitarish.com`, `*.clinic.e2e.vitarish.com`, `*.vitarish.com`, and
`*.clinic.vitarish.com` to the ingress IPv4 address as DNS-only A records, set the
corresponding wildcard host variables in the operator-managed environment
files, and provide a narrowly scoped Cloudflare API token with Zone read and
DNS edit access for the zone. The published Caddy image includes the Cloudflare DNS challenge
module. Exact application host routes remain more specific than the wildcard
fallback routes.
Every exact application hostname and wildcard sample must resolve only to that
public IPv4 address before deployment; remove stale A or AAAA records first.

## First deployment

Download the exact release commit's `release-images-<commit>.env` artifact to the operator machine. Copy `.env.shared.example`, `.env.test.example`, and `.env.production.example` to their matching untracked files in `src/Hosts/deployment`. Replace every placeholder and use the artifact's image references. Authenticate Docker to GHCR on the server with package read access; the default installer path uses root's protected `/root/.docker/config.json`. Alternatively, put a token with `read:packages` in `/root/.config/vitarish/ghcr-token`, owned by root with mode `600`, and supply `-RegistryUser` to the installer. Do not put the token in the repository or command line. The operator machine must trust the server's SSH host key before running the installer. Allow inbound TCP 80 and 443, and configure DNS above. Hostnames must resolve to the server before HTTPS readiness can pass.

For the initial deployment, the owner authorized generating missing runtime credentials on 2026-09-25. Generate each value independently with the operating system cryptographic random generator, store values only in the ignored `.env.test` and `.env.production` files, restrict those files to the operator account locally and mode `600` on the server, and never print or commit the values. This one-time approval covers the missing deployment credentials only; it does not permit reusing credentials across environments or changing repository secret handling requirements.

Place the licensed MinIO AIStor file at `src/Hosts/deployment/minio.license` on the operator machine. The installer copies it to `/opt/vitarish/deployment/minio.license` and restricts it to root mode `600`; Compose mounts it read-only into the AIStor containers. The license file is ignored by Git and must not be added to a release artifact or commit.

The deployment bundles use PostgreSQL 18 or later and mount the persistent volume at `/var/lib/postgresql`, which contains PostgreSQL's major-version-specific data directory. This target is required by the PostgreSQL 18+ official image; earlier image majors require a different mount target. For an existing PostgreSQL 17 or older cluster, do not start PostgreSQL 18 against its volume. Use a planned `pg_upgrade` or logical dump/restore into the new volume, then keep the old volume offline until application reads, writes, and backup checks pass. The deployment operator owns this migration; remove the old volume only after the new database has passed those checks and its backup is verified. See the official [PostgreSQL image storage guidance](https://hub.docker.com/_/postgres) for the version-specific `PGDATA` layout.

Main applies the Duende IdentityServer operational-store migration before serving traffic. The generated initial migration creates the operational tables and indexes without rewriting application data. Rolling back the application release leaves these tables and their migration history in place; do not apply the generated `Down` migration after grants, device codes, or sessions have been written because it drops that operational state.

For a host that already has OSS MinIO data, stop Main and every other writer and stop MinIO before touching its data volume. Copy the stopped volume to the new AIStor volume, then leave the original volume offline until the AIStor data and application checks pass:

```powershell
$sourceVolume = 'vitarish-test-minio'
$targetVolume = 'vitarish-test-aistor'
docker volume create $targetVolume
docker run --rm --volume "${sourceVolume}:/source:ro" --volume "${targetVolume}:/target" busybox:1.36.1 sh -c 'cp -a /source/. /target/'
```

AIStor upgrades the copied object-store metadata when it starts. Keep the original volume as the pre-upgrade recovery point; writes accepted by AIStor after cutover are not present in that copy. Do not run OSS MinIO and AIStor against the same volume at the same time. For the local-only `src/Hosts/docker-compose.main.yml`, use `minio-data` as the source and `aistor-data` as the target in the same copy procedure. The remote host checked for this rollout has no existing Vitarish data volume, so it requires no MinIO data migration.

From the repository root on the operator machine, the initial install command copies the exact Compose, Caddy, environment, manifest, and server scripts; installs Docker Engine and Compose if needed; uses the protected GHCR login or authenticates with the supplied token; and invokes the server preflight and deployment. `-Environment` selects `Both`, `Test`, or `Production`; its default is `Both`. `-ValidateOnly` copies and validates the inputs without starting Vitarish containers:

```powershell
pwsh -NoProfile -File src/Hosts/scripts/Install-UbuntuServer.ps1 -SshTarget root@203.0.113.10 -PublicIPv4 203.0.113.10 -ReleaseManifest src/Hosts/deployment/release-images-REPLACE_WITH_COMMIT.env -ValidateOnly
pwsh -NoProfile -File src/Hosts/scripts/Install-UbuntuServer.ps1 -SshTarget root@203.0.113.10 -PublicIPv4 203.0.113.10 -ReleaseManifest src/Hosts/deployment/release-images-REPLACE_WITH_COMMIT.env
pwsh -NoProfile -File src/Hosts/scripts/Install-UbuntuServer.ps1 -SshTarget root@203.0.113.10 -PublicIPv4 203.0.113.10 -ReleaseManifest src/Hosts/deployment/release-images-REPLACE_WITH_COMMIT.env -Environment Production
```

`Setup-UbuntuServer.sh` installs only missing Docker Engine and Compose prerequisites on Ubuntu 20.04 or 24.04 through the official [Docker package repository](https://docs.docker.com/engine/install/ubuntu/). It does not install PowerShell. On a server with Docker Engine already running, it adds only the Compose plugin. The operator command requires local PowerShell, SSH, and SCP; the server runs Bash only.

From the repository root, validate the operator files and rendered Compose bundles, then run the server deployment command. Replace the documentation IPv4 address with the public ingress address configured in DNS:

```powershell
$manifest = './release-images-REPLACE_WITH_COMMIT.env'
pwsh -NoProfile -File src/Hosts/scripts/Deploy-ServerBundles.ps1 -ValidateOnly -ReleaseManifest $manifest
pwsh -NoProfile -File src/Hosts/scripts/Deploy-ServerBundles.ps1 -ReleaseManifest $manifest -PublicIPv4 203.0.113.10
```

On Ubuntu 20.04 or 24.04, run `sudo bash src/Hosts/scripts/Setup-UbuntuServer.sh` to prepare Docker. Run the PowerShell repository gate on the operator machine, then deploy on the server with Bash; no PowerShell is installed on the server:

```bash
sudo bash src/Hosts/scripts/Deploy-ServerBundles.sh --release-manifest ./release-images-REPLACE_WITH_COMMIT.env --public-ipv4 203.0.113.10 --validate-only
sudo bash src/Hosts/scripts/Deploy-ServerBundles.sh --release-manifest ./release-images-REPLACE_WITH_COMMIT.env --public-ipv4 203.0.113.10
```

Run `pwsh -NoProfile -File src/Hosts/scripts/Test-DeploymentBundles.ps1` on the operator machine against the exact bundle that will be copied to the server. The Bash command checks protected environment files, image digests against the manifest, Compose rendering, DNS, Caddy validation, container readiness, and public HTTPS. Do not start the bundles until the release manifest, protected operator files, DNS, and registry access are in place.

The command checks Docker access, pinned release images, the deployment gate, all Compose renders, and DNS against the specified public ingress IPv4 address before changing containers. It then pulls the selected images, validates the Caddyfile in the pinned Caddy image, starts ingress and the selected environment, and waits for container health. `Both` deploys and probes test and production; a single-environment update requires that selected environment to already be running and probes only its configured HTTPS hostnames, including its wildcard sample. All configured hostnames must still resolve to the specified ingress address, and all Compose bundles are validated. A failed step returns a nonzero exit code and leaves containers and volumes in place for diagnosis. The probe checks TLS, HSTS, and a successful or redirected response for exact hosts; an unknown tenant under a wildcard may legitimately return a 4xx response. Verify a real tenant workflow separately before declaring the release ready. Main performs its owned database migrations and object-storage bucket creation during startup; the deployment does not add a second migration or bucket authority.

Every operator image reference must match the supplied release manifest exactly; the deployment command stops before pulling images when a reference differs.

Proxy subnets are part of the host trust boundary. Keep them stable during normal image updates. To change them on an existing host, stop both stacks and ingress in this order:

```powershell
docker compose --env-file .env.shared --env-file .env.test -f docker-compose.test.yml down
docker compose --env-file .env.shared --env-file .env.production -f docker-compose.production.yml down
docker compose --env-file .env.shared -f docker-compose.ingress.yml down
```

These commands do not remove volumes. Run the deployment command again; it recreates the proxy networks while retaining application data and Caddy state.

The deployment command runs the gate after reading `.env.shared`; the gate validates the operator-supplied proxy subnet and ingress address from that file. The example file is only a template and does not establish the configured trust boundary. For diagnosis, the equivalent manual renders and Caddy validation are:

```powershell
docker compose --env-file .env.shared -f docker-compose.ingress.yml config --quiet
docker compose --env-file .env.shared --env-file .env.test -f docker-compose.test.yml config --quiet
docker compose --env-file .env.shared --env-file .env.production -f docker-compose.production.yml config --quiet
docker compose --env-file .env.shared -f docker-compose.ingress.yml run --rm --no-deps caddy caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile
```

The ingress command uses the same pinned image and environment as the running reference bundle.

From the repository root, the deterministic checks are `src/Hosts/scripts/Test-DeploymentBundles.ps1`, `src/Hosts/scripts/Test-DeploymentBundles.Tests.ps1`, and `src/Hosts/scripts/Deploy-ServerBundles.Tests.ps1`. The bundle fixtures exercise valid configuration plus negative cases for mutable images, cross-environment resources, published state ports, missing health dependencies, invalid ingress addressing, broad proxy trust, missing route hosts, release-image name mismatch, and public agent routing. The deployment command fixtures render all three bundles with synthetic operator files and verify that missing image digests, clinic wildcard configuration, or the public ingress address stop deployment before startup.

The deployment preflight fixtures also reject image references that differ from the release manifest.

## Updates and rollback

Change common image references in `.env.shared` and application image references in the matching untracked environment file. Render the configuration, run the repository deployment gate, pull the exact release images, and recreate that environment. Keep the previous image manifest until the new stack has passed readiness and browser/API smoke checks. Roll back by restoring the previous manifest and recreating only the affected environment; retain additive database migrations and their tables so existing operational state remains compatible.

The `container-images` workflow publishes each application image with the commit SHA, resolves immutable digests for Caddy and MinIO AIStor, and uploads a release `.env` manifest. Copy `VITARISH_CADDY_IMAGE`, `VITARISH_AISTOR_IMAGE`, and the infrastructure image references into `.env.shared`, then copy the matching `VITARISH_TEST_*_IMAGE` or `VITARISH_PRODUCTION_*_IMAGE` values into the corresponding environment file. Keep secrets in these untracked files and restrict their access to deployment operators.

## Backups and recovery

Back up each environment independently and retain the release manifest with the backup:

```powershell
# PostgreSQL logical backup and restore (the container owns POSTGRES_USER/POSTGRES_DB)
docker compose --env-file .env.shared --env-file .env.test -f docker-compose.test.yml exec -T postgres sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" --format=custom' > .\backups\test-postgres.dump
Get-Content .\backups\test-postgres.dump -AsByteStream | docker compose --env-file .env.shared --env-file .env.test -f docker-compose.test.yml exec -T postgres sh -c 'pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists'

# Redis persistence backup (after confirming the service is quiescent)
docker compose --env-file .env.shared --env-file .env.test -f docker-compose.test.yml exec -T redis sh -c 'redis-cli -a "$REDIS_PASSWORD" BGSAVE'
docker compose --env-file .env.shared --env-file .env.test -f docker-compose.test.yml cp redis:/data/dump.rdb .\backups\test-redis.rdb

# MinIO AIStor is private; use the pinned AIStor image's mc client on the environment network.
New-Item -ItemType Directory -Force .\backups\test-aistor | Out-Null
docker compose --env-file .env.shared --env-file .env.test -f docker-compose.test.yml run --rm --no-deps --entrypoint mc -e "MC_HOST_vitarish-test=http://<access-key>:<secret-key>@aistor:9000" --volume "${PWD}\backups\test-aistor:/backup" aistor mirror --preserve vitarish-test/<bucket-name> /backup

# Elasticsearch: first register the operator-controlled repository using its approved storage settings,
# then execute the snapshot through the private service network.
docker compose --env-file .env.shared --env-file .env.test -f docker-compose.test.yml exec -T elasticsearch sh -c 'curl -fsS -u "elastic:$ELASTIC_PASSWORD" -X PUT "http://localhost:9200/_snapshot/vitarish-test-backups/release-<commit>?wait_for_completion=true"'
```

Restore each artifact into disposable environment-local volumes first, verify application readiness and representative reads/writes, then promote the restored volume or snapshot. Never copy production state into test without an explicit sanitized refresh procedure; deleting test volumes is the only supported test reset.

Run the same procedures against production by replacing the `test` Compose file, network, bucket, backup directory, and snapshot repository names with their `production` equivalents. Keep the credentials and backup destinations separate.

## Test reset and refresh

For a deliberate test reset, stop only the test project and remove its named volumes; this destroys test state and must never be run with the production project:

```powershell
docker compose --env-file .env.shared --env-file .env.test -f docker-compose.test.yml down
docker volume rm vitarish-test-postgres vitarish-test-redis vitarish-test-aistor vitarish-test-elasticsearch
docker compose --env-file .env.shared --env-file .env.test -f docker-compose.test.yml up -d
```

For a refresh from production, first restore a separately created, sanitized backup into disposable test volumes, verify tenant and secret scrubbing, then replace only the test volumes and rerun the readiness checks. There is no automatic production-to-test copy job.

The deployment gate is repository implementation evidence only. DNS, ACME issuance, image provenance, backup success, and the behavior of the exact deployed host require release/operator evidence.
