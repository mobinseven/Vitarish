# Vitarish Solution Map for Agents

Use this map before scanning the repository. It captures the stable structure and the placement rules that are easy to miss.

## One-minute placement guide

| Need to add/change | Put it here | Do not put it here | Notes |
| --- | --- | --- | --- |
| Routable Blazor page (`@page`) for the main app | `src/Hosts/Main/Pages/<Area>/...` | `src/Modules/*/Interface/Components` | Pages are host composition. Module interfaces provide reusable UI pieces only. |
| Route constants / URL builders | `src/Hosts/Main/Common/AppRoutes.cs` and `src/Hosts/Main/Common/Routes/*Routes.cs` | Inline strings spread through pages/components | `AppRoutes` is the source of truth for main-app routes. |
| Main app shell/layout/routing infrastructure | `src/Hosts/Main/Components` | Module folders | Host owns layouts, route rendering, and app-level page bases. |
| Reusable module UI component, dialog, grid, plugin UI | `src/Modules/<Module>/Interface/Components`, `.../Plugins`, `.../DependencyInjection` | `src/Hosts/Main/Pages` | No `@page`; expose components/pages' building blocks to the host. |
| Module business contracts / use cases | `src/Modules/<Module>/Application` | Host or Interface projects | Interfaces and handlers live here; UI calls abstractions rather than data stores. |
| Module domain entities, DTOs, enums, descriptors | `src/Modules/<Module>/Domain` | Infrastructure or Host | Keep domain model free of UI and persistence concerns where possible. |
| EF Core, repositories, external adapters, jobs, services | `src/Modules/<Module>/Infrastructure` | Domain or Host | Infrastructure implements Application abstractions and owns persistence/external details. |
| Module registration and endpoint initialization | `src/Modules/<Module>/Interface/<Module>Module.cs` or `src/Modules/<Module>/Hosting/<Module>Module.cs` | `src/Hosts/Main/Program.cs` unless truly host-specific | Register modules through the aggregator. |
| Add a module to the app | `src/Modules/Aggregator/Modules.Aggregator/ModuleExtensions.cs` and its `.csproj` references | Manual one-off host registration | Main calls `RegisterModules` / `InitializeModules`. |
| Identity bounded context work | `src/Identity/<Context>/{Domain,Application,Infrastructure,Hosting}` | Modules or Host | Identity contexts mirror the module layering. |
| Shared interface primitives | `src/Interfaces/Interfaces.Base` | Feature modules | Base UI services/extensions/components used by multiple interfaces. |
| Cross-module app aggregation | `src/Apps/Base`, `src/Apps/Aggregator` | Host pages or individual modules | App-level orchestration over module/application contracts. |
| Shared libraries/framework pieces | `src/Libraries/*` | Feature modules | Entities, Audit, generators, and reusable infrastructure primitives. |
| Tests | `tests/<same top-level area>/...` | Production projects | Mirror source ownership: `tests/Modules/<Module>.Tests`, `tests/Hosts/Main.Tests`, etc. |

## Architecture at a glance

```text
src/
  Hosts/Main/             executable web host: Program, middleware, layouts, routable Pages, migrations
  Apps/                   app-level aggregation/orchestration over module and identity application contracts
  Modules/<Module>/       feature bounded contexts
    Domain/               entities, DTOs, enums, descriptors, pure rules
    Application/          abstractions, commands/queries, events, use-case contracts
    Infrastructure/       EF DbContext, repositories, external services, jobs, implementation details
    Interface/            reusable Razor components, plugins, module UI DI; no routable pages
    Hosting/              module endpoints/host-facing registration when a module needs web endpoints
  Identity/<Context>/     identity bounded contexts with the same Domain/Application/Infrastructure/Hosting split
  Interfaces/             shared UI/interface packages not owned by a single feature module
  Libraries/              reusable platform libraries (Entities, Audit, generators, etc.)

tests/                    tests mirror the source area they verify
```

## Dependency direction rules

- Prefer this flow: `Host -> Apps/Aggregator -> Module Interface/Hosting -> Application -> Domain`, with `Infrastructure` implementing Application contracts.
- Keep Domain independent of UI and host concerns.
- Keep routable page composition in the Host; keep reusable module UI in module Interface projects.
- Avoid direct Host-to-Infrastructure coupling unless existing host bootstrapping or migration code already requires it.
- Do not create a second state/context model for the same concern; extend the existing owner instead.

## Blazor page and UI rules

- A file with `@page` belongs under `src/Hosts/Main/Pages/...` for the main app.
- A page should compose smaller components from `src/Modules/<Module>/Interface/Components/...`.
- Module Interface components should avoid hard-coded app routes; use parameters, callbacks, or `AppRoutes` from host pages where routing is host-owned.
- Add or update route constants before wiring navigation. Follow the route conventions documented in `AppRoutes`: lowercase kebab-case, plural resources, constrained ID segments, and query strings for view state.

## Module onboarding checklist

1. Create/extend the module layers only as needed: `Domain`, `Application`, `Infrastructure`, `Interface`, and optional `Hosting`.
2. Put reusable UI in `Interface/Components`; put routable pages in `src/Hosts/Main/Pages/<ModuleOrArea>`.
3. Add route constants/builders under `src/Hosts/Main/Common/Routes` if the host navigates to the page.
4. Register module services inside the module's own DI/module extension.
5. Add the module to `src/Modules/Aggregator/Modules.Aggregator/ModuleExtensions.cs` and `Modules.Aggregator.csproj` when it should load in the main app.
6. Add host migrations/bootstrapping only where the existing host pattern requires it.
7. Add tests in the mirrored `tests/...` area.

## Quick examples

- New Social connections page: create `src/Hosts/Main/Pages/Social/SocialConnectionsPage.razor`, add `AppRoutes.Social.Connections`, and compose reusable Social UI from `src/Modules/Social/Interface/Components`.
- New Social connections grid: create `src/Modules/Social/Interface/Components/SocialConnectionsGrid.razor`; no `@page` directive.
- New webhook endpoint: keep endpoint mapping in `src/Modules/Social/Hosting/Endpoints` and initialize from `SocialModule`, not from a Razor page.
