# Vitarish Solution Instructions

Use this map before scanning the repository. It captures the stable structure and the placement rules that are easy to miss.

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
  Interfaces/             shared UI/interface packages not owned by a single feature module
  Libraries/              reusable platform libraries (Entities, Audit, generators, etc.)

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

## Journey maps

Journey maps, which are directions about UX, are stored in `docs\journeys\*`.

## Entity Conventions

When creating a new entity class, agents must follow the centralized entity-management conventions already demonstrated by the Sea module.

### Required artifacts for every new entity

For each new entity, create the smallest required set of artifacts:

- **Entity class**  
  - Must be based on the solution’s existing `Entity` base class.  
  - Must keep domain state and behavior focused on the entity’s single responsibility.

- **Read DTO**  
  - Create a dedicated `<EntityName>ReadDto`.  
  - Use it only for query/read-facing data transfer.

- **Write DTO**  
  - Create a dedicated `<EntityName>WriteDto`.  
  - Use it only for command/write-facing input data.

- **Descriptor and its DTO**  
  - Create a dedicated `<EntityName>Descriptor`.
  - Mark it with `[EntityDescriptorFor(typeof(<EntityName>))]`
  - Create its DTO `<EntityName>DescriptorDto`.

- **Mappers**  
  - Create a dedicated Mapperly auto-generated mapper methods `ToRead`, `ToWrite`, `ToEntity`, and `ToDescriptor` for the entity.
  - Follow the same mapper style and placement used by the Sea module.  
  - Mapping rules must be centralized in this mapper rather than duplicated across controllers, services, repositories, or UI components.

- **Entity mapping extensions**  
  - Create `EntityMappingExtensions` under the `*.Domain.Mappers` namespace and define extension methods of the mapper methods.  
  - Use these extensions for standardized entity/DTO mapping entry points where the existing solution pattern requires them.

- **Repository**  
  - Create a dedicated repository class based on the solution’s existing `EntityRepository` pattern.  
  - The repository must be responsible only for persistence access for that entity.  
  - Business rules must not be placed in the repository.

- **Service**  
  - Create a dedicated service class based on the solution’s existing `EntityService` pattern.
  - `EntityService` base class provides all required CQRS style artifacts for base crud operations. 
  - Services must be a gate to all CQRS style commands and queries of the module beyond base CRUD operations.

### Reference implementation

Use the **Sea module** as the source of truth for implementation style, naming, project placement, mapper structure, repository structure, service structure, and extension conventions.

### Design intent

These conventions centralize entity management by ensuring each entity owns a consistent set of DTOs, mappers, mapping extensions, repository access, and optional service orchestration. This reduces duplicated mapping and persistence logic, decreases defects, simplifies debugging, and the most important objective: easier separation/decoupling of client and server sides.

### Architectural requirements

- Keep domain logic out of DTOs, repositories, adapters, controllers, and UI components.  
- Keep read and write models separate.  
- Do not mix command behavior with query behavior.  
- Do not duplicate mapping rules across artifacts.  
- Do not introduce services, abstractions, or layers unless they are required by the current entity behavior.


## Focused module builds

For faster inner-loop builds, use the module build profile flags documented in `docs/development/focused-builds.md`. The source of truth is `src/Build/ModuleBuildProfiles.props`, with `ModuleBuildProfile=All` preserving the full module graph.

Examples:

```bash
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Users
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Appointments

## Caution

- Never create EF migrations.
dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Custom -p:EnableUsersModule=true -p:EnableAppointmentsModule=true
```

Use focused builds only for local speed. Before submitting changes, run `ModuleBuildProfile=All` or the full solution build. Keep module selection in MSBuild project composition; do not add source-level `#if` switches for disabled modules.
