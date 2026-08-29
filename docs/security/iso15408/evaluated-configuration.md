# Vitarish evaluated configuration

Status: configuration baseline for issues [#766](https://github.com/mobinseven/Vitarish/issues/766) and [#767](https://github.com/mobinseven/Vitarish/issues/767). Reviewed 2026-08-15.

This document is the authoritative scope manifest for the initial ISO/IEC 15408 readiness configuration. It describes one machine running one Main process and its two required state services. It is not a claim that the wider repository or production deployment is evaluated. The Security Target and proposed requirements matrix are [`security-target.md`](security-target.md) and [`sfr-sar-matrix.md`](sfr-sar-matrix.md).

This profile is separate from the network-questionnaire v1.0 profile. Do not
reuse its identity, compose file, topology, or evidence for that profile; see
[`../network-questionnaire-v1.0/evaluated-configuration.md`](../network-questionnaire-v1.0/evaluated-configuration.md).

The TOE is evaluated in a single-machine configuration. Availability and integrity claims do not cover multi-instance failover. Database, cache, storage, operating system, container runtime, and ingress remain environmental dependencies unless explicitly included by a future Security Target. Distributed deployment, multi-region behavior, rolling upgrades, and cluster failover are outside the initial claim.

## Identity

| Item | Value | Evidence/owner |
| --- | --- | --- |
| TOE name | Vitarish Main evaluated configuration | This manifest |
| Configuration ID | `vitarish-toe-main-evaluated-2026-08-15` | `Vitarish__EvaluatedConfiguration__Identity` and `ManifestIdentity` in `docker-compose.iso15408.yml`; release operator owns the release record |
| Source baseline commit | `5a06b5ed02a174a4e848c51dcec8a2154d6a69ec` | `git rev-parse HEAD` at review; the Security Target and matrix are subsequent uncommitted worktree changes |
| Build profile | `Evaluated` | `src/Build/ModuleBuildProfiles.props`; runtime allowlist in module/app aggregators |
| Target framework | `net10.0` | `src/Directory.Build.props` |
| .NET SDK observed | `10.0.302` | `dotnet --version` |
| .NET runtime image | `10.0.11` linux-x64 | `docker exec vitarish-iso15408-main-1 dotnet --info` |
| Main artifact | `src/Hosts/Main/Main.csproj` published as `Vitarish.Main.dll` | Main project assembly naming and Dockerfile; evaluated compose passes the profile explicitly |
| Deployment mode | Single machine; Main, PostgreSQL, and Redis on one private container network | `docker-compose.iso15408.yml` |

The release operator MUST record the commit containing this evaluated slice, image digests, generated OpenAPI hash, and release artifact hashes before calling a deployment evaluated. The current local smoke evidence is from an uncommitted worktree and is not a release identity. Tags alone are not release identity.

## Enabled software

| Component | Role | Runtime inclusion | Version source |
| --- | --- | --- | --- |
| Main | HTTP host, middleware, authentication/authorization, tenant resolution, endpoint composition | Required | `src/Hosts/Main/Program.cs`, Main project |
| Authentication | Identity and token-facing module boundary | Enabled | `src/Modules/Authentication/Module` |
| Requests | Request transport/module boundary | Enabled | `src/Modules/Requests/Module` |
| Users | Accounts and user persistence boundary | Enabled | `src/Modules/Users/Module` |
| Parties | Tenant/party ownership boundary | Enabled | `src/Modules/Parties/Module` |
| Platform | Platform administration and tenant-facing platform boundary | Enabled | `src/Modules/Platform/Module` |
| Feed | Host feed aggregation and read-only feed boundary | Enabled | `src/Modules/Feed/Module`; required by `HostingComposition` |
| PostgreSQL 18.0 | Transactional persistence and Hangfire storage | Required environmental service | evaluated compose file; operator pins digest |
| Redis 7.4.5 | Data-protection key/certificate and distributed-cache store | Required environmental service | evaluated compose file; operator pins digest |

No app is enabled in evaluated runtime mode. This intentionally excludes Clinic, ProjectManagement, and Landing endpoints and their agent/medical/provider composition.

## Evaluated endpoint and capability allowlist

The evaluated HTTP surface is the endpoint mapping of these six module groups only. Route values and operation IDs remain owned by the module Contracts/API files; this table is the configuration allowlist, not a second route source of truth.

| Group | Base route | Mapping owner | Capability boundary |
| --- | --- | --- | --- |
| Authentication | `/api/modules/authentication` | `src/Modules/Authentication/Api` and `AuthenticationRoutes` | Login/challenge, session, logout, account credential changes, antiforgery, and passkey endpoints |
| Requests | `/api/modules/requests` | `src/Modules/Requests/Api` and `RequestsRoutes` | Request identity query |
| Users | `/api/modules/users` | `src/Modules/Users/Api` and `UsersApiRoutes` | User lookup/creation, profile/culture, WebAuthn credentials, personal access tokens, and role administration |
| Parties | `/api/modules/parties` | `src/Modules/Parties/Api` and `PartiesApiRoutes` | Owners, tenants, tenant indicators, setup, and platform-admin administration |
| Platform | `/api/modules/platform` | `src/Modules/Platform/Api` and `PlatformApiRoutes` | Locale notification administration |
| Feed | `/api/modules/feed` | `src/Modules/Feed/Api` and `FeedRoutes` | Feed items, sources, and configuration queries |

The only evaluated provider capabilities are PostgreSQL persistence/Hangfire storage and Redis data-protection/distributed cache. No AI, agent, medical/Tamin, communications, search, object-storage, social, commerce, payment, or provider-specific capability is enabled or represented in the evaluated compose file.

## Excluded integrations and residual risk

| Exclusion | Enforcement | Owner | Environmental assumption | Residual risk |
| --- | --- | --- | --- | --- |
| AI providers and agent orchestration | `Evaluated` profile omits Intelligence; evaluated runtime composes no apps; no agent-runtime service | Main/module composition owners | No external AI endpoint is reachable from the machine | A future build using `All` is outside this scope |
| Medical and Tamin workflows | Medical is omitted from `Evaluated`; no Clinic app | Medical and app composition owners | No medical provider credentials are supplied | Source remains present and must not be mistaken for TOE behavior |
| SMS, SMTP, notification providers | Communications omitted; no provider environment variables | Communications module owner | No outbound mail/SMS route is permitted | Generic network egress is an environmental control |
| Search and Elasticsearch | Search and Knowledge omitted; no Elasticsearch service | Search module owner | No Elasticsearch host is configured | Search code remains in repository artifacts outside this build |
| Social, commerce, payment, and provider-specific integrations | Social, Commerce, Files, Sea, Financial, Contents, Links, Appointments, and provider-specific capabilities omitted from evaluated build | Owning module composition | No provider credentials or endpoints are supplied | Existing public contracts remain non-evaluated compatibility surface |
| Web/agent containers | No Web or agent-runtime service in the evaluated compose | Host/deployment owner | Browser is an environmental user agent calling Main | Browser UI is not part of the evaluated software artifact |

Excluded public contracts are disabled by composition, not deleted or redesigned. A deployment that changes the build profile, enables an excluded module, adds an excluded service, or supplies excluded provider configuration is a different configuration identity.

## Deployment inputs

Required inputs are `Vitarish__Identity__Authority`, `Vitarish__Identity__DataProtectionPassword`, `Vitarish__Security__AuthenticationAttempts__CurrentPepper`, `ConnectionStrings__Db`, and `ConnectionStrings__Redis`; secret values are supplied through the deployment secret store or environment and are never recorded here. The evaluated compose file sets `Vitarish__EvaluatedConfiguration__Enabled=true`, uses configuration ID `vitarish-toe-main-evaluated-2026-08-15`, and builds with `MODULE_BUILD_PROFILE=Evaluated`.

The host binding is private (`127.0.0.1:5000` to Main's HTTP port `8080`). A separately operated TLS-terminating ingress is required for external traffic. The private HTTP upstream is not an externally secure interface and is covered by `OE.INGRESS`, not by a claim that Main itself terminates public TLS.

Required runtime services are reachable only on the private compose network. The Main host is published on `127.0.0.1:5000` as a private upstream for the required TLS reverse proxy/operator boundary; PostgreSQL and Redis are not published to the host.

## Interfaces and trust boundaries

1. Browser or operator client -> Main HTTPS/API boundary: untrusted HTTP, cookies, bearer tokens, antiforgery tokens, localization, and tenant-selection inputs. Transport validation belongs to API; authentication/authorization and tenant resolution belong to Main/Identity and the owning module.
2. Main -> PostgreSQL: trusted service boundary carrying parameterized queries, persisted accounts, tenant state, application state, audit state, and Hangfire records. Database credentials and storage encryption are environmental responsibilities.
3. Main -> Redis: trusted service boundary carrying data-protection keys/certificates and cache state. Redis authentication, network isolation, and persistence are environmental responsibilities.
4. Operator -> host/container runtime: privileged deployment boundary. The operator controls image digests, environment secrets, volumes, process lifecycle, and release identity.

The complete asset, interface, and data-flow inventory is in [`toe-inventory.md`](toe-inventory.md); the diagram is in [`topology.md`](topology.md).

## Validation record

- `git diff --check` is required for this change.
- `dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Evaluated` proves the evaluated dependency set compiles.
- A startup smoke test MUST run the evaluated compose file with non-production test secrets and verify the registered module/app list and absence of excluded endpoints/services before release.
- The release record MUST retain the resolved image digests, build output hash, generated OpenAPI hash, smoke-test output, and source commit.

### 2026-08-16 local candidate evidence

This is evidence for an uncommitted local candidate, not a release identity. The source baseline was `2f1fc8c94c3e38d72214782415a9d4dd90ed542b` with a dirty worktree containing this security slice and unrelated owner work. A release operator must replace that description with the clean commit containing the evaluated slice.

| Evidence ID | Result |
| --- | --- |
| `EV-768-20260816-STARTUP` | `docker compose -f src/Hosts/docker-compose.iso15408.yml up --build -d --wait` completed successfully with disposable test-only inputs. Main, PostgreSQL, and Redis reached healthy/running state; Main logged `Application started` in Production on HTTP port `8080`, published privately as `127.0.0.1:5000`. |
| `EV-768-20260816-INVALID-CONFIG` | A one-shot Main container with `ManifestIdentity=intentionally-invalid` exited non-zero (`139` reported by Docker) before host startup with `Security configuration validation failed: Security:EvaluatedConfiguration identity must match the deployment manifest.` |
| `EV-768-20260816-ARTIFACTS` | Main image digest `sha256:8b443da6f95391a6540c5e6f95cf0979014a813b46b57be3fd6671f4ad8db977`; PostgreSQL digest `sha256:41fc5342eefba6cc2ccda736aaf034bbbb7c3df0fdb81516eba1ba33f360162c`; Redis digest `sha256:90e7a336d044f1abc9e9dbc05d65566850896d11453bbd1dd0fb7e5059f0e8fb`. Published artifact SHA-256 values: `Vitarish.Main.dll` `64e40a31f3261cca079f71e8ef7f7a7014914c385c07d70733bc33c69e00a5a8`; `Vitarish.Main.deps.json` `f44bf611e2f1322989a0eea476aeb4b327bd260f5558e453901303a2e89b103b`; `Vitarish.Main.runtimeconfig.json` `eff38a9750f7cca2407b1d639a963d56ffea92088e6da971bbbfa74e3dcd7224`. |
| `EV-768-20260816-ROUTES` | Authentication antiforgery returned `500` over the private non-TLS upstream; Requests, Feed, Users, Parties, and Platform base prefixes returned `401`. Medical, Intelligence, Search, and Communications base prefixes also returned `401`, so base-prefix status is not proof that an excluded endpoint is absent. Exact endpoint probes remain dependent on #771's final inventory. |
| `EV-768-20260816-OPENAPI` | No evaluated OpenAPI hash was produced. Profile-specific generation failed during endpoint metadata inference because a `seaAccess` parameter was inferred as a request body for an endpoint that does not allow inferred body parameters. This is a release-evidence gap, not a hash to substitute with the wider checked-in Main specs. |

The antiforgery `500` occurred only when probing the private HTTP upstream directly; Main rejected the non-SSL request because secure antiforgery cookies are mandatory. The production/browser probe must traverse the TLS reverse proxy. This is an environmental prerequisite, not evidence of an excluded route.

The published image contains transitive compatibility assemblies from repository project references, but only the allowlisted modules/apps are registered and mapped when `Vitarish__EvaluatedConfiguration__Enabled=true`. Binary presence is not evaluated functionality; changing that composition flag or build profile changes the configuration identity.

No secret values, private keys, tokens, credentials, or production payloads belong in this document or its evidence.
