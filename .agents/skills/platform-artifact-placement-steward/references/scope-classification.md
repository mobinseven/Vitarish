# Scope Classification

Before choosing a path, classify scope.

## Scope Types

- App-specific: owned by `src/Apps/<App>`.
- Module-specific: owned by `src/Modules/<Module>`.
- Submodule/capability-specific: owned by a nested boundary under a module, such as `Medical/Prescription`, when it has its own workflow, state, API wrapper, route group, or expected independent evolution.
- Host-specific: owned by `src/Hosts/<Host>`.
- React Web host-specific: owned by the React/TypeScript application under `src/Hosts/<Host>` and classified further as app shell, route/page, module feature, submodule/capability feature, component, hook, generated API client, asset, or host configuration.
- Module Interface UI: owned by `src/Modules/<Module>/Interface` and grouped further by submodule/capability when the UI belongs to a nested workflow.
- Identity-specific: owned by `src/Identity/<BoundedContext>`.
- Interface package: owned by `src/Interfaces/Interfaces.<Name>`.
- Shared platform primitive: owned by `src/Modules/Base`, `src/Libraries/<Library>`, or an established platform module.
- Library capability: owned by `src/Libraries/<Library>`.
- Documentation: owned by the existing scoped `docs/...` source of truth.
- Test artifact: owned by `tests/...` near the production scope it verifies.

## Scope Selection Rule

Prefer the narrowest scope that owns the behavior.

If the scope is unclear, ask for the owning app, module, submodule/capability, host, Web host feature, identity context, interface package, library, or documentation scope unless a safe assumption is obvious.

## Module/Submodule Selection Rule

A submodule/capability gets its own UI placement when it has a distinct user journey, route group, state model, generated client wrapper, command/query surface, cache invalidation pattern, or future independent evolution.

Examples:

```txt
Medical
Medical/Prescription
Medical/Appointments
Knowledge
Knowledge/Corpus
```

Do not flatten those into a single generic module UI folder when the capability owns a real workflow.

## Scope Warnings

- Apps own app-specific behavior and must not become a dumping ground for reusable module logic.
- Host pages compose route-level UI and should not own reusable module or interface package behavior.
- Module Interface components own reusable module UI and should not become host page folders.
- Submodule/capability UI should stay under the owning capability folder when it has its own workflow or state.
- React Web host pages compose routes and route-specific state; reusable React UI belongs in feature, component, module Interface, or interface package scope according to ownership.
- React Web host feature hooks/components may orchestrate generated clients, but must not become the source of business rules or backend implementation details.
- Generated Web host API/client code belongs in the host API generated folder and must not be edited manually.
- Identity behavior stays under Identity bounded contexts.
- Shared libraries and `src/Modules/Base` must not contain module-specific business behavior.
- Documentation follows the existing scoped source of truth and should not duplicate rules.
