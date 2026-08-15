# Vitarish evaluated configuration

Status: configuration baseline for issues [#766](https://github.com/mobinseven/Vitarish/issues/766) and [#767](https://github.com/mobinseven/Vitarish/issues/767). Reviewed 2026-08-15.

This document is the authoritative scope manifest for the initial ISO/IEC 15408 readiness configuration. It describes one machine running one Main process and its two required state services. It is not a claim that the wider repository or production deployment is evaluated.

## Identity

| Item | Value | Evidence/owner |
| --- | --- | --- |
| TOE name | Vitarish Main evaluated configuration | This manifest |
| Configuration ID | `vitarish-toe-main-evaluated-2026-08-15` | This manifest; release operator owns the release record |
| Source baseline commit | `b826ec212135bb32a0da9f23734976915595d6fc` | `git rev-parse HEAD`; the evaluated slice is currently an uncommitted worktree change |
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

The evaluated HTTP surface is the endpoint mapping of these five module groups only. Route values and operation IDs remain owned by the module Contracts/API files; this table is the configuration allowlist, not a second route source of truth.

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
| Social, commerce, payment, and provider-specific integrations | Social, Commerce, Files, Sea, Financial, Contents, Links, Feed, Appointments omitted from evaluated build | Owning module composition | No provider credentials or endpoints are supplied | Existing public contracts remain non-evaluated compatibility surface |
| Web/agent containers | No Web or agent-runtime service in the evaluated compose | Host/deployment owner | Browser is an environmental user agent calling Main | Browser UI is not part of the evaluated software artifact |

Excluded public contracts are disabled by composition, not deleted or redesigned. A deployment that changes the build profile, enables an excluded module, adds an excluded service, or supplies excluded provider configuration is a different configuration identity.

## Deployment inputs

Required non-secret inputs are `Vitarish__Identity__Authority`, `Vitarish__Identity__DataProtectionPassword`, `ConnectionStrings__Db`, and `ConnectionStrings__Redis`; values are supplied through the deployment secret store or environment and are never recorded here. The evaluated compose file sets `Vitarish__EvaluatedConfiguration__Enabled=true` and builds with `MODULE_BUILD_PROFILE=Evaluated`.

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

### 2026-08-15 local smoke evidence

The reference compose stack built and started successfully with test-only inputs. Observed image IDs were Main `sha256:2e83ab3b6d1c2d2f9d48a5f765658110c2d83782d2de48cdd0f85f6dfc1fcbad`, PostgreSQL `sha256:41fc5342eefba6cc2ccda736aaf034bbbb7c3df0fdb81516eba1ba33f360162c`, and Redis `sha256:90e7a336d044f1abc9e9dbc05d65566850896d11453bbd1dd0fb7e5059f0e8fb`. Main was running on `127.0.0.1:5000`; PostgreSQL and Redis were healthy and had no host ports.

The enabled route probes returned expected authentication responses: Authentication antiforgery `500`, Requests `401`, Feed `401`, Users `401`, Parties `401`, and Platform `401`. Medical, Intelligence, Search, and Communications probes returned `404`. The `500` Authentication antiforgery result occurred only when probing the private HTTP upstream directly; Main correctly rejected the non-SSL request because secure antiforgery cookies are mandatory. The production/browser probe must traverse the TLS reverse proxy. This is an environmental prerequisite, not evidence of an excluded route.

The published image contains transitive compatibility assemblies from repository project references, but only the allowlisted modules/apps are registered and mapped when `Vitarish__EvaluatedConfiguration__Enabled=true`. Binary presence is not evaluated functionality; changing that composition flag or build profile changes the configuration identity.

No secret values, private keys, tokens, credentials, or production payloads belong in this document or its evidence.
