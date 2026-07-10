  # Blazor Interface Decommission Plan

  ## Summary

  - Decommission all Blazor/Razor Component UI and all .NET Interface UI projects across the solution.
  - Keep src/Hosts/Main as the backend/API/identity/Hangfire/OpenAPI host; remove its UI role.
  - Keep existing React Web routes and packages as the only app UI surface. Do not build React parity for Blazor-only workflows as part of this decommission.
  - Per selected defaults: Blazor-only workflows are retired, and old Blazor URLs are removed without redirects.
  - Current baseline: ~413 Razor files under src, 118 route-bearing Main host pages, and 23 Interface-style projects.

  ## Key Architecture Changes

  - Add a new ADR, likely docs/architecture/decisions/0009-decommission-blazor-interfaces.md, that supersedes ADR-0003’s .Interface UI allowance while preserving ADR-0003’s Contracts/Client/API boundary
    rules.

  - Define final ownership:
      - React Web feature packages own UI and app experiences.
      - Backend modules keep Domain/Application/Infrastructure/Api/Contracts.
      - C# Client projects remain only when a non-Blazor .NET consumer still uses them.
      - .Interface projects are deleted, not converted into new abstraction layers.

  - Remove public .NET UI extension points:
      - Delete Add*Interface, Configure*Interface, AddVitarishModulesBaseInterface, Blazor plugin/configuration extensions, and public Razor components.
      - Move any still-needed non-UI C# HTTP client registration into the owning Client project’s dependency-injection namespace.

  - Remove public Blazor routes:
      - Delete Main host AppRoutes entries that only served Blazor pages.
      - Do not add redirect endpoints. Retired paths should be unhandled by Main and return normal 404/not-found behavior depending on deployment routing.

  - Preserve backend APIs by default:
      - Do not delete Domain/Application/API behavior merely because the Blazor UI was removed.
      - Delete backend endpoints only when a separate module decision says the capability itself is retired.

  ## Implementation Phases

  1. Establish governance and freeze
      - Create the decommission ADR and mark Blazor/Razor Component UI as forbidden for new production code.
      - Update docs/codebase-map.md, Web runbooks, ADR-0003 validation docs, and docs/memory.md.
      - Add a retirement register under app/platform docs listing retired Blazor-only workflow groups: Platform admin, owner tenant management, Commerce, Medical/prescriptions, Sea owner/user/admin,
        Knowledge owner/admin, Communications admin/reminders, and any other route group found in inventory.

      - Freeze new .razor, Microsoft.NET.Sdk.Razor, MudBlazor, and Microsoft.AspNetCore.Components production additions except during active deletion PRs.

  2. Normalize React as the only UI surface
      - Keep existing React routes in src/Hosts/Web/apps/private-practice-general and knowledge-qa-demo.
      - Do not implement missing React parity for retired Blazor workflows.
      - Update React coverage audits so missing Blazor-only journeys are classified as retired, not missing.
      - Remove stale docs that cite .razor evidence as current app evidence, or move that evidence into the retirement register.
      - Keep app route files as composition surfaces and avoid moving Blazor concepts into React packages.

  3. Remove Main host Blazor runtime
      - Delete Blazor shell files under src/Hosts/Main/Components: App.razor, Routes.razor, layouts, reconnect UI, Blazor base classes, and component-only helpers.
      - Delete route pages under src/Hosts/Main/Pages/**, including account, clinic, platform, parties, appointments, Sea, commerce, prescriptions, links, knowledge, and common Blazor pages.
      - Delete the remaining legacy .cshtml account submit page if no non-Blazor backend path uses it.
      - Remove AddRazorComponents, AddInteractiveServerComponents, MapRazorComponents, AddRazorPages, and MapRazorPages when no .cshtml remains.
      - Remove Blazor-specific auth handling such as BlazorResultHandler.
      - Remove Blazor circuit telemetry registration and circuit-only Platform module code.
      - Remove Main UI static assets that only supported Blazor/MudBlazor: blazor.web.js references, MudBlazor CSS/JS, reconnect scripts, stack/glass/palette/customization CSS, and local Font Awesome assets
        unless another non-Blazor consumer remains.

      - Keep backend middleware order intact for forwarded headers, localization, CORS, rate limiting, session, IdentityServer, authentication, authorization, multitenancy, antiforgery, OpenAPI, module
        initialization, Hangfire, and scheduled jobs.

  4. Delete module/app Interface projects
      - Remove direct Main references to Apps.*.Interface, Modules.*.Interface, and Modules.Base.Interface.
      - Remove Module -> Interface project references from every module project.
      - Remove App -> Interface project references from app projects.
      - Delete src/Modules/*/Interface, src/Apps/*/Interface, and tracked src/Interfaces/* artifacts.
      - For each deleted Interface project, classify contents:
          - UI components, view models, UI state, navigation, theme/palette, Blazor localization, MudBlazor wrappers, and plugin UI are deleted.
          - Client registration moves to Client only if a surviving .NET consumer exists.
          - Contract DTOs incorrectly living in Interface move to Contracts only if still needed by backend/API/React.
          - Infrastructure or business behavior found in Interface moves to the owning backend layer only if still needed; otherwise it is retired with the UI.

      - Remove cross-interface aggregators such as Modules.Aggregator.Interface and Apps.Aggregator.Interface.

  5. Clean backend and build graph
      - Update Vitarish.slnx to remove deleted Interface projects.
      - Update src/Hosts/Main/Main.csproj to remove Blazor project references and duplicated RazorComponent Remove item groups.
      - Keep module build profiles for backend module composition; remove only UI/page-specific profile mechanics.
      - Run restore to update packages.lock.json files and eliminate MudBlazor and deleted project package-lock entries.
      - Audit C# Client projects after Interface deletion. Delete only those with no remaining .NET consumer; otherwise keep them as API client adapters.
      - Remove dead DI registrations, unused namespaces, stale route constants, and unused base/UI notification types.

  6. Update validation and CI
      - Replace or supersede validate_interface_boundaries.py with a fail-fast validator that forbids Blazor Interface resurrection.
      - New validator must fail on tracked production occurrences of:
          - *.razor, *.razor.cs, *.razor.css, *.razor.js
          - Microsoft.NET.Sdk.Razor
          - MudBlazor
          - AddRazorComponents, MapRazorComponents, InteractiveServer, blazor.web.js
          - src/Modules/*/Interface, src/Apps/*/Interface, src/Interfaces/*

      - Keep API validators: validate_module_api_boundaries.py and validate_openapi_endpoint_coverage.py.
      - Update .github/workflows/architecture-validation.yml to run the no-Blazor validator and trigger on src/Hosts/Main/**, src/Modules/**, src/Apps/**, tools/architecture/**, and architecture docs.
  7. Final documentation cleanup
      - Remove Web runbook “Coexistence with Blazor” guidance.
      - Update host docs so Main is backend-only and Web is UI-only.
      - Mark old Blazor task docs, including the Interfaces.Accounts plan, as superseded by the decommission ADR or remove them if they are no longer useful.
      - Update app journey/test docs so retired workflows are explicit product retirements, not unresolved implementation gaps.
      - Update platform module docs that currently cite Blazor components as current evidence.

  ## Test Plan

  - Static decommission checks:
      - rg --files src -g "*.razor" -g "*.razor.cs" -g "*.razor.css" -g "*.razor.js" returns no production files.
      - rg -n "MudBlazor|Microsoft.NET.Sdk.Razor|AddRazorComponents|MapRazorComponents|InteractiveServer|blazor.web.js|\\.Interface" has no forbidden production hits.
      - New no-Blazor architecture validator passes.

  - Backend checks:
      - dotnet restore Vitarish.slnx
      - dotnet build Vitarish.slnx -p:ModuleBuildProfile=All
      - dotnet test for all tests/Modules/*.Tests/*.csproj
      - python3 tools/architecture/validate_module_api_boundaries.py
      - python3 tools/architecture/validate_openapi_endpoint_coverage.py

  - Web checks from src/Hosts/Web:
      - npm run typecheck
      - npm run test
      - npm run build
      - npm run validate:api-boundary
      - npm run validate:localization
      - npm run validate:date-inputs
      - npm run validate:web-ui-boundary
      - npm run validate:app-boundaries
      - npm run validate:docs:app-boundaries

  - Runtime smoke:
      - Start Main and confirm /openapi/{documentName}.json, module APIs, auth APIs, Hangfire dashboard, antiforgery, CORS, tenant header forwarding, and scheduled jobs still work.
      - Start Web and confirm current React login, account, home, knowledge QA, clinic, and Sea routes that still exist work against Main.
      - Confirm removed Blazor URLs have no compatibility redirects.

  ## Assumptions

  - Blazor-only workflows are intentionally retired; no React parity is required before deletion.
  - Old Blazor URLs are removed, not redirected or proxied.
  - Main remains the backend service host; this plan does not remove IdentityServer, Hangfire, OpenAPI, module APIs, persistence, jobs, or backend module behavior.
  - React Web remains the only UI family for current and future app experiences.
  - Generated clients, DTOs, hooks, query keys, and mocks are regenerated only through existing generation scripts and are never manually edited.
