# Artifact Placement Rules

Classify every new or changed artifact before adding it.

| Artifact type | Preferred location | Responsibility |
| --- | --- | --- |
| Domain entity, enum, pure model, pure rule | `src/Modules/<Module>/Domain` | Own module state and pure business behavior |
| Read/write DTO or descriptor | `src/Modules/<Module>/Domain/Dto`, `Domain/Models`, or existing module convention | Carry data across explicit boundaries without business behavior |
| Application command | `src/Modules/<Module>/Application/Commands` | Change state through a single use case |
| Application query | `src/Modules/<Module>/Application/Queries` | Return data without changing observable state |
| Application contract/port | `src/Modules/<Module>/Application/Abstractions` or `Application/Contracts` | Define stable dependencies needed by application logic |
| Application event | `src/Modules/<Module>/Application/Events` | Represent use-case-level notifications or integration points |
| Infrastructure adapter | `src/Modules/<Module>/Infrastructure` | Implement application contracts with concrete technology |
| Repository | `src/Modules/<Module>/Infrastructure/Repositories` | Persist and retrieve one persistence concern only |
| DbContext/data mapping | `src/Modules/<Module>/Infrastructure/Data` | Own EF and persistence mapping details |
| Background job | `src/Modules/<Module>/Infrastructure/BackgroundJobs` | Run infrastructure-backed asynchronous work |
| Reusable module UI | `src/Modules/<Module>/Interface/Components` | Render reusable components without owning host routes |
| UI-facing interface model/service | `src/Modules/<Module>/Interface/Models` or `Interface/Services` | Support reusable module UI only |
| Module DI | `src/Modules/<Module>/Interface/DependencyInjection`, `Infrastructure/DependencyInjection`, or `Module` | Register the owning layer’s services |
| Module composition | `src/Modules/<Module>/Module` | Package the module layer projects for aggregation |
| Routable host page | `src/Hosts/Main/Pages` | Compose route-level UI |
| Route constants/builders | `src/Hosts/Main/Common/Routes` | Centralize host-owned navigation paths |
| Shared platform primitive | `src/Libraries` or `src/Modules/Base` | Provide cross-module primitives without module-specific behavior |
| App-specific artifact | `src/Apps/<App>` | Own behavior specific to that app boundary |
| Identity artifact | `src/Identity/<BoundedContext>` | Own identity-specific behavior |
| Test | `tests/...` | Verify the corresponding production artifact |
| Documentation | `docs/...` | Record product, platform, architectural, operational, or developer guidance |
