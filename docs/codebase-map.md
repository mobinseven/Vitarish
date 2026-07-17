# Codebase map

Status: Active  
Owner: Platform Architecture  
Scope: Repository navigation  
Last reviewed: 2026-07-10

## 1. Purpose and scope

Read this file first when asked to locate, add, or move code in this repo; do not treat it as a source of behavior or policy rules.

This map is a short navigation guide for contributors and agents. It answers where major code, documentation, generated artifacts, runtime pieces, and maintenance rules live. It is not the source of truth for architecture policy, business behavior, product scope, or operational procedures; use the owning code, `AGENTS.md`, ADRs, module docs, and runbooks for those details.

Update this file only when repository navigation changes. Keep it folder-level and link/pointer oriented.

## 2. Repository overview

- **Purpose:** Vitarish is a modular healthcare/business platform with tenant-aware web applications, module APIs, shared contracts, infrastructure adapters, and documentation for journeys, architecture decisions, tests, and runbooks.
- **System type:** A .NET modular backend/Blazor host plus a Next.js React web workspace. The backend owns module composition, identity, persistence, OpenAPI generation, Hangfire jobs, and server-rendered Razor pages. The web workspace owns modern React applications, feature packages, generic UI packages, API transport, and localization composition.
- **Main technologies:** .NET/ASP.NET Core, Razor Components/Blazor Server, EF Core, PostgreSQL/Npgsql, Duende IdentityServer, Hangfire, Finbuckle.MultiTenant, Redis integration, MediatR, FluentValidation, Next.js, React, TypeScript, HeroUI, TanStack Query, Orval, Vitest, Docker Compose, and GitHub Actions.
- **Inside this repo:** Backend modules under `src/Modules`, app aggregators under `src/Apps`, runnable hosts under `src/Hosts`, identity/libraries under `src/Identity` and `src/Libraries`, React web apps/packages under `src/Hosts/Web`, tests under `tests`, architecture/development/product docs under `docs`, validation scripts under `tools` and `src/Hosts/Web/scripts`, and agent operating rules under `AGENTS.md` plus `.agents/skills`.
- **External systems:** Production databases, Redis, object/file storage providers, mail/SMS/communication gateways, FIDO/passkey authenticators, Tamin/prescription providers, OpenAI/Ollama/AI providers, Elasticsearch/search infrastructure, Google Speech, deployment infrastructure, secrets stores, and live tenant/user data are external.

## 3. Top-level folder map

| Path | Contents | Edit guidance |
| --- | --- | --- |
| `.agents/skills` | Canonical project skills for Codex/agents. | Edit only for skill-governance work; keep bridge skills in sync. |
| `.claude/skills` | Claude bridge skills pointing back to `.agents/skills`. | Do not make canonical policy here. |
| `.github` | Issue templates and CI workflows. | High-care DevOps area. |
| `docs` | Architecture decisions, module/app docs, runbooks, indexes, this map. | Safe for documentation changes; update scoped docs near behavior changes. |
| `scripts` | Repository scripts. | High-care; check callers before changing. |
| `src/Apps` | App composition/aggregator projects and app-level contracts/clients/core/interface packages. | Add app-specific composition here when a module is exposed through an app. |
| `src/Build` | Build-related source. | High-care; affects multiple projects. |
| `src/Hosts/Main` | ASP.NET Core host, Blazor/Razor pages, auth, OpenAPI generation, Dockerfile, host settings. | High-risk runtime/composition root; avoid business rules in pages/host. |
| `src/Hosts/Web` | npm workspace for Next.js apps and shared React packages, including generic UI, feature packages, Web API transport, and localization composition. | UI-safe when following web boundary rules; generated API output is not manual. |
| `src/Identity` | Identity client/application/domain/infrastructure/hosting code. | Security-sensitive; requires careful review. |
| `src/Libraries` | Shared server/client/contracts/generators such as Audit and Entities. | High reuse; avoid app-specific behavior. |
| `src/Modules` | Business modules with Domain/Application/Infrastructure/Api/Contracts/Client/Interface/Module slices. | Normal location for module behavior; preserve boundaries. |
| `tests` | .NET xUnit tests by module. | Safe for tests; copy module test conventions. |
| `tools` | Architecture/testing validators. | High-care; CI and policy enforcement may depend on these. |

## 4. Project/module layout

Most business modules follow this slice layout:

| Slice | Responsibility |
| --- | --- |
| `Domain` | Business entities, value objects, policies, and rules. No infrastructure, UI, transport, persistence, clocks, filesystem, network, or vendors. |
| `Application` | Commands, queries, orchestration, validation, ports, and use-case-owned mapping. Depends on domain and abstractions. |
| `Infrastructure` | EF Core `DbContext`, entity configurations, migrations, repositories/adapters, provider clients, and external service implementations. |
| `Api` | HTTP endpoints/routes and OpenAPI metadata. Thin transport layer only; no business rules. |
| `Contracts` | Serialization-only request/response DTOs and API contract shapes. |
| `Client` | Generated or hand-authored API client boundary, query keys/hooks where applicable. Do not import backend internals. |
| `Interface` | Module-to-host/frontend boundary packages and composition-facing services. |
| `Module` | Backend module registration, initialization, and composition. |

Ownership and dependency rules:

- UI code lives in `src/Hosts/Web/apps/*/src/app` and shared React packages under `src/Hosts/Web/shared/*`. React Web app route files compose package surfaces; workflow feature implementation belongs in `src/Hosts/Web/shared/<feature>-web` under ADR-0008. App-local workflow folders are forbidden; `home` and `landing` remain app-owned route/shell composition unless reusable workflow behavior is extracted.
- Business logic lives in `src/Modules/<Module>/Domain`; orchestration lives in `Application`.
- Database and external infrastructure live in `src/Modules/<Module>/Infrastructure`, `src/Libraries/*/*.Server`, identity infrastructure, or truly host-owned infrastructure.
- Shared contracts/DTOs live in `src/Modules/<Module>/Contracts`, `src/Apps/*/Interface`, and generated TypeScript clients derived from OpenAPI.
- Allowed direction is host/composition toward modules/interfaces; Api/Infrastructure inward to Application/Domain/Contracts; Application to Domain and ports/contracts; Domain to no platform-specific implementation; React apps to generated clients, feature APIs, and shared UI packages.
- Forbidden direction: Domain/Application must not import UI, EF Core, ASP.NET transport, vendor SDKs, filesystem/network/clock implementations, or frontend clients. React/Client/Interface artifacts must not import backend Api/Application/Domain/Infrastructure/server-only packages.

### Feature/module map

Use module names as the feature map. For each module, the conventional locations are:

| Feature/module | UI | Commands/writes | Queries/reads | Domain | Persistence | Tests | Docs |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Authentication | React routes and `src/Hosts/Web/shared/auth-web` | `src/Modules/Authentication/Application` | same | `src/Modules/Authentication/Domain` | `Infrastructure` and `src/Identity` | `tests/Modules/Authentication.Tests`, package tests | `docs/platform/modules/authentication` |
| Journey/onboarding | React routes and `src/Hosts/Web/shared/journey-web` | `src/Modules/Journey/Application` | same | `src/Modules/Journey/Domain` | `Infrastructure` | `tests/Modules/Journey.Tests`, package tests | `docs/platform/modules/base/journeys` and app journeys |
| Platform/admin/tenants | `src/Hosts/Main/Pages/Platform`, React owner/admin areas | `src/Modules/Platform/Application` | same | `src/Modules/Platform/Domain` | `Infrastructure` | `tests/Modules/Platform.Tests` | `docs/platform/modules/platform` |
| Parties/tenants/owners | `src/Hosts/Main/Pages/Parties` | `src/Modules/Parties/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Parties.Tests` | `docs/platform/modules/parties` |
| Users | Account/profile routes and `src/Hosts/Web/shared/account-web` | `src/Modules/Users/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Users.Tests`, package tests | `docs/platform/modules/users` |
| Appointments | `src/Hosts/Main/Pages/Appointments`, clinic appointment pages | `src/Modules/Appointments/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Appointments.Tests` | `docs/platform/modules/appointments`, `docs/apps/clinic-owner/journeys` |
| Sea/clinic workflows | React routes with `src/Hosts/Web/shared/sea-web` and `src/Hosts/Web/shared/clinic-web` | `src/Modules/Sea/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Sea.Tests`, package tests | `docs/platform/modules/sea` |
| Medical/prescriptions | `src/Hosts/Main/Pages/Prescriptions`, `Pages/Support/Prescriptions` | `src/Modules/Medical/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Medical.Tests` | `docs/platform/modules/medical` |
| Knowledge/QA | `src/Hosts/Main/Pages/Knowledge`, `src/Hosts/Web/shared/knowledge-web`, app knowledge routes | `src/Modules/Knowledge/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Knowledge.Tests`, package tests | `docs/platform/modules/knowledge` |
| Intelligence | `src/Hosts/Web/shared/intelligence-web` owns Intelligence OpenAPI generation, browser realtime dictation, and generic localized primitives | `src/Modules/Intelligence/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Intelligence.Tests`, package tests | `docs/platform/modules/intelligence` when present |
| Search | platform search UI | `src/Modules/Search/Application` | same | `Domain` | `Infrastructure` | `tests/Modules/Search.Tests` | `docs/platform/modules/search` |
| Files, Contents, Communications, Commerce, Links, Feed, Requests, Social, Financial | Matching host/app pages when present | `src/Modules/<Name>/Application` | same | `Domain` | `Infrastructure` | Add/use `tests/Modules/<Name>.Tests` when present | `docs/platform/modules/<name>` when present |

## 5. Runtime map

- **Running services:** `src/Hosts/Main` is the ASP.NET Core backend/Blazor host. `src/Hosts/Web/apps/private-practice-general` and `src/Hosts/Web/apps/knowledge-qa-demo` are Next.js apps. Docker Compose runs `web` on port 3000 and `main` on port 5000 mapped to container port 8080.
- **Communication:** React apps call backend APIs using generated Orval clients from OpenAPI specs. Blazor pages/components call server-side module services directly through DI. The backend exposes API endpoints, Razor pages/components, IdentityServer, Hangfire dashboard, and development OpenAPI at `/openapi/{documentName}.json`.
- **Datastores/services:** PostgreSQL via EF Core/Npgsql is the primary database. Redis is configured for data protection/distributed caching where enabled. Hangfire uses PostgreSQL. External integrations include communication providers, file/object storage, search/Elasticsearch, AI/speech providers, passkey authenticators, and medical/prescription providers.
- **Authentication:** Host-owned identity is configured in `src/Hosts/Main` with Duende IdentityServer, ASP.NET authentication/authorization, session/cookie/antiforgery policy, and module endpoint security. React auth behavior belongs in app auth components/hooks and calls backend auth endpoints.
- **Frontend/backend/worker/API/infrastructure:** Next.js and Blazor/Razor are frontend surfaces; module `Api` projects are backend HTTP surfaces; Hangfire jobs and scheduled jobs are worker runtime; `Infrastructure` projects own persistence/external adapters; Docker/GitHub Actions are infrastructure/deployment support.

## 6. Common change guide

- **New page:** Blazor: `src/Hosts/Main/Pages/<Area>` plus route constants in `src/Hosts/Main/Common/Routes` if needed. Next: `src/Hosts/Web/apps/<app>/src/app/<route>/page.tsx` imports page/surface components from the owning feature package and wires app navigation/shell files.
- **New component:** Workflow feature-specific React components live in `src/Hosts/Web/shared/<feature>-web`; app route/shell components stay in the owning app; generic presentation primitives live in `src/Hosts/Web/shared/web-ui`. App-local workflow feature folders are forbidden. Do not create design-system wrappers unless needed now.
- **New domain entity:** `src/Modules/<Module>/Domain`; add persistence mapping in Infrastructure only if it must be stored.
- **New command:** `src/Modules/<Module>/Application` under existing command/write convention; expose through Api only when needed.
- **New query:** `src/Modules/<Module>/Application` under existing query/read convention; keep it side-effect free.
- **Validation:** Domain invariants in Domain; use-case/input validation in Application; transport shape validation in Api; UI form validation in owning UI feature. Do not duplicate rules.
- **Database migration:** Generate EF Core migrations in `src/Modules/<Module>/Infrastructure/Migrations/<DbName>` or the owning server library/host migration assembly.
- **Translations/localization:** React locale/provider/formatting belongs in the core Web localization package; workflow feature messages belong to the owning `src/Hosts/Web/shared/<feature>-web` namespace; app-owned route/shell copy such as `home` and `landing` stays with the app route composition. Blazor/.NET localization lives under the owning host/module resources if present. Follow ADR-0007.
- **Tests:** .NET tests in `tests/Modules/<Module>.Tests`; React tests under app/shared package `src/__tests__`; architecture checks under `tools/architecture` or existing npm validators.
- **Pattern to copy:** Copy the nearest same-module/same-layer artifact first. For API-to-React work, follow `docs/runbooks/openapi-to-react-client.md` and ADR-0003/0005 conventions.

## 7. Generated code and fragile areas

Generated or derived artifacts:

- `src/Hosts/Web/shared/*/src/api/generated` for package-owned generated TypeScript clients after ADR-0008 migration. Existing `src/Hosts/Web/apps/*/src/api/generated` output is legacy app-bound generated code until its owning feature package is extracted.
- OpenAPI specs under `src/Hosts/Web/shared/*/src/api/specs` after ADR-0008 migration. Existing `src/Hosts/Web/apps/*/src/api/specs` specs are legacy app-bound sources until migrated.
- EF migration designer files and model snapshots under `src/**/Migrations`.
- `tsconfig.tsbuildinfo`, build output (`bin`, `obj`, `.next`, `node_modules`) when present.

Fragile or high-care areas:

- `AGENTS.md`, architecture ADRs/conventions/validators, identity/auth/security, host startup and middleware, module registration/composition, database migrations/snapshots, generated API boundaries, CI, package lock/version files, Docker/deployment config, and secrets/templates.
- API boundary drift between OpenAPI, generated clients, feature wrappers, and React routes; duplicated `apiFetch` behavior outside the Web transport package; ADR-0003 interface violations; module feature flags in `Main.csproj`; auth/cookie/session/antiforgery ordering; tenant resolution; EF migrations/snapshots; date/time UTC persistence; localization/date input rules; Docker port/base URL mismatches; npm workspace dependency/lockfile drift.
- Do not edit generated clients by hand; regenerate from OpenAPI. Do not put business rules in endpoints, UI, migrations, DTOs, localization resources, or SQL. Keep command/write operations separate from query/read operations.

## 8. Build/test/run pointers

- **Backend run:** `dotnet run --project src/Hosts/Main/Main.csproj`.
- **Backend build:** `dotnet build src/Hosts/Main/Main.csproj` or build the specific module/test project touched.
- **Backend tests:** `dotnet test tests/Modules/<Module>.Tests/<Module>.Tests.csproj` for changed module tests.
  
- **Web install:** from `src/Hosts/Web`, run `npm install` when dependencies are missing or package files changed.
- **Web dev:** from `src/Hosts/Web`, `npm run dev:private-practice-general` or `npm run dev:knowledge-qa-demo`.
- **Web build/typecheck/test:** from `src/Hosts/Web`, use `npm run build`, `npm run typecheck`, `npm run test`, and targeted workspace scripts.
- **Web API generation:** package or app scripts run `generate:api` before dev/build; after OpenAPI changes, regenerate in the owning feature package when migrated, or in the legacy app workspace until migration is complete.

- **Docker/dev containers:** `docker compose -f src/Hosts/docker-compose.web.yml up --build` runs web plus main.

- **Before committing:** Run the narrowest deterministic checks for changed files, plus required architecture/API/UI validators. For documentation-only changes, at minimum inspect the Markdown and verify git diff.

## 9. Documentation map

- **Architecture:** `docs/architecture/decisions/_index.md`, `docs/architecture/conventions`, and `docs/architecture/validation`.
- **Agent/rules:** `AGENTS.md` and `.agents/skills/*/SKILL.md`.
- **Repository documentation index:** `docs/README.md`.
- **Platform/module behavior:** `docs/platform/README.md` and `docs/platform/modules/<module>`.
- **App-specific journeys:** `docs/apps/<app>`.
- **Host runbooks/tasks:** `docs/hosts/<host>`.
- **Cross-cutting runbooks:** `docs/runbooks`.
- **Journey/test indexes:** `docs/indexes`.
- **Development workflow notes:** `docs/development`.
- **Durable lessons:** [`docs/memory.md`](memory.md) records practical repo-specific lessons that do not belong in ADRs, runbooks, or module docs.

## 10. Maintenance rules

- Update this map when top-level folders change; a host/app/module/package is added, removed, or renamed; generated-code locations move; build/test/run commands change; ownership/review boundaries change; external systems/config conventions change; or risky areas/validators change.
- Never include secrets, live tenant/user data, private credentials, long design rationale, volatile implementation minutiae, speculative roadmap, or duplicate business rules.
- This map answers “where is it and what owns it?” Architecture docs answer “what rule/decision governs it and why?”
- This map is stable navigation. Durable agent lessons belong in a memory file or scoped docs if the repo adopts one.
- Keep it short: prefer folder-level pointers, tables, and links to owning docs. If a section needs detailed behavior, move that detail to the owning module/app/runbook/ADR and link it here.
