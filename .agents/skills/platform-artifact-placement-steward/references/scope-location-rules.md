# Scope Location Rules

## App scope

Use `src/Apps/<App>` for behavior that belongs to one app boundary and should not be reused as platform/module behavior.

## Module scope

Use `src/Modules/<Module>` for reusable module-owned domain, application, infrastructure, interface, and composition artifacts.

## Host scope

Use `src/Hosts/<Host>` for route-level composition, host routes, layouts, and host-specific wiring.

## Identity scope

Use `src/Identity/<BoundedContext>` for identity-owned behavior, entities, flows, policies, and persistence.

## Shared platform scope

Use `src/Libraries` or `src/Modules/Base` only for stable cross-module primitives with no module-specific behavior.

## Test scope

Use `tests/...` and mirror the production ownership as closely as the existing test convention allows.

## Documentation scope

Use `docs/apps/<app>/modules/<module>/...` for app/module docs, `docs/platform/modules/<module>/...` for platform module docs, and `docs/platform/architecture/...` for global architecture rules.
