# React Web Host Placement

Use this reference when the artifact belongs to a React/TypeScript Web host under `src/Hosts/<Host>`.

## Classification Rule

Classify React Web host artifacts before choosing a folder:

- App shell: root app, providers, router registration, global layout, error boundary, theme provider.
- Routable page: route-level component that binds URL params, page metadata, route loaders, and route-specific composition.
- Feature artifact: host-owned feature component, feature hook, feature API wrapper, mutation invalidation, or feature state.
- Host-only component: reusable component for this host only.
- Shared UI/interface artifact: component intended for reuse across hosts/apps or owned by a module/interface package.
- Generated API artifact: OpenAPI/Orval DTO, client, React Query hook, query key, MSW mock, or generated schema.
- API infrastructure: custom fetcher/mutator, auth/header/cookie handling, cancellation, base URL, and ProblemDetails mapping.
- Asset/style artifact: static asset, stylesheet, theme token bridge, or host-specific design asset.

## Placement Rules

Prefer the existing React host convention if it exists. Otherwise use these defaults:

| Artifact | Default placement | Rule |
| --- | --- | --- |
| App shell/provider/router | `src/Hosts/<Host>/src/app` or `src/Hosts/<Host>/src/router` | Host composition only; no business rules |
| Routable page | `src/Hosts/<Host>/src/pages/<RouteName>` | Compose feature UI and route state; do not own reusable logic |
| Feature component | `src/Hosts/<Host>/src/features/<Feature>/components` | Own feature-specific UI composition |
| Feature hook | `src/Hosts/<Host>/src/features/<Feature>/hooks` | Expose use-case-oriented feature behavior |
| Feature API wrapper | `src/Hosts/<Host>/src/features/<Feature>/api` | Wrap generated hooks/clients and own feature invalidation/update behavior |
| Host-only reusable component | `src/Hosts/<Host>/src/components` or `src/Hosts/<Host>/src/ui` | Keep only host-owned reusable UI here |
| Generated API/client/hook/mock | `src/Hosts/<Host>/src/api/generated` | Generated only; never edit manually |
| Custom fetcher/mutator | `src/Hosts/<Host>/src/api` | Centralize base URL, auth, cookies/tokens, headers, cancellation, and ProblemDetails mapping |
| Asset/style | `src/Hosts/<Host>/src/assets` or `src/Hosts/<Host>/src/styles` | Host-owned frontend assets and styles |
| Test | Existing host test convention, colocated `*.test.ts(x)`, or `tests/Hosts/<Host>.Tests` | Verify the nearest production scope |

## Page and Component Ownership

- Put URL-bound composition in pages.
- Put reusable host-specific UI in host `components` or `ui`.
- Put feature-specific UI and hooks under `features/<Feature>`.
- Put reusable module-owned UI in `src/Modules/<Module>/Interface` for .NET/Razor module interfaces when that is the established module UI boundary.
- Put reusable cross-host UI in `src/Interfaces/Interfaces.<Name>` or the established shared UI package.
- Do not place reusable behavior in a page only because the first consumer is a page.

## Dependency Rules

React Web host code may depend on:

- generated API clients/hooks under the same host
- feature hooks and host UI components
- contract-safe DTOs emitted from OpenAPI/generation
- frontend-safe UI, routing, state, validation, and test libraries approved by the host convention

React Web host code must not depend on:

- backend `Application`, `Domain`, `Infrastructure`, `Api`, server projects, persistence projects, or server-only libraries
- raw domain/entity state unless exposed through transport-safe contracts
- raw backend route strings when generated clients or route constants exist
- generated code that was manually edited
- business-rule implementations that belong in Domain/Application

## Generated API Rule

Generated Web host code is disposable output. Place it under `src/api/generated`, keep it out of feature/page folders, and wrap it with feature hooks before pages consume it.

Pages should consume feature hooks and feature components. They should not know raw endpoint names unless the host has not introduced feature wrappers yet.
