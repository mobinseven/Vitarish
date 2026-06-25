# Artifact Placement Table

Use existing conventions inside the owning scope before inventing a new folder.

| Artifact type | Preferred location | Responsibility |
| --- | --- | --- |
| Domain entity, enum, value object, pure model, pure rule | `src/Modules/<Module>/Domain/...`, `src/Apps/<App>/Domain/...`, or `src/Identity/<BoundedContext>/Domain/...` | Own state and pure business behavior for the owning boundary |
| Domain event | `Domain/Events` under the owning module, app, or identity context | Represent a business fact inside the owning boundary |
| Domain mapper | Existing `Domain/Mappers` convention only when mapping is pure and technology-free | Translate between domain-owned shapes without infrastructure, transport, or UI coupling |
| Internal domain DTO/model | Existing `Domain/Dto` or `Domain/Models` convention only when already established and not public API contract state | Carry domain-owned internal data without transport behavior |
| Public API request DTO | `src/Modules/<Module>/Contracts` for migrated ADR-0003 modules | Represent serialization-safe request input for the module API |
| Public API response DTO | `src/Modules/<Module>/Contracts` for migrated ADR-0003 modules | Represent serialization-safe response output for the module API |
| Public API result/error DTO | `src/Modules/<Module>/Contracts` for migrated ADR-0003 modules | Represent transport-safe success and failure shapes |
| Public API enum | `src/Modules/<Module>/Contracts` for migrated ADR-0003 modules | Represent public transport enum values, not domain behavior |
| Module API route constants | `src/Modules/<Module>/Contracts` when shared by Client and Api | Provide one route source for the module HTTP boundary |
| Contract transport-only value shape | `src/Modules/<Module>/Contracts` | Carry serialization-safe values without business behavior |
| Application command | `Application/Commands` under the owning boundary | Change state through one use case |
| Application query | `Application/Queries` under the owning boundary | Return data without changing observable state |
| Application contract/port | `Application/Abstractions` or existing `Application/Contracts` convention | Define stable dependencies needed by application logic |
| Application event | `Application/Events` | Represent use-case-level notifications or integration points |
| Client HTTP adapter | `src/Modules/<Module>/Client` | Provide frontend-safe HTTP methods for module use cases |
| Client result/failure mapping | `src/Modules/<Module>/Client` unless shared as transport contract | Surface validation, auth, conflict, timeout, cancellation, transport, deserialization, and unexpected failures intentionally |
| Api endpoint mapping | `src/Modules/<Module>/Api` | Register Minimal API endpoints and map HTTP contracts to Application commands/queries |
| Api response/failure mapping | `src/Modules/<Module>/Api` | Convert Application outcomes to structured HTTP responses without leaking internals |
| Infrastructure adapter | `Infrastructure/...` | Implement application contracts with concrete technology |
| Repository | `Infrastructure/Repositories` | Persist and retrieve one persistence concern only |
| DbContext/data mapping/migration | `Infrastructure/Data` or existing `Infrastructure/Migrations` convention | Own EF and persistence mapping details |
| Infrastructure validator | Existing `Infrastructure/Validators` convention when validation depends on infrastructure or external state | Validate infrastructure-backed constraints outside pure domain rules |
| Background job | `Infrastructure/BackgroundJobs`, `src/Modules/Base/BackgroundJobs`, or host infrastructure when host-owned | Run asynchronous work with explicit ownership and dependency boundaries |
| Module interface component | `src/Modules/<Module>/Interface/Components` | Render reusable frontend-safe UI owned by that module |
| Module interface plugin | Existing `src/Modules/<Module>/Interface/Plugins` convention | Expose module-owned UI integration points |
| Module interface model/service | `src/Modules/<Module>/Interface/Models` or `Interface/Services` | Support reusable module UI without backend implementation coupling |
| Interface package component | `src/Interfaces/Interfaces.<Name>/Components` | Provide reusable cross-app UI in an interface package |
| Interface package abstraction | `src/Interfaces/Interfaces.<Name>/Abstractions` | Define UI-facing abstractions for the interface package |
| Interface package model | `src/Interfaces/Interfaces.<Name>/Models` | Carry UI-facing state for the interface package |
| Interface package service | `src/Interfaces/Interfaces.<Name>/Services` | Support interface package behavior without backend implementation coupling |
| Interface localization resource | Existing `src/Interfaces/Interfaces.<Name>/Resources` or `Localization` convention | Own UI text/resources for the interface package |
| Module DI | Existing `Interface/DependencyInjection`, `Infrastructure/DependencyInjection`, or module composition convention | Register the owning layer’s services |
| Module composition | `src/Modules/<Module>/Module` or existing aggregate project convention | Package module API, Application, Domain, and Infrastructure registrations for host startup |
| Routable host page | `src/Hosts/<Host>/Pages/...` | Compose route-level UI and route-specific state |
| Host route constants/builders | `src/Hosts/<Host>/Common/Routes` | Centralize host-owned UI navigation paths |
| Host authorization/pipeline/platform integration | Existing `src/Hosts/<Host>/Authorization`, `Infrastructure/Pipeline`, or `Infrastructure/Platform` convention | Compose host-specific runtime behavior |
| App-specific artifact | `src/Apps/<App>/...` | Own behavior specific to that app boundary |
| Identity artifact | `src/Identity/<BoundedContext>/...` | Own identity-specific behavior |
| Shared entity transport primitive | `src/Libraries/Entities/Entities.Contracts` when justified | Provide shared serialization-safe entity transport primitives |
| Entity server artifact | `src/Libraries/Entities/Entities.Server` | Own EF, repositories, caching, backend services, server commands/queries, and server notification infrastructure |
| Shared platform primitive | `src/Modules/Base/...` or existing platform module convention | Provide cross-module primitives without module-specific business behavior |
| Library capability | `src/Libraries/<Library>/...` | Provide reusable library behavior with clear ownership and no app/module leakage |
| Source generator | Existing `src/Libraries/<Library>/<Library>.Generators` convention | Generate code for the owning library capability |
| Test | Existing `tests/...` convention nearest the production scope | Verify the corresponding production artifact |
| Documentation | Existing scoped `docs/...` source of truth | Record product, platform, architecture, operational, or developer guidance |
