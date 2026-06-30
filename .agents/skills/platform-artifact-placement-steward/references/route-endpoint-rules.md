# Route and Endpoint Rules

## Module API Route Prefix

Module API routes use this prefix:

```txt
/api/modules/<module>
```

The `<module>` segment is lowercase kebab-case and identifies the module route ownership.

Nested capability routes may add a capability segment when the capability owns a distinct API surface:

```txt
/api/modules/<module>/<capability>
```

Example:

```txt
/api/modules/medical/prescription
```

## Route Artifact Placement

- Host UI navigation routes: `src/Hosts/<Host>/Common/Routes`, `src/Hosts/<Host>/src/routes`, or the host’s established route convention.
- Routable Razor host page: `src/Hosts/<Host>/Pages/<Module>/<Capability>` or the host’s established page convention.
- Routable React Web page: `src/Hosts/<Host>/src/pages/<module>/<capability>` or the host’s established route/page convention.
- React route registration/router modules: `src/Hosts/<Host>/src/router` or `src/Hosts/<Host>/src/app` by existing convention.
- React feature-local route helpers: `src/Hosts/<Host>/src/features/<module>/<capability>` only when the route helper is feature-owned and not global navigation.
- Module Interface page/composition entries: existing `src/Modules/<Module>/Interface/Pages`, `Interface/Plugins`, or capability equivalent only when the module owns the UI composition contract.
- Module API route constants shared by Client and Api: `src/Modules/<Module>/Contracts`.
- Endpoint registration methods such as `Map<Module>Api`: `src/Modules/<Module>/Api`.
- Host startup calls to endpoint registration: host or module composition root according to existing convention.

## Page Expectations

- Pages compose route-level UI, route params, loaders, metadata, authorization, layout, and route-specific state.
- Pages should consume feature components, feature hooks, or module Interface components; they should not own reusable UI or business behavior.
- Group host pages by module and capability when it makes navigation and ownership clearer.
- Razor pages should not become a replacement for `src/Modules/<Module>/Interface/Components`.
- React pages should consume feature components and feature hooks, not raw generated endpoint hooks, when feature wrappers exist.
- Pages must not own reusable module UI, domain rules, application use-case logic, generated API code, or backend endpoint details.
- Reusable host-specific React components belong in `src/Hosts/<Host>/src/components`, `src/Hosts/<Host>/src/ui`, or the host’s established equivalent.
- Feature-specific React components, hooks, and API wrappers belong under `src/Hosts/<Host>/src/features/<module>/<capability>` when capability ownership exists.

## Endpoint Mapping Expectations

- Use explicit endpoint registration methods such as `Map<Module>Api`.
- Keep endpoint handlers thin.
- Map contract DTOs to Application commands or queries at the API boundary.
- Propagate `CancellationToken` from the HTTP request through Application calls.
- Return structured failures.
- Avoid domain/entity leakage in responses.
- Avoid UI/client/Web host dependencies in API projects.

## Command and Query Expectations

- Command endpoints mutate state and must not act as data query APIs.
- Query endpoints return data and must not mutate observable state.
- Combined command/query behavior requires redesign before implementation.
