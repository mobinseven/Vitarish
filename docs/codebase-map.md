# Codebase map

Status: Active  
Owner: Platform Architecture  
Scope: Repository navigation  
Last reviewed: 2026-07-31

## 1. Purpose

Use this map to find the current owner and location of code, generated
artifacts, tests, commands, and supporting documentation. It is not a source of
business behavior or architecture policy. Those rules belong to code,
`AGENTS.md`, ADRs, module documentation, and runbooks.

Update this file when repository navigation changes. Keep detailed behavior and
design rationale in their authoritative documents.

## 2. Current architecture at a glance

Vitarish consists of:

- a modular .NET backend under `src/Modules`, `src/Apps`, `src/Identity`, and
  `src/Libraries`;
- a reusable backend composition library at `src/Libraries/Hosting`;
- a production-only ASP.NET Core host at `src/Hosts/Main`;
- a non-publishable in-memory browser-test host at `src/Hosts/E2e`;
- a React/Next.js Web workspace at `src/Hosts/Web`;
- a server-only Node agent runtime at
  `src/Hosts/Web/apps/agent-runtime`;
- .NET and Web tests under `tests` and package-local test folders;
- architecture and test validators under `tools` and
  `src/Hosts/Web/scripts`.

React Web is the production UI surface.

`src/Libraries/Hosting` owns shared registration, middleware, and lifecycle
composition. `src/Hosts/Main` owns production providers and operations:
PostgreSQL, Redis, certificates, Hangfire, OpenAPI, migrations, and
initialization. `src/Hosts/E2e` supplies isolated in-memory providers and
deterministic fixtures for the two-host Playwright suite.

The main technology families are ASP.NET Core Minimal APIs, EF Core with
PostgreSQL/Npgsql, Duende IdentityServer, Hangfire, Finbuckle.MultiTenant,
Redis, MediatR, FluentValidation, Next.js, React, TypeScript, HeroUI,
TanStack Query, Orval, Vitest, Playwright, Docker Compose, and GitHub Actions.

## 3. Top-level map

| Path | Current responsibility | Guidance |
| --- | --- | --- |
| `.agents/skills` | Canonical project skills. | Follow `.agents/AGENTS.md`; keep skills procedural and scoped. |
| `.claude/skills` | Discovery bridges to canonical skills. | Do not place canonical rules here. |
| `.github` | CI workflows and repository automation. | Check workflow callers before editing. |
| `docs` | ADRs, conventions, app/module documentation, runbooks, indexes, and this map. | Follow `docs/AGENTS.md`. |
| `src/Apps` | .NET app aggregators and app-owned backend slices. Current roots are `Aggregator`, `Base`, `Clinic`, and `ProjectManagement`. | Follow `src/Apps/AGENTS.md`; keep module business rules in modules. |
| `src/Build` | Shared MSBuild configuration and module build profiles. | High-impact shared build surface. |
| `src/Libraries/Hosting` | Shared backend registration and middleware composition used by executable hosts. | Keep provider and operational choices in executable hosts. |
| `src/Hosts/Main` | Production ASP.NET Core runtime host. | Follow its `AGENTS.md`; no E2E branches or product UI. |
| `src/Hosts/E2e` | Non-publishable in-memory host for real two-host browser tests. | No migrations, Hangfire, or test authentication bypass. |
| `src/Hosts/Web` | npm workspace containing React apps, feature packages, Web infrastructure, and the agent runtime. | Follow its `AGENTS.md`; never edit generated API output manually. |
| `src/Identity` | Identity Domain/Application/Infrastructure/Hosting/Client code. | Security-sensitive; follow its `AGENTS.md`. |
| `src/Libraries` | Shared .NET libraries and generators, including Audit and Entities. | Keep app- and module-specific behavior out. |
| `src/Modules` | Modular backend capabilities. | Follow `src/Modules/AGENTS.md` and preserve inward dependency direction. |
| `tests/Apps` | App-level .NET tests. | Test app-owned behavior and composition boundaries. |
| `tests/Modules` | Module-level .NET tests. | Test the layer that owns the behavior. |
| `tools/architecture` | Repository architecture and documentation validators. | Follow `tools/AGENTS.md`. |
| `tools/testing` | Cross-cutting hot-test and test-synchronization tools. | Follow `tools/AGENTS.md`. |
| `Vitarish.slnx` | Current .NET solution entry point. | Use for full-solution validation. |

Build output and local tooling folders such as `bin`, `obj`, `.next`,
`node_modules`, `.vs`, and `artifacts` are not source locations.

## 4. .NET backend layout

Business modules commonly use these slices. Not every module needs every
slice.

| Slice | Owner and responsibility |
| --- | --- |
| `Domain` | Business state, invariants, entities, value objects, and pure policies. |
| `Application` | Commands, queries, workflows, validation coordination, and application-owned ports. |
| `Contracts` | Serialization-safe public transport shapes and route values. |
| `Api` | Minimal API endpoint registration and transport-to-application mapping. |
| `Infrastructure` | EF Core persistence, migrations, provider adapters, caches, files, background jobs, and concrete options. |
| `Module` | Explicit module registration and composition exposed to hosts. |
| `Client` | .NET HTTP client boundary for current .NET consumers. |

Dependency direction:

- Domain is independent of Application and all outward implementations.
- Application depends on Domain and Application-owned abstractions.
- API and Infrastructure adapt inward-facing contracts.
- composition roots depend on the projects they wire.
- Contracts remain independent of Domain, Application, persistence, UI, and
  vendor models.
- Client projects do not import server internals.

EF Core belongs in persistence infrastructure and migration tooling. It must
not leak through Domain/Application contracts as `DbContext`, `IQueryable<T>`,
entities, proxies, tracking state, or provider types.

For statically analyzable composition, dependency selection, generated
serialization, trimming, and Native AOT readiness, follow
[ADR-0010](architecture/decisions/0010-prefer-statically-analyzable-native-aot-compatible-design.md)
and its
[engineering standard](architecture/conventions/adr-0010-static-analyzability-native-aot.md).
Native AOT is a readiness constraint, not a requirement to publish the current
production host as Native AOT.

## 5. React Web layout

Run npm workspace commands from `src/Hosts/Web`.

### Apps

| Path | Responsibility |
| --- | --- |
| `apps/private-practice-owner` | Private-practice owner application. |
| `apps/patients` | Patient-facing application. |
| `apps/knowledge-qa-demo` | Knowledge QA demonstration application. |
| `apps/vitarish-landing` | Public landing application. |
| `apps/vitarish-pm` | Dedicated tenant-scoped project-management application backed by SEA. |
| `apps/agent-runtime` | Server-only OpenAI Agents SDK runtime; not a browser UI. |

### Shared packages

| Path | Responsibility |
| --- | --- |
| `shared/account` | Reusable account/profile Web feature surface. |
| `shared/auth` | Authentication Web feature and generated API boundary. |
| `shared/clinic` | Clinic/private-practice feature surface and generated clinic API boundary. |
| `shared/communications` | Communications feature and generated API boundary. |
| `shared/contents` | Content feature surface. |
| `shared/intelligence` | Intelligence feature, generated API boundary, and browser realtime/dictation behavior. |
| `shared/journey` | Journey/onboarding feature and generated API boundary. |
| `shared/knowledge` | Knowledge feature surface. |
| `shared/platform` | Platform administration feature surface. |
| `shared/sea` | SEA feature and generated API boundary. |
| `shared/api-client` | Shared browser transport: base URL, credentials, tenant header, CSRF, cancellation, and transport errors. |
| `shared/localization` | Locale state, providers, formatting, and feature namespace registration. |
| `shared/ui` | Generic Web presentation, layout primitives, and design tokens. |

App `src/app/**/page.tsx` files own route entry points and compose exported
feature surfaces. Reusable workflow UI, hooks, generated clients, and
feature-local localization belong in the owning `shared/<feature>-web`
package. App-owned route shells, home/landing composition, and app navigation
stay in the app.

Use raw HeroUI/HeroUI Pro primitives by default. Add generic Web presentation
to `shared/ui` only when it satisfies the promotion rules in
[ADR-0008](architecture/decisions/0008-standardize-app-experience-boundaries.md).

`src/Modules/Intelligence/Agents` contains the provider-neutral profile, capability-pack, composition, manifest, and compatibility framework defined by ADR-0012. Explicit app-owned profile, pack, and composition artifacts live under paths such as `src/Apps/Clinic/Agents`; they may reference their owning Application and Intelligence.Agents, while API and Infrastructure continue to consume application-owned agent contracts through Clinic.Application.

## 6. Capability map

| Capability | Backend owner | Web owner when present | Tests | Documentation |
| --- | --- | --- | --- | --- |
| Authentication | `src/Modules/Authentication`, `src/Identity` | `shared/auth` | `tests/Modules/Authentication.Tests` | `docs/platform/modules/authentication` |
| Journey/onboarding | `src/Modules/Journey` | `shared/journey` | `tests/Modules/Journey.Tests` | `docs/platform/modules/base/journeys` |
| Clinic/private practice | `src/Apps/Clinic`, relevant modules | `apps/private-practice-owner`, `shared/clinic` | `tests/Apps/Clinic.*` and owning module tests | `docs/apps/private-practice-general` and module docs |
| Patient experience | relevant modules and `src/Apps/Clinic` | `apps/patients`, feature packages | owning app/module tests | current app journeys under `docs/apps` |
| Platform/tenants | `src/Modules/Platform`, `Parties`, `Users` | `shared/platform`, owner app routes | matching `tests/Modules/*.Tests` | matching `docs/platform/modules/*` |
| Appointments | `src/Modules/Appointments` | clinic feature/routes | `tests/Modules/Appointments.Tests` | `docs/platform/modules/appointments` |
| SEA | `src/Modules/Sea` | `shared/sea`, `shared/clinic` | `tests/Modules/Sea.Tests` | `docs/platform/modules/sea` |
| Medical/prescriptions | `src/Modules/Medical` | clinic feature/routes | `tests/Modules/Medical.Tests` | `docs/platform/modules/medical` |
| Knowledge | `src/Modules/Knowledge` | `shared/knowledge`, `apps/knowledge-qa-demo` | `tests/Modules/Knowledge.Tests` and package tests | `docs/platform/modules/knowledge` |
| Intelligence | `src/Modules/Intelligence` | `shared/intelligence`; server runtime in `apps/agent-runtime` | `tests/Modules/Intelligence.Tests` and package tests | `docs/platform/modules/intelligence` |
| Communications | `src/Modules/Communications` | `shared/communications` | package and owning module tests | `docs/platform/modules/communications` |
| Contents | `src/Modules/Contents` | `shared/contents` | `tests/Modules/Contents.Tests` | `docs/platform/modules/contents` when present |
| Files | `src/Modules/Files` | owning feature routes/packages | `tests/Modules/Files.Tests` | `docs/platform/modules/files` |
| Search | `src/Modules/Search` | owning feature routes/packages | `tests/Modules/Search.Tests` | `docs/platform/modules/search` |
| Other modules | matching `src/Modules/<Name>` | owning app/feature package when implemented | matching test project when present | matching module docs when present |

## 7. Runtime and data flow

- `src/Hosts/Main` exposes module Minimal APIs, authentication/authorization,
  IdentityServer, Hangfire, and development OpenAPI documents at
  `/openapi/{documentName}.json`.
- React feature packages call backend APIs through generated Orval clients and
  `shared/api-client`.
- Main writes generated OpenAPI documents to
  `src/Hosts/Web/src/api/specs` during configured builds.
- Feature-owned OpenAPI inputs and generated TypeScript output live under
  `src/Hosts/Web/shared/<feature>-web/src/api/specs` and
  `src/Hosts/Web/shared/<feature>-web/src/api/generated` where present.
- `apps/agent-runtime` communicates with Main through the configured internal
  Intelligence endpoints and shared-secret boundary.
- PostgreSQL/Npgsql is the primary persistence stack. Hangfire uses PostgreSQL.
  Redis supports host caching, data protection, and identity-related runtime
  concerns where configured.
- External adapters include communication, object/file storage,
  search/Elasticsearch, AI/speech, passkey, and medical/prescription providers.

`src/Hosts/docker-compose.web.yml` describes `web` on port 3000, `main` on
host port 5000/container port 8080, and `agent-runtime` on port 3003. Inspect
the compose build paths before relying on it; the repository currently has no
`src/Hosts/Web/Dockerfile`.

## 8. Common change guide

- **New Web route:** add
  `src/Hosts/Web/apps/<app>/src/app/<route>/page.tsx`, then update the owning
  route constants/navigation/intent registries and run the route audit.
- **New Web workflow UI:** use
  `src/Hosts/Web/shared/<feature>-web`; keep the app route thin.
- **New generic Web primitive:** prefer HeroUI/HeroUI Pro directly; use
  `shared/ui` only for genuinely generic presentation.
- **New domain behavior:** use `src/Modules/<Module>/Domain`.
- **New command/query/workflow:** use the owning module or app `Application`
  slice. Queries must remain side-effect free.
- **New HTTP endpoint:** change Contracts/Application/API at their owners,
  keep endpoints thin, update authoritative OpenAPI metadata, then regenerate
  clients.
- **New external provider:** implement an Infrastructure adapter for an
  Application-owned port and register it at composition.
- **Database change:** update the owning persisted model/configuration and
  generate the migration under the owning Infrastructure migration tree.
- **Localization:** generic locale/formatting belongs in
  `shared/localization`; feature messages belong in the feature package;
  app route/shell copy stays in the app.
- **Tests:** use `tests/Modules/<Module>.Tests`,
  `tests/Apps/<App>.Tests`, or package-local Web tests according to the
  behavior owner.

For API-to-React work, follow
[`docs/runbooks/openapi-to-react-client.md`](runbooks/openapi-to-react-client.md).
For route topology, outer route layout, and scroll ownership, follow the scoped
[`src/Hosts/Web/AGENTS.md`](../src/Hosts/Web/AGENTS.md) rules.

## 9. Generated and high-care areas

Generated or derived artifacts include:

- `src/Hosts/Web/src/api/specs/Main*.json`;
- feature package `src/api/generated` trees;
- feature package `src/api/specs` copies derived from authoritative OpenAPI;
- EF migration designer files and model snapshots;
- compiler/framework output such as `bin`, `obj`, `.next`, and
  `tsconfig.tsbuildinfo`.

Never repair generated clients, DTOs, hooks, query keys, mocks, migration
designer output, or snapshots manually. Change the authoritative input and run
the canonical generator.

High-care areas include:

- root and scoped `AGENTS.md`, `.agents/skills`, ADRs, and architecture
  validators;
- `src/Build/ModuleBuildProfiles.props` and `src/Hosts/Main/Main.csproj`;
- Main startup, middleware order, auth, tenant resolution, data protection,
  Hangfire, and OpenAPI composition;
- persistence models, migrations, concurrency, and UTC time handling;
- generated API boundaries, package locks, Docker/configuration templates, CI,
  and secrets;
- Web transport ownership, CSRF/antiforgery behavior, localization, app/feature
  import boundaries, and route/layout registries;
- Native AOT/trimming compatibility, dynamic runtime dependencies, and warning
  suppressions governed by ADR-0010.

## 10. Build, test, and run pointers

### .NET

```powershell
dotnet run --project src/Hosts/Main/Main.csproj
dotnet build src/Hosts/E2e/E2e.csproj --no-restore
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=All
dotnet build Vitarish.slnx
dotnet test tests/Modules/<Module>.Tests/<Module>.Tests.csproj
```

Focused `ModuleBuildProfile` builds are inner-loop checks only. Read
[`docs/development/focused-builds.md`](development/focused-builds.md) for the
profile source of truth and required merge-level validation.

### Web

From `src/Hosts/Web`:

```powershell
npm install
npm run dev:private-practice-owner
npm run dev:patients
npm run dev:knowledge-qa-demo
npm run dev:vitarish-landing
npm run dev:vitarish-pm
npm run dev:agent-runtime
npm run build
npm run build:vitarish-pm
npm run typecheck
npm run test
npm run validate:api-boundary
npm run validate:localization
npm run validate:web-ui-boundary
npm run validate:app-boundaries
npm run lint:layout
npm run test:layout-lint
```

Use the owning package's `generate:api` script after an OpenAPI change. Do not
assume every workspace exposes every root script; inspect its `package.json`.

For documentation-only changes, verify paths/links, scan for stale names, and
run `git diff --check`. Builds are not required unless the documentation change
also modifies executable or generated behavior.

## 11. Documentation pointers

- Repository operating contract: [`AGENTS.md`](../AGENTS.md)
- Architecture boundaries: [`docs/architecture.md`](architecture.md)
- Architecture decisions: [`docs/architecture/decisions/_index.md`](architecture/decisions/_index.md)
- Documentation index: [`docs/README.md`](README.md)
- Platform/module docs: [`docs/platform`](platform)
- App experience docs: [`docs/apps`](apps)
- Host docs and runbooks: [`docs/hosts`](hosts)
- Cross-cutting runbooks: [`docs/runbooks`](runbooks)
- Journey and test indexes: [`docs/indexes`](indexes)
- Durable repository lessons: [`docs/memory.md`](memory.md)

## 12. Maintenance rules

- Keep this document current with real folder names, runtime roles, generated
  paths, and canonical commands.
- Do not duplicate business rules, security policy, provider contracts, or
  implementation detail owned elsewhere.
- Record only current repository locations and responsibilities.
- Prefer folder-level pointers and links. Move detailed procedures to runbooks
  or skills and detailed rationale to ADRs.
