# Modular UI Placement

Use this reference when an artifact is a UI component, page, route composition, feature hook, view model, UI service, or UI test that belongs to a module or a nested submodule/capability.

## Classification Rule

Classify UI artifacts by owner before choosing a folder:

- Host page: URL-bound page owned by a concrete host.
- Host route group: route registration or navigation grouping owned by a concrete host.
- Module UI: reusable UI owned by one product/backend module, such as Medical or Knowledge.
- Submodule/capability UI: UI owned by a nested capability inside a module, such as Medical/Prescription.
- Host feature UI: host-owned implementation of one module or submodule workflow.
- Shared interface UI: UI reused across modules, apps, or hosts with a named shared interface owner.

## Ownership Rule

Keep module UI structure aligned with the product/backend boundary. A module may contain multiple submodules/capabilities, and each capability that has its own workflow, state, API wrapper, route group, or expected independent evolution gets its own folder.

Default shape:

```txt
src/Modules/<Module>/Interface/Components/<Capability>/...
src/Modules/<Module>/Interface/Models/<Capability>/...
src/Modules/<Module>/Interface/Services/<Capability>/...
src/Hosts/<Host>/Pages/<Module>/<Capability>/...
src/Hosts/<Host>/src/pages/<module>/<capability>/...
src/Hosts/<Host>/src/features/<module>/<capability>/...
```

Example:

```txt
src/Modules/Medical/Interface/Components/Prescription/PrescriptionEditor.razor
src/Modules/Medical/Interface/Models/Prescription/PrescriptionEditorModel.cs
src/Hosts/Web/Pages/Medical/Prescription/PrescriptionPage.razor
src/Hosts/Web/src/pages/medical/prescription/PrescriptionPage.tsx
src/Hosts/Web/src/features/medical/prescription/components/PrescriptionEditor.tsx
src/Hosts/Web/src/features/medical/prescription/hooks/usePrescriptionWorkflow.ts
```

## Placement Rules

| Artifact | Default placement | Rule |
| --- | --- | --- |
| Module interface component | `src/Modules/<Module>/Interface/Components` | Use when the component is module-owned and safe for frontend/interface consumers. |
| Submodule/capability interface component | `src/Modules/<Module>/Interface/Components/<Capability>` | Use when the component belongs to one nested workflow, such as Medical/Prescription. |
| Module interface model | `src/Modules/<Module>/Interface/Models` | Keep UI state and display models frontend-safe and module-owned. |
| Submodule/capability interface model | `src/Modules/<Module>/Interface/Models/<Capability>` | Keep capability display state near the capability UI. |
| Module interface service | `src/Modules/<Module>/Interface/Services` | Support module UI without backend implementation coupling. |
| Submodule/capability interface service | `src/Modules/<Module>/Interface/Services/<Capability>` | Support one capability UI workflow without polluting the parent module. |
| Routable Razor host page | `src/Hosts/<Host>/Pages/<Module>/<Capability>` or existing host page convention | Pages compose route state, authorization, layout, and module Interface components. |
| Routable React Web page | `src/Hosts/<Host>/src/pages/<module>/<capability>` or existing route convention | Pages compose route state and feature components; no reusable logic. |
| React Web submodule/capability component/hook | `src/Hosts/<Host>/src/features/<module>/<capability>` | Keep host-owned React workflow code inside the owning module/capability. |
| Interface package component | `src/Interfaces/Interfaces.<Name>/Components` or existing shared UI package | Use only with a named shared owner and at least two real consumers or a clear platform requirement. |
| Test | Nearest existing test convention for the host, module Interface, page, or feature | Test the narrowest owning UI scope. |

## Page Rules

- Pages are composition boundaries, not reusable component libraries.
- Host pages may group by module and capability to keep navigation understandable.
- Prefer `Pages/<Module>/<Capability>` for Razor pages and `pages/<module>/<capability>` for React pages when no stronger host convention exists.
- Put reusable UI below `Interface/Components/<Capability>` or `features/<module>/<capability>/components`, not directly in a page folder.
- Put route-only view composition in the page folder only when it has no expected reuse outside that route.

## Submodule/Capability Rules

Create a capability folder when any of these are true:

- It has a distinct user workflow.
- It has distinct UI state, query keys, mutations, or cache invalidation.
- It wraps a distinct generated client/API area.
- It has a distinct route group.
- It is expected to evolve independently from the parent module.

Do not create a capability folder only for one trivial component when the parent module already has a clear small-component convention.

## Dependency Rules

Module Interface UI may depend on frontend-safe contracts, interface services, localization, validation, and UI libraries approved by the module convention.

Module Interface UI must not depend on backend Application, Domain, Infrastructure, Api, persistence, server-only services, or raw entities unless a frontend-safe contract explicitly exposes them.

Host pages may depend on module Interface components, host layout/navigation, authorization, and feature hooks. Host pages must not own business rules or import backend implementation projects.
