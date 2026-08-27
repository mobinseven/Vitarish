# Tenant isolation evidence for #772

Status: tenant-isolation implementation and runtime proof complete for the
requested evaluated boundary below. The residuals at the end record provider
and inventory limits without weakening the tenant-isolation conclusion.

## Runtime fixture

The real Web host and real Main composition run together on one machine. The
E2E seeder creates two independent tenant indicators and tenant-owned data:

| Fixture | Host | Tenant ID | User | Patient | File |
| --- | --- | --- | --- | --- | --- |
| A | `localhost` | `9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92` | `e2e-owner` | `2d7a3eb9-1d47-4c63-9a45-19d7e6a0fb18` | `4b8d1e7a-9f24-4d65-a8b1-6c0e2f5d7a91` |
| B | `tenant-b.localhost` | `b2d2e4a1-2fd8-4781-8d9f-6c96f2a5e7b4` | `e2e-tenant-b-owner` | `f4c8109a-6f22-4a31-9ab8-48d7ad0d7e2f` | `9e2f6a41-7c35-4b8d-a0e6-1f5d9c2b7a84` |

The platform-admin fixture is `e2e-platform-admin`. Its cross-tenant access
is exercised separately from ordinary tenant access.

## Requirement matrix

| Requirement | Current evidence | Result |
| --- | --- | --- |
| Direct identifier substitution | Tenant A requests Tenant B patient, file, Parties tenant, user/profile, and Search tenant IDs. A gets 404/403 and no B body. Tenant B reads its own patient/file. The generated auth-web Users profile client is also exercised against real Main for A's foreign-user 403 and B's own-user 200. | Proven in real runtime |
| Alternate routes | Canonical and legacy patient assistant routes are denied for A's substituted B patient ID. File metadata and content routes both deny A's B file ID. | Proven in real runtime |
| Tenant host behavior | Host resolution unit tests cover two hosts, forged `X-Vitarish-Tenant-Host`, and trusted forwarded host. Runtime covers A's cookie on B origin and explicit tenant/forwarded-host headers. | Proven |
| Stale claims/cached authorization | B owner logs in, platform admin removes B's current role, and the already-authenticated B page is denied on the next patient request. Authorization reads current role state rather than a positive role cache. | Proven in real runtime |
| Search | A's public Search request containing B's tenant ID is denied; tenant-scoped patient search returns only the current tenant's patient. Search administration/index routes require the platform-admin policy. | Boundary and runtime proof |
| Error responses | Foreign patient/file identifiers produce non-success responses without the foreign patient's/file's identifying body. File content lookup checks tenant-filtered metadata before resolving object storage. | Proven for evaluated routes |
| Inference/assistant | A's canonical and legacy patient assistant requests, plus the Intelligence agent-conversation patient and practice transports, are denied for B identifiers. The anonymous internal agent callback and malformed credential are both 401 before callback execution. | Boundary proof; provider inference residual below |
| Platform-admin operations | Platform admin reads B's Parties tenant and patient, then switches to A's host and reads A's patient. Ordinary tenant users receive 403 for global Parties and Users management routes. | Proven in real runtime |
| Export/download | The protected Files content-download operation is the evaluated export-like path: A downloads its own content (200), A's B substitution is 404 with no B content, and B downloads its own content (200). No separate bulk export route exists in the evaluated inventory. | Download/export path proven; bulk export not applicable |

## Code and test owners

- Tenant resolution and host trust: `TenantResolver` plus
  `tests/Modules/Parties.Tests/Infrastructure/TenantResolverTests.cs`.
- Current authorization and role revocation: `Authorizer`,
  `IdentityDefinitions`, and the protected endpoint-group policy.
- Resource ownership: module/application query filters and the evaluated
  Clinic, Files, Parties, Search, and Users routes.
- E2E composition and fixture identity:
  `src/Hosts/E2e/Program.cs` and
  `src/Hosts/E2e/Infrastructure/Fixtures/E2eFixtureSeeder.cs`; the test-only
  object store is `src/Hosts/E2e/Infrastructure/Storage/E2eObjectStorageService.cs`.
- Runtime assertions: `src/Hosts/Web/tests/two-host/authenticated-boundary.spec.ts`.
- Route-policy inventory: `docs/security/authorization-matrix.md` and the
  OpenAPI endpoint validator.

## Validation record

Executed against the current worktree, based at `2a300a46dbd079b5422e05ed422ee9bb64f8d219`:

- `dotnet build src/Modules/Users/Module/Users.Module.csproj --no-restore --no-incremental` — passed.
- `dotnet build src/Modules/Parties/Module/Parties.Module.csproj --no-restore --no-incremental` — passed.
- `dotnet build src/Modules/Search/Module/Search.Module.csproj --no-restore --no-incremental` — passed.
- `dotnet build src/Modules/Files/Module/Files.Module.csproj --no-restore --no-incremental` — passed; existing warnings only.
- `dotnet restore src/Hosts/E2e/E2e.csproj --disable-parallel --verbosity:minimal` — passed.
- `dotnet build src/Hosts/E2e/E2e.csproj --no-restore --no-incremental` — passed; existing warnings only.
- `dotnet test tests/Modules/Parties.Tests/Parties.Tests.csproj --no-restore --filter FullyQualifiedName~TenantResolverTests` — 3/3 passed.
- `dotnet test tests/Modules/Search.Tests/Search.Tests.csproj --no-restore` — 12/12 passed.
- `dotnet run --project tools/architecture -- openapi-endpoints` — `Violations found: 0`.
- `npm run test:e2e:two-host` from `src/Hosts/Web` — 8/8 passed in the real two-host composition, including generated auth-web Users profile assertions, tenant-filtered file search, binary download/export behavior, and Intelligence agent-conversation substitutions; the fresh authenticated dashboard proof screenshot was visually inspected at `src/Hosts/Web/artifacts/two-host-e2e/authenticated-dashboard-proof.png`.

## Residual risks and boundaries

- The E2E host uses in-memory persistence and object storage, with a disabled
  semantic search backend. Tenant and authorization boundaries are exercised,
  but a live provider-backed inference response and live search index are not
  proven here.
- Startup logs still contain pre-existing post-commit audit persistence errors
  caused by an audit tenant mismatch in the in-memory E2E composition. They do
  not make the HTTP assertions pass silently and remain an operational defect
  to repair separately.
- Generated output was not edited. The generated auth-web Users profile
  function was invoked against real Main through the E2E test transport; other
  generated clients were not separately exercised against a live server.
- This evidence covers the evaluated core platform boundary. Medical, AI,
  search, commerce, and provider workflows outside those routes remain outside
  this issue's accepted runtime scope unless their route is explicitly added
  to the matrix.
