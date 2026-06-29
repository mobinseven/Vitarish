# Scope Classification

Before choosing a path, classify scope.

## Scope Types

- App-specific: owned by `src/Apps/<App>`.
- Module-specific: owned by `src/Modules/<Module>`.
- Host-specific: owned by `src/Hosts/<Host>`.
- React Web host-specific: owned by the React/TypeScript application under `src/Hosts/<Host>` and classified further as app shell, route/page, feature, component, hook, generated API client, asset, or host configuration.
- Identity-specific: owned by `src/Identity/<BoundedContext>`.
- Interface package: owned by `src/Interfaces/Interfaces.<Name>`.
- Shared platform primitive: owned by `src/Modules/Base`, `src/Libraries/<Library>`, or an established platform module.
- Library capability: owned by `src/Libraries/<Library>`.
- Documentation: owned by the existing scoped `docs/...` source of truth.
- Test artifact: owned by `tests/...` near the production scope it verifies.

## Scope Selection Rule

Prefer the narrowest scope that owns the behavior.

If the scope is unclear, ask for the owning app, module, host, Web host feature, identity context, interface package, library, or documentation scope unless a safe assumption is obvious.

## Scope Warnings

- Apps own app-specific behavior and must not become a dumping ground for reusable module logic.
- Host pages compose route-level UI and should not own reusable module or interface package behavior.
- React Web host pages compose routes and route-specific state; reusable React UI belongs in feature, component, module Interface, or interface package scope according to ownership.
- React Web host feature hooks/components may orchestrate generated clients, but must not become the source of business rules or backend implementation details.
- Generated Web host API/client code belongs in the host API generated folder and must not be edited manually.
- Identity behavior stays under Identity bounded contexts.
- Shared libraries and `src/Modules/Base` must not contain module-specific business behavior.
- Documentation follows the existing scoped source of truth and should not duplicate rules.
