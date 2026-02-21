# Sea Estate Creation Guide (Pattern from `src/Estates/Sea`)

This guide captures the implementation style used by the Sea estate—from domain modeling through infrastructure wiring and interface plugins—so you can build another estate with the same architecture and conventions.

## 1) Architectural shape and project layout

Create one estate as **four sibling projects**:

- `src/Estates/<EstateName>/Domain`
- `src/Estates/<EstateName>/Application`
- `src/Estates/<EstateName>/Infrastructure`
- `src/Estates/<EstateName>/Interface`

Sea follows clean layering:

- **Domain**: entities, DTOs, descriptors, enums, mapper declarations.
- **Application**: abstractions and MediatR command/query contracts.
- **Infrastructure**: EF Core context, repositories, services, handlers, DI, serializers, feed sources.
- **Interface**: estate entrypoint, UI components, plugin contracts + implementations, interface DI.

Keep cross-layer dependencies one-way:

- `Interface -> Infrastructure -> Application -> Domain`
- `Interface` may also reference shared interface/base modules.

## 2) Coding style used across Sea

Follow these style rules to stay consistent:

- Use **file-scoped namespaces**.
- Prefer **records for request/descriptor/plugin context models**.
- Prefer **primary constructors** on handlers/services/components where possible.
- Use concise collection literals (`[]`, `[.. source]`) and expression-bodied members when clear.
- Use `GlobalUsings.cs` per project to keep files small.
- Keep files narrowly focused (one main responsibility per file/class).

## 3) Domain design pattern

### Entities

In `Domain/Entities`:

- Inherit from shared `Entity` base (`Vitarish.Entities.Server.Core`).
- Use `SetField(ref field, value, nameof(Property))` in mutable properties to preserve change tracking behavior used across the platform.
- Default strings to `string.Empty`; default dictionary metadata to `[]`.
- Add behavior methods on entities where it clarifies invariants (example: `Event.Commit`, `Event.Uncommit`).

### DTOs / descriptors / snapshots

In `Domain/Dto` and `Domain/Models`:

- Keep read/write DTOs split (`*ReadDto`, `*WriteDto`).
- Use descriptor DTOs for filtering/search (`*DescriptorDto`).
- Keep statistics/projection models in `Domain/Models` (`ProjectStats`, etc.).

### Mapping

In `Domain/Mappers`:

- Use Mapperly with partial mapper classes (`[Mapper(...)]`).
- Keep mapper extension composition centralized (`EntityMappingExtensions` + per-entity mapper files).

## 4) Application layer pattern (CQRS contracts + service interfaces)

### Service abstractions

In `Application/Abstractions`:

- Define interfaces per aggregate area (`IProjectsService`, `IProceduresService`, etc.).
- Define estate marker interface (`ISeaEstate : IEstate`).
- Add access-controlled wrappers as dedicated interfaces (`IAccessControlledProjectsService`, etc.) instead of mixing auth logic into base services.

### Commands/queries

In `Application/Commands` and `Application/Queries`:

- Define MediatR request contracts only; keep them small.
- Naming pattern:
  - `VerbNounCommand` and `GetNounQuery`
  - Files grouped by feature (`Commands/Events`, `Commands/Attachments`, ...).

## 5) Infrastructure layer pattern

### DbContext and persistence

In `Infrastructure/Data/<Estate>DbContext.cs`:

- Configure DbSets per entity.
- Apply multi-tenant + soft-delete filters centrally (Sea uses Finbuckle + `HasQueryFilter`).
- Configure JSON columns and projections/views here.
- Use `SaveChanges`/`SaveChangesAsync` hooks for tenant enforcement.

### Repositories

In `Infrastructure/Repositories`:

- Inherit from shared `EntityRepository<TEntity, TDescriptor, TDbContext>`.
- Override `Apply` to compose descriptor-based filtering (search, role visibility, template mode, etc.).
- Keep query logic repository-local; do not spread LINQ filters into UI.

### Services

In `Infrastructure/Services`:

- Base entity services inherit shared `EntityService<...>` and mostly wire converters.
- Access-controlled services inherit shared `AccessControlledServiceBase` and gate operations via user roles.
- Keep authorization as decorator-like wrappers around base services.

### Handlers

In `Infrastructure/Handlers`:

- Implement MediatR `IRequestHandler<,>` for each command/query contract.
- Keep orchestration logic here (e.g., event-chain mutation).
- Prefer explicit local variable naming and null-handling with early returns.

### DI and serializers

In `Infrastructure/DependencyInjection/DependencyInjectionExtensions.cs`:

- Provide one extension entrypoint (e.g., `AddSeaInfrastructure`).
- Register generated entity handlers, MediatR handlers, access-controlled services, and feed sources.

In `Infrastructure/Serializers`:

- Add source-generated JSON context and register via `ConfigureHttpJsonOptions` from estate entrypoint.

## 6) Interface layer pattern

### Estate entrypoint

In `Interface/SeaEstate.cs` (or equivalent):

- Extend `Estate` and implement marker interface.
- `Register(...)` wires:
  - generated domain handlers,
  - DB context,
  - infrastructure,
  - JSON serializer context,
  - interface/plugin configuration.
- `Initialize(...)` performs one-time runtime setup (e.g., view initialization).

### Components

In `Interface/Components`:

- Use base component classes in `Components/Base` to share injected services and common UI logic.
- Keep `.razor.cs` for behavior and `.razor` for markup.
- Use plugin lookups by tenant/plugin ID to allow runtime substitution.

### Plugin system

In `Interface/Plugins`:

- Define plugin context records implementing shared `PluginContext`.
- Define focused plugin interfaces (`IProjectsGridPlugin`, `IProjectEditPlugin`, `IProjectDetailsPlugin`, event item creation/timeline contracts).
- Implement concrete plugins in dedicated folders (Sea uses `Plugins/Clinic/*`).
- Plugin implementations return `RenderFragment` and pass callbacks through `EventCallback.Factory.Create(...)`.

### Interface DI

In `Interface/DependencyInjection`:

- Keep plugin registration and general interface registration separate:
  - `ConfigurePlugins()` for concrete plugin mapping.
  - `ConfigureInterface()` for overall interface service wiring.

## 7) Estate registration with aggregator

To activate a new estate globally:

1. Add new estate class (like `SeaEstate`) in its interface project.
2. Reference the estate in `src/Estates/Aggregator/Estates.Aggregator/EstatesExtensions.cs` by adding it to `GetEstates()`.
3. Ensure localized display name exists for `EstatesProvider` usage.

## 8) Recommended implementation checklist

Use this sequence when creating a new estate:

1. Scaffold `Domain/Application/Infrastructure/Interface` projects and references.
2. Define core entities + DTO/descriptor models.
3. Add mapper declarations/extensions.
4. Define application abstractions and command/query contracts.
5. Implement DbContext + repositories.
6. Implement services (base + access-controlled wrappers).
7. Implement MediatR handlers.
8. Add infrastructure DI extension.
9. Implement interface components and plugin contracts.
10. Implement concrete plugin variants and register in interface DI.
11. Create estate entrypoint class inheriting `Estate`.
12. Add estate to aggregator list.

## 9) Quality guardrails (based on Sea conventions)

- Keep command and query concerns separate.
- Keep auth checks in access-controlled wrappers, not in UI.
- Keep mapping centralized and declarative.
- Keep tenant filtering + soft delete guaranteed at DbContext/repository level.
- Prefer plugin-based UI extension points over `if/else` UI branching.
- Keep descriptors as single source of filtering truth across repositories/services.
