# Target of Evaluation and security-relevant asset inventory

Reviewed 2026-08-15 against the evaluated configuration in [`evaluated-configuration.md`](evaluated-configuration.md). This is an inventory, not a final SFR/SAR selection.

## TOE components

| Item | Path/artefact | Owner and responsibility | Deployment role | Evidence |
| --- | --- | --- | --- | --- |
| Main host | `src/Hosts/Main` -> `Vitarish.Main.dll` | Main composition owner; lifecycle, middleware, API exposure | One process/container | `Program.cs`, `Main.csproj`, `Dockerfile` |
| Authentication module | `src/Modules/Authentication` | Authentication owns identity and token-facing domain/application/API/infrastructure | In-process assembly | `src/Modules/Authentication/Module`; `ModuleBuildProfiles.props` |
| Requests module | `src/Modules/Requests` | Requests owns request transport and identity query boundary | In-process assembly | `src/Modules/Requests/Module`; `ModuleBuildProfiles.props` |
| Users module | `src/Modules/Users` | Users owns account, role, profile, WebAuthn, and PAT state | In-process assembly | `src/Modules/Users/Module`; `ModuleBuildProfiles.props` |
| Parties module | `src/Modules/Parties` | Parties owns owner, tenant, and tenant-membership state | In-process assembly | `src/Modules/Parties/Module`; `ModuleBuildProfiles.props` |
| Platform module | `src/Modules/Platform` | Platform owns platform administration and locale notification state | In-process assembly | `src/Modules/Platform/Module`; `ModuleBuildProfiles.props` |
| Feed module | `src/Modules/Feed` | Feed owns read-only feed aggregation and is required by Hosting composition | In-process assembly | `src/Modules/Feed/Module`; `ModuleBuildProfiles.props` |
| Persistence schema | PostgreSQL databases and EF migrations of enabled components | Each owning module/infrastructure; Main owns host audit/Hangfire wiring | PostgreSQL volume | module migration trees; `configurePostgres` |
| Key/cache state | Redis data-protection and distributed-cache entries | Main host data-protection/cache owner | Redis volume | `HostDataProtectionExtensions`, `HostRedisExtensions` |
| API contract | OpenAPI generated from Main | Endpoint/Contracts owners; generated output is derived | Release evidence and client boundary | Main OpenAPI build settings and generated spec path |
| Build/release | Docker image and source commit | Release/deployment operator | Immutable release inputs | Dockerfile, Git commit, package lock |

The Web React applications, agent-runtime, excluded modules, and provider services are repository assets but not TOE components for this configuration.

## Runtime and library versions

| Layer | Version/evidence |
| --- | --- |
| .NET target | `net10.0`, `src/Directory.Build.props` |
| .NET SDK at review | `10.0.302`, command output |
| .NET runtime in evaluated image | `10.0.11`, `docker exec ... dotnet --info` |
| ASP.NET/OpenAPI/EF/Redis packages | `10.0.9` family where declared in `src/Directory.Packages.props` and Main lock file |
| Npgsql EF provider | `10.0.2`; Npgsql `10.0.3` |
| Duende IdentityServer | `7.4.7` |
| Hangfire | `1.8.23`; PostgreSQL storage `1.21.1` |
| StackExchange.Redis | `3.0.7` |
| PostgreSQL environmental image | `postgres:18.0`, digest required in release record |
| Redis environmental image | `redis:7.4.5`, digest required in release record |

The package lock and resolved container image digests, rather than floating tags, are the final version evidence for a release.

## Security-relevant assets

| Asset category | Owner | CIA relevance | Protection location |
| --- | --- | --- | --- |
| Account credentials and credential metadata | Identity/Users | C high, I high, A medium | PostgreSQL; never recorded in evidence |
| Sessions, cookies, bearer tokens, antiforgery tokens | Main/Identity | C high, I high, A medium | Browser and protected transport; key material in Redis |
| Tenant membership, roles, claims, authorization context | Users/Parties/Identity | C high, I high, A high | PostgreSQL and signed/protected tokens |
| Core user, tenant, and platform data | Owning enabled module | C high, I high, A high | PostgreSQL volume and backups |
| Audit and Hangfire records | Audit/Main | C high, I high, A medium | PostgreSQL |
| Data-protection key ring and signing certificate material | Main/Identity | C high, I high, A high | Redis and deployment secret store |
| Database/Redis connection credentials | Operator/environment | C high, I high, A high | Secret store/environment only |
| Configuration and evaluated allowlist | Main/build/deployment owners | C medium, I high, A high | Image, compose file, deployment environment |
| Release artifacts, source commit, OpenAPI, image digests | Release operator | C medium, I high, A high | Release registry and evidence record |

## Security-relevant interfaces

| Interface | Direction and boundary | Input/output category | Validation/authority owner |
| --- | --- | --- | --- |
| Browser/API | Browser -> Main; external/untrusted boundary | HTTP requests, cookies, bearer tokens, JSON, multipart where enabled; structured responses/errors | API validates shape; Identity/Main validates auth, antiforgery, CORS, tenant context |
| Authentication/token | Browser/client <-> Identity/Main | Credentials, authorization code/token, claims, session cookies | Identity owns credential and token rules; Main owns middleware composition |
| Tenant/host resolution | Request -> Main/Finbuckle resolver -> Parties | Host/header/request context to tenant ID | Main resolver and Parties ownership checks |
| Persistence | Main/modules -> PostgreSQL | Parameterized SQL/EF commands and rows | Owning Infrastructure; migrations are authoritative schema changes |
| Cache/key ring | Main -> Redis | Cache entries, data-protection keys, certificate material | Main data-protection/cache owner; Redis is untrusted storage if compromised |
| Administration/audit | Operator/admin client -> protected Main endpoints -> audit store | Administrative commands and audit events | Owning Application authorization; Audit owner persists records |
| Deployment/configuration | Operator -> container runtime/Main | Image, profile, environment, secret references | Release operator and evaluated manifest |

## Threat agents and privileged roles

- Unauthenticated internet client: can send malformed, replayed, oversized, or cross-tenant requests.
- Authenticated but unauthorized tenant member: can attempt horizontal or vertical privilege escalation.
- Compromised tenant administrator: trusted for assigned tenant administration but not platform-wide authority.
- Platform administrator/operator: privileged to alter tenants, deployment inputs, secrets, and release lifecycle; controlled outside the application by host access and change review.
- Compromised PostgreSQL/Redis or host operator: environmental high-impact threat; TOE relies on network isolation, secret handling, backups, and host controls.

## Data flow and trust-boundary agreement

The browser sends requests to the single Main process. Main authenticates and resolves tenant context, invokes only the five enabled modules, and persists state to PostgreSQL. Main uses Redis for protected key/cache state. Operators supply the image/profile and secret references. There is no evaluated flow to AI, agent-runtime, medical/Tamin, communications, search, object storage, payment, social, or other provider services. See [`topology.md`](topology.md).

## Unknowns and ownership decisions

- Exact PostgreSQL/Redis image digests, database backup location, host OS patch level, TLS certificate issuance, and production network ACLs are deployment-record inputs, not repository facts; the release operator must record them.
- Browser implementation and reverse-proxy configuration are environmental because the evaluated artifact exposes Main directly on the private host binding.
- Existing repository projects retain excluded public contracts and dependencies. They are not silently reclassified as TOE components; any future inclusion requires a new configuration ID and review.
