# Estate Creation Guide

An **estate** is a package that can be assigned to a tenant (`IEstate` / `Estate` contract). This guide defines how to create a new estate from scratch, using existing platform patterns.

## 1) What an estate is

An estate should:

- expose a unique `Name`;
- implement tenant-assignable registration and initialization lifecycle;
- encapsulate one bounded functional domain;
- remain independently composable in the estates aggregator.

Core contract:

- `IEstate` defines `Name`, `Register(...)`, and `Initialize(...)`.
- `Estate` is the base class used by concrete estate implementations.

## 2) Required project structure

Create one estate as four sibling projects:

- `src/Estates/<EstateName>/Domain`
- `src/Estates/<EstateName>/Application`
- `src/Estates/<EstateName>/Infrastructure`
- `src/Estates/<EstateName>/Interface`

Layer responsibilities:

- **Domain**: entities, enums, DTOs/descriptors, domain models, mapping declarations.
- **Application**: service abstractions + MediatR command/query contracts.
- **Infrastructure**: persistence, repositories, handlers, services, dependency injection, serializers.
- **Interface**: estate entrypoint, UI components, plugin contracts and plugin implementations.

Dependency flow should stay one-way:

- `Interface -> Infrastructure -> Application -> Domain`

## 3) Domain layer blueprint

### Entities

- Inherit from the shared `Entity` base.
- Keep entity files focused on one aggregate root/entity.
- Preserve change tracking behavior with `SetField(...)` where this convention is used.
- Include domain behavior methods only when they enforce domain invariants.

### DTOs, descriptors, snapshots

- Separate read/write DTOs (`*ReadDto`, `*WriteDto`).
- Use descriptor DTOs (`*DescriptorDto`) for filtering, paging, and query options.
- Keep projection/statistics models in `Domain/Models`.

### Mapping

- Keep mapping centralized in `Domain/Mappers`.
- Use mapper generator declarations consistently (Mapperly in current platform style).

## 4) Application layer blueprint

### Abstractions

- Define one interface per capability area (`IProjectsService`, `IEventsService`, etc.).
- Define one estate marker interface (`I<EstateName>Estate : IEstate`).
- Keep access control as dedicated wrapper interfaces (`IAccessControlled*Service`) instead of mixing concerns.

### CQRS contracts

- Place commands in `Application/Commands/<Feature>`.
- Place queries in `Application/Queries/<Feature>`.
- Keep contracts small and transport-only.
- Keep command and query responsibilities strictly separated.

## 5) Infrastructure layer blueprint

### DbContext

- Implement `<EstateName>DbContext` under `Infrastructure/Data`.
- Define all `DbSet<T>` for estate entities.
- Apply multi-tenant + soft-delete filters centrally in the context.
- Configure value conversions (e.g., JSON metadata) and read models/views.

### Repositories

- One repository per entity/aggregate query surface.
- Keep descriptor-based filtering in repository `Apply(...)` logic.
- Do not push repository filtering logic to components.

### Services

- Base services should handle CRUD orchestration and mapping.
- Access-controlled services should wrap base services and enforce policy.
- Keep auth/policy checks out of UI components.

### Handlers

- Implement MediatR handlers in `Infrastructure/Handlers`.
- Keep handlers focused on command/query orchestration.
- Prefer explicit null handling and deterministic flow.

### Dependency injection and serialization

- Add one infrastructure entry extension (e.g., `Add<EstateName>Infrastructure`).
- Register MediatR handlers, repositories, services, wrappers, and optional feed sources.
- Register source-generated JSON serialization context where applicable.

## 6) Interface layer blueprint

### Estate entrypoint

Implement `<EstateName>Estate : Estate, I<EstateName>Estate` in `Interface`.

`Register(...)` should wire:

- domain-generated handlers/registrations;
- estate DbContext;
- infrastructure registrations;
- JSON options/context;
- interface and plugin registrations.

`Initialize(...)` should run estate startup tasks (e.g., SQL view initialization).

### Components

- Use `Components/Base` classes for shared behavior and injected dependencies.
- Keep markup in `.razor`; logic in `.razor.cs` when complex.
- Keep components narrowly focused (single responsibility).

### Plugin system

- Define plugin context records using shared `PluginContext`.
- Define narrow plugin interfaces per extension point.
- Implement concrete plugin variants under `Interface/Plugins/...`.
- Resolve active plugins through tenant/plugin identifier, not hardcoded branching.

### Interface DI

- Keep plugin mapping in one extension (e.g., `ConfigurePlugins`).
- Keep broader interface setup in another extension (e.g., `ConfigureInterface`).

## 7) Registering the estate globally

To make an estate available:

1. Implement `<EstateName>Estate` in the estate interface project.
2. Add it to `EstatesExtensions.GetEstates()` in the estates aggregator.
3. Ensure display/localization resources exist for the estate name.

## 8) Step-by-step implementation checklist

1. Scaffold `Domain/Application/Infrastructure/Interface` projects.
2. Add project references with one-way dependency flow.
3. Implement domain entities, DTOs, descriptors, models.
4. Add mapper declarations/extensions.
5. Define application abstractions and CQRS contracts.
6. Implement DbContext and repositories.
7. Implement base services + access-controlled wrappers.
8. Implement MediatR handlers.
9. Add infrastructure DI entry extension.
10. Build interface components and plugin contracts.
11. Implement concrete plugins and register them.
12. Implement estate entrypoint (`Estate` subclass).
13. Register estate in aggregator.
14. Validate tenant assignment and runtime initialization.

## 9) Quality guardrails

- Single responsibility per module/class.
- One source of truth for filtering (descriptor-driven repository logic).
- Command/query separation preserved end-to-end.
- Tenant and soft-delete constraints enforced below UI level.
- Extension through plugins over invasive branching.
- Keep abstractions stable; keep implementation extensible.
