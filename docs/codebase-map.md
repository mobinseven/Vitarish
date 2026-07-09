# Codebase map

Status: Active  
Owner: Platform Architecture  
Scope: Repository navigation  
Last reviewed: 2026-07-09

This map is a navigational index for future contributors and agents. It summarizes where things live and points to authoritative owners; it is not a substitute for `AGENTS.md`, architecture decisions, module docs, runbooks, or code.

## Repository overview

- **Purpose:** Vitarish is a modular healthcare/business platform with tenant-aware web applications, module APIs, shared contracts, infrastructure adapters, and documentation for journeys, architecture decisions, tests, and runbooks.
- **System type:** A .NET modular backend/Blazor host plus a Next.js React web workspace. The backend owns module composition, identity, persistence, OpenAPI generation, Hangfire jobs, and server-rendered Razor pages. The web workspace owns modern React applications and shared UI packages.
- **Main technologies:** .NET/ASP.NET Core, Razor Components/Blazor Server, EF Core, PostgreSQL/Npgsql, Duende IdentityServer, Hangfire, Finbuckle.MultiTenant, Redis integration, MediatR, FluentValidation, Next.js, React, TypeScript, HeroUI, TanStack Query, Orval, Vitest, Docker Compose, GitHub Actions.
- **Inside this repo:** Backend modules under `src/Modules`, app aggregators under `src/Apps`, runnable hosts under `src/Hosts`, identity/libraries under `src/Identity` and `src/Libraries`, React web apps/packages under `src/Hosts/Web`, tests under `tests`, architecture/development/product docs under `docs`, validation scripts under `tools` and `src/Hosts/Web/scripts`, and agent operating rules under `AGENTS.md` plus `.agents/skills`.
- **External systems or other repos:** Production databases, Redis, object/file storage providers, mail/SMS/communication gateways, FIDO/passkey authenticators, Tamin/prescription providers, OpenAI/Ollama/AI providers, Elasticsearch/search infrastructure, Google Speech, deployment infrastructure, secrets stores, and any live tenant/user data are external. Treat package registries and generated API clients as derived from configured dependencies/specs, not as business-rule sources.

## Folder structure

| Path | Contents | Edit guidance |
| --- | --- | --- |
| `.agents/skills` | Canonical project skills for Codex/agents. | Edit only for skill-governance work; keep bridge skills in sync. |
| `.claude/skills` | Claude bridge skills pointing back to `.agents/skills`. | Do not make canonical policy here. |
| `.github` | Issue templates and CI workflows. | High-care DevOps area. |
| `.mimocode` | MimoCode plans, schemas, scripts. | Edit only when maintaining that workflow. |
| `docs` | Architecture decisions, module/app docs, runbooks, indexes, this map. | Safe for documentation changes; update scoped docs near behavior changes. |
| `scripts` | Repository scripts. | High-care; check callers before changing. |
| `src/Apps` | App composition/aggregator projects and app-level contracts/clients/core/interface packages. | Add app-specific composition here when a module is exposed through an app. |
| `src/Build` | Build-related source. | High-care; affects multiple projects. |
| `src/Hosts/Main` | ASP.NET Core host, Blazor/Razor pages, auth, OpenAPI generation, Dockerfile, host settings. | High-risk runtime/composition root; avoid business rules in pages/host. |
| `src/Hosts/Web` | npm workspace for Next.js apps and shared React packages. | UI-safe when following web boundary rules; generated API output is not manual. |
| `src/Identity` | Identity client/application/domain/infrastructure/hosting code. | Security-sensitive; requires careful review. |
| `src/Libraries` | Shared server/client/contracts/generators such as Audit and Entities. | High reuse; avoid app-specific behavior. |
| `src/Modules` | Business modules with Domain/Application/Infrastructure/Api/Contracts/Client/Interface/Module slices. | Normal location for module behavior; preserve boundaries. |
| `tests` | .NET xUnit tests by module. | Safe for tests; copy module test conventions. |
| `tools` | Architecture/testing validators. | High-care; CI and policy enforcement may depend on these. |

**Generated code:** `src/Hosts/Web/apps/*/src/api/generated`, OpenAPI specs under `src/Hosts/Web/apps/*/src/api/specs` and `src/Hosts/Web/src/api/specs`, EF migration designer files and model snapshots under `src/**/Migrations`, `tsconfig.tsbuildinfo`, build output (`bin`, `obj`, `.next`, `node_modules`) when present. Do not manually edit generated clients, generated DTOs/hooks/query keys/MSW mocks, migration designer/model snapshot files except through the relevant generator/migration workflow.

**High-risk areas:** `AGENTS.md`, architecture ADRs/conventions/validators, identity/auth/security, host startup and middleware, module registration/composition, database migrations/snapshots, generated API boundaries, CI, package lock/version files, Docker/deployment config, and secrets/templates.

**Where new code usually goes:** Put domain rules in `src/Modules/<Module>/Domain`, use-case commands/queries and validation in `Application`, persistence/external adapters in `Infrastructure`, HTTP endpoints in `Api`, serialization DTOs in `Contracts`, generated/typed clients in `Client`, host composition in `Module` or `Interface`, Blazor pages in `src/Hosts/Main/Pages/<Area>`, Next pages in `src/Hosts/Web/apps/<app>/src/app`, React feature code in `src/Hosts/Web/apps/<app>/src/features/<feature>`, shared React primitives in `src/Hosts/Web/shared/web-ui`, and tests under the matching `tests/Modules/<Module>.Tests` or web `src/__tests__` area.

## Projects/modules

### Project slice responsibilities

Most business modules follow this pattern:

- `Domain`: business entities/value objects/policies/rules. No infrastructure, UI, transport, persistence, clocks, filesystem, network, or vendors.
- `Application`: commands, queries, orchestration, validation, ports, DTO mapping to contracts when owned by the use case. Depends on domain and abstractions.
- `Infrastructure`: EF Core `DbContext`, entity configurations, migrations, repositories/adapters, provider clients, external service implementations.
- `Api`: HTTP endpoints/routes and OpenAPI metadata. Thin transport layer only; no business rules.
- `Contracts`: serialization-only request/response DTOs and API contract shapes.
- `Client`: generated or hand-authored API client boundary, query keys/hooks where applicable. Do not import backend internals.
- `Interface`: module-to-host/frontend boundary packages and composition-facing services.
- `Module`: backend module registration, initialization, and composition.

### Major module/package owners

- **UI code:** Blazor/Razor UI lives in `src/Hosts/Main/Pages` and app/core components under `src/Apps/*/Core`; React UI lives in `src/Hosts/Web/apps/*/src/app`, `components`, `features`, and shared packages under `src/Hosts/Web/shared/*`.
- **Business logic:** `src/Modules/<Module>/Domain` first; orchestration in `Application`.
- **Database/infrastructure:** `src/Modules/<Module>/Infrastructure`, `src/Libraries/*/*.Server`, identity infrastructure, and host infrastructure where the dependency is truly host-owned.
- **Shared contracts/DTOs:** `src/Modules/<Module>/Contracts`, `src/Apps/*/Interface`, and client-facing generated TypeScript from OpenAPI.
- **Allowed dependencies:** Host/composition may depend on modules/interfaces; Api/Infrastructure depend inward on Application/Domain/Contracts; Application depends on Domain and ports/contracts; Domain depends on nothing platform-specific; React apps depend on generated clients, feature APIs, and shared UI packages.
- **Forbidden dependencies:** Domain/Application must not import UI, EF Core, ASP.NET transport, vendor SDKs, filesystem/network/clock implementation, or generated frontend clients. React/Client/Interface artifacts must not import backend Api/Application/Domain/Infrastructure/server-only packages. Contracts must not contain business behavior, persistence models, UI state, or backend internals.

## Runtime structure

- **Running services:** `src/Hosts/Main` is the ASP.NET Core backend/Blazor host. `src/Hosts/Web/apps/private-practice-general` and `src/Hosts/Web/apps/knowledge-qa-demo` are Next.js apps. Docker Compose runs `web` on port 3000 and `main` on port 5000 mapped to container port 8080.
- **Communication:** React apps call backend APIs using generated Orval clients from OpenAPI specs. Blazor pages/components call server-side module services directly through DI. The backend exposes API endpoints, Razor pages/components, IdentityServer, Hangfire dashboard, and development OpenAPI at `/openapi/{documentName}.json`.
- **Datastores/services:** PostgreSQL via EF Core/Npgsql is the primary database. Redis is configured for data protection/distributed caching where enabled. Hangfire uses PostgreSQL. External integrations include communication providers, file/object storage, search/Elasticsearch, AI/speech providers, passkey authenticators, and medical/prescription providers.
- **Authentication:** Host-owned identity is configured in `src/Hosts/Main` with Duende IdentityServer, ASP.NET authentication/authorization, session/cookie/antiforgery policy, and module endpoint security. React auth behavior belongs in app auth components/hooks and calls backend auth endpoints.
- **Frontend/backend/worker/API/infrastructure:** Next.js and Blazor/Razor are frontend surfaces; module `Api` projects are backend HTTP surfaces; Hangfire jobs and scheduled jobs are worker runtime; `Infrastructure` projects own persistence/external adapters; Docker/GitHub Actions are infrastructure/deployment support.

## Feature areas

Use module names as the feature map. For each module, the conventional locations are:

| Feature/module | UI | Commands/writes | Queries/reads | Domain | Persistence | Tests | Docs |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Authentication | `src/Hosts/Main/Pages/Accounts`, `src/Hosts/Web/apps/*/src/components/auth`, `features/auth` | `src/Modules/Authentication/Application` | same | `src/Modules/Authentication/Domain` | `Infrastructure` and `src/Identity` | `tests/Modules/Authentication.Tests` | `docs/platform/modules/authentication` |
| Journey/onboarding | React `features/journey`, host common/setup pages | `src/Modules/Journey/Application` | same | `src/Modules/Journey/Domain` | `Infrastructure` | `tests/Modules/Journey.Tests` | `docs/platform/modules/base/journeys` and app journeys |
| Platform/admin/tenants | `src/Hosts/Main/Pages/Platform`, React owner/admin areas | `src/Modules/Platform/Application` | same | `src/Modules/Platform/Domain` | `Infrastructure` | `tests/Modules/Platform.Tests` | `docs/platform/modules/platform` |
| Parties/tenants/owners | `src/Hosts/Main/Pages/Parties` | `src/Modules/Parties/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Parties.Tests` | `docs/platform/modules/parties` |
| Users | Account/profile UI and user features | `src/Modules/Users/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Users.Tests` | `docs/platform/modules/users` |
| Appointments | `src/Hosts/Main/Pages/Appointments`, clinic appointment pages | `src/Modules/Appointments/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Appointments.Tests` | `docs/platform/modules/appointments`, `docs/apps/clinic-owner/journeys` |
| Sea/clinic workflows | `src/Hosts/Main/Pages/Sea`, clinic Sea pages, React `features/sea` | `src/Modules/Sea/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Sea.Tests` | `docs/platform/modules/sea` |
| Medical/prescriptions | `src/Hosts/Main/Pages/Prescriptions`, `Pages/Support/Prescriptions` | `src/Modules/Medical/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Medical.Tests` | `docs/platform/modules/medical` |
| Knowledge/QA | `src/Hosts/Main/Pages/Knowledge`, `src/Hosts/Web/shared/knowledge-web`, app knowledge routes | `src/Modules/Knowledge/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Knowledge.Tests`, package tests | `docs/platform/modules/knowledge` |
| Search | platform search UI | `src/Modules/Search/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Search.Tests` | `docs/platform/modules/search` |
| Files, Contents, Communications, Commerce, Links, Feed, Requests, Social, Financial, Intelligence | Matching host/app pages when present | `src/Modules/<Name>/Application` | same | `Domain` | `Infrastructure` | Add/use `tests/Modules/<Name>.Tests` when present | `docs/platform/modules/<name>` when present |

## Common changes

- **New page:** Blazor: `src/Hosts/Main/Pages/<Area>` plus route constants in `src/Hosts/Main/Common/Routes` if needed. Next: `src/Hosts/Web/apps/<app>/src/app/<route>/page.tsx` and app navigation/shell files.
- **New component:** App-specific React component in app `components` or `features`; reusable primitives in `src/Hosts/Web/shared/web-ui`; Blazor component near the owning page/app core. Do not create design-system wrappers unless needed now.
- **New domain entity:** `src/Modules/<Module>/Domain`; add persistence mapping in Infrastructure only if it must be stored.
- **New command:** `src/Modules/<Module>/Application` under existing command/write convention; expose through Api only when needed.
- **New query:** `src/Modules/<Module>/Application` under existing query/read convention; keep it side-effect free.
- **Validation:** Domain invariants in Domain; use-case/input validation in Application; transport shape validation in Api; UI form validation in owning UI feature. Do not duplicate rules.
- **Database migration:** Generate EF Core migrations in `src/Modules/<Module>/Infrastructure/Migrations/<DbName>` or the owning server library/host migration assembly.
- **Translations/localization:** React app messages under `src/Hosts/Web/apps/<app>/src/localization`; Blazor/.NET localization under the owning host/module resources if present. Follow ADR-0007.
- **Tests:** .NET tests in `tests/Modules/<Module>.Tests`; React tests under app/shared package `src/__tests__`; architecture checks under `tools/architecture` or existing npm validators.
- **Pattern to copy:** Copy the nearest same-module/same-layer artifact first. For API-to-React work, follow `docs/runbooks/openapi-to-react-client.md` and ADR-0003/0005 conventions.

## Build/test/run

- **Backend run:** `dotnet run --project src/Hosts/Main/Main.csproj`.
- **Backend build:** `dotnet build src/Hosts/Main/Main.csproj` or build the specific module/test project touched.
- **Backend tests:** `dotnet test tests/Modules/<Module>.Tests/<Module>.Tests.csproj` for changed module tests.
- **Architecture validation:** `python3 tools/architecture/validate_interface_boundaries.py` when ADR-0003/interface/client boundaries are touched; app experience validators live under `tools/architecture` and web scripts.
- **Web install:** from `src/Hosts/Web`, run `npm install` when dependencies are missing or package files changed.
- **Web dev:** from `src/Hosts/Web`, `npm run dev:private-practice-general` or `npm run dev:knowledge-qa-demo`.
- **Web build/typecheck/test:** from `src/Hosts/Web`, use `npm run build`, `npm run typecheck`, `npm run test`, and targeted workspace scripts.
- **Web API generation:** app scripts run `generate:api` before dev/build; run it manually in the app workspace after OpenAPI changes.
- **Lint/format:** Existing npm `lint`, `typecheck`, localization/date/API boundary validators; .NET formatting is not configured as a global required command in this map.
- **Docker/dev containers:** `docker compose -f src/Hosts/docker-compose.web.yml up --build` runs web plus main.
- **Before committing:** Run the narrowest deterministic checks for changed files, plus required architecture/API/UI validators. For documentation-only changes, at minimum inspect the rendered/linked Markdown and verify git diff.

## Configuration

- **Environment variables:** Templates live in `src/Hosts/Main/appsettings.Template.env`; Next public config uses `NEXT_PUBLIC_*` variables, including Docker build args such as `NEXT_PUBLIC_API_BASE_URL`.
- **App settings:** `src/Hosts/Main/appsettings.Template.json` and host option binding in `src/Hosts/Main/Infrastructure/Options`.
- **Docker:** `src/Hosts/Main/Dockerfile`, `src/Hosts/Web/apps/*/Dockerfile`, and `src/Hosts/docker-compose.web.yml`.
- **CI/CD:** `.github/workflows`.
- **Package manager settings:** .NET central versions in `src/Directory.Packages.props` and `tests/Directory.Packages.props`; npm workspace and lockfile in `src/Hosts/Web/package.json` and `package-lock.json`; workspace package files under each app/shared package.
- **Public config:** Browser-exposed `NEXT_PUBLIC_*` values and non-secret defaults/templates.
- **Secrets:** Connection strings, Duende/identity credentials, signing keys, provider API keys, Redis/PostgreSQL passwords, mail/SMS credentials, Tamin/medical credentials, AI/search/cloud credentials, and tenant/user data must never be committed.

## Data/persistence

- **Database:** PostgreSQL via EF Core/Npgsql.
- **DbContexts/access:** Each module's `Infrastructure` project owns its database context and adapters; shared audit persistence is under `src/Libraries/Audit/Audit.Server`; identity persistence is under `src/Identity` and host identity setup.
- **Migrations:** `src/Modules/<Module>/Infrastructure/Migrations`, `src/Libraries/Audit/Audit.Server/Migrations`, and any owning infrastructure/server library migration folder.
- **Entity configurations:** Module `Infrastructure` projects near each `DbContext`.
- **SQL views/stored procedures/read models:** Keep in owning module infrastructure if present; do not put business rules in SQL without an explicit owning doc/decision.
- **Seed data:** Host/module initialization code and infrastructure seed routines where present; treat as production-impacting.
- **Dangerous data files:** Migrations, model snapshots, seed data, entity mappings, tenant/identity schema, audit schema, and date/time persistence conventions.

## API/contracts

- **Endpoints/routes/controllers:** `src/Modules/<Module>/Api` for module HTTP endpoints; host maps and secures them in `src/Hosts/Main`.
- **DTOs/contracts:** `src/Modules/<Module>/Contracts`; app/interface contracts under `src/Apps/*/Interface` when app-facing.
- **Generated clients:** `src/Hosts/Web/apps/*/src/api/generated`; do not edit manually.
- **OpenAPI specs:** Generated by `src/Hosts/Main/Main.csproj` into web API spec folders; development runtime exposes `/openapi/{documentName}.json`.
- **Versioning:** Contract compatibility is governed by module contract projects, OpenAPI endpoint conventions, generated client regeneration, and ADR-0003/0005 docs; add explicit versioning only through an architecture decision.
- **Do not expose:** Domain entities, persistence models, internal IDs/secrets, provider credentials, stack traces, authorization internals, tenant-crossing data, or business rules not meant for clients.

## Ownership/review

- **Source of truth:** This repo currently has no `CODEOWNERS` file. Ownership is documented by scoped docs: `AGENTS.md`, `docs/README.md`, module READMEs under `docs/platform/modules`, host docs under `docs/hosts`, and ADRs under `docs/architecture`.
- **Careful review:** Auth/identity/security, tenancy, migrations/data-loss, API contracts/generated clients, host startup/middleware, CI/Docker/deployment, package versions/locks, architecture validators/skills, and external integrations.
- **Avoid casual edits:** Generated clients/specs/snapshots, lockfiles, central package props, migration snapshots/designers, `AGENTS.md`, ADRs/conventions, host composition, secrets templates, and skill routing.
- **Review types:** Security review for auth/identity/secrets/tenant isolation; backend review for Domain/Application/Infrastructure/API/migrations; frontend review for React/Blazor UI/accessibility/localization; DevOps review for Docker/CI/package/deployment settings.

## Fragile areas

- **Likely breakpoints:** API boundary drift between OpenAPI, generated clients, and React wrappers; ADR-0003 interface violations; module feature flags in `Main.csproj`; auth/cookie/session/antiforgery ordering; tenant resolution; EF migrations/snapshots; date/time UTC persistence; localization/date input rules; Docker port/base URL mismatches; npm workspace dependency/lockfile drift.
- **Recurring cautions:** Do not edit generated clients by hand; regenerate from OpenAPI. Do not put business rules in endpoints, UI, migrations, DTOs, localization resources, or SQL. Keep command/write operations separate from query/read operations.
- **Package quirks:** Orval output is generated; HeroUI/React UI must follow web boundary/localization validators; EF model snapshots must match migrations; Duende/IdentityServer and passkey behavior is security-sensitive.
- **Before modifying risky areas:** Read `AGENTS.md`, relevant ADRs, module docs, runbooks, and validators; run targeted tests plus boundary validators; inspect generated diffs carefully.

## Documentation

- **Architecture:** `docs/architecture/decisions/_index.md`, `docs/architecture/conventions`, and `docs/architecture/validation`.
- **Agent/Codex rules:** `AGENTS.md` and `.agents/skills/*/SKILL.md`.
- **Durable Codex lessons:** No dedicated `codex-memory.md` is present in this repo; use scoped durable docs or create one only through explicit documentation governance.
- **Feature behavior:** `docs/platform/modules/<module>` for app-agnostic module behavior and `docs/apps/<app>` for app-specific journeys.
- **Architecture decisions:** `docs/architecture/decisions` for global ADRs and scoped `docs/**/decisions` for module/app decisions.
- **Docs to update after code changes:** Update the owning module/app journey, test plan/index, runbook, ADR/convention, and this map when the navigational shape changes.

## Maintenance

- **Update this map when:** Top-level folders change; a host/app/module/package is added/removed/renamed; generated-code locations move; build/test/run commands change; ownership/review boundaries change; external systems/config conventions change; risky areas or validators change.
- **Never include:** Secrets, live tenant/user data, private credentials, long design rationale, volatile implementation minutiae, speculative roadmap, or duplicate business rules.
- **This map vs `architecture.md`/ADRs:** This map answers “where is it and what owns it?” Architecture docs answer “what rule/decision governs it and why?”
- **This map vs `codex-memory.md`:** This map is stable navigation. Durable agent lessons would belong in a memory file or scoped docs if the repo adopts one.
- **Keep it short:** Prefer folder-level pointers, tables, and links to owning docs. If a section needs detailed behavior, move that detail to the owning module/app/runbook/ADR and link it here.
