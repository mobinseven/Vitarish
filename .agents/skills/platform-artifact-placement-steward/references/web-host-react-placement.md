# React Web Host Placement

Use this reference when the artifact belongs to a React/TypeScript Web host under `src/Hosts/<Host>`.

## Classification Rule

Classify React Web host artifacts before choosing a folder:

- App shell: root app, providers, router registration, global layout, error boundary, theme provider.
- Routable page: route-level component that binds URL params, page metadata, route loaders, and route-specific composition.
- Module feature artifact: host-owned React artifact that presents one backend/product module, such as Medical or Knowledge.
- Submodule/capability feature artifact: host-owned React artifact that presents a nested module capability, such as Medical/Prescription, Medical/Appointments, or Knowledge/Corpus.
- Host-only component: reusable component for this host only and not owned by a module or submodule.
- Shared UI/interface artifact: component intended for reuse across hosts/apps or owned by a module/interface package.
- Generated API artifact: OpenAPI/Orval DTO, client, React Query hook, query key, MSW mock, or generated schema.
- API infrastructure: custom fetcher/mutator, auth/header/cookie handling, cancellation, base URL, and ProblemDetails mapping.
- Asset/style artifact: static asset, stylesheet, theme token bridge, or host-specific design asset.

## Modular Feature Ownership Rule

React Web host feature folders must preserve modular ownership. Do not flatten unrelated modules into one `features` bucket or mix module capabilities only because they share UI technology.

Use this hierarchy unless the host already has a stronger convention:

```txt
src/Hosts/<Host>/src/pages/<module>/...
src/Hosts/<Host>/src/pages/<module>/<submodule-or-capability>/...
src/Hosts/<Host>/src/features/<module>/...
src/Hosts/<Host>/src/features/<module>/<submodule-or-capability>/...
```

Examples:

```txt
src/Hosts/<Host>/src/pages/medical/prescription/PrescriptionPage.tsx
src/Hosts/<Host>/src/features/medical/components/...
src/Hosts/<Host>/src/features/knowledge/components/...
src/Hosts/<Host>/src/features/medical/prescription/components/...
src/Hosts/<Host>/src/features/medical/prescription/hooks/...
src/Hosts/<Host>/src/features/knowledge/corpus/api/...
```

The module or submodule segment should map to the owning product/backend boundary. For example, `Medical` and `Knowledge` must stay apart; `Medical/Prescription` should not be mixed into generic `medical/components` if Prescription has its own workflow, state, generated client wrapper, route group, or future independent evolution.

## Placement Rules

Prefer the existing React host convention if it exists. Otherwise use these defaults:

| Artifact | Default placement | Rule |
| --- | --- | --- |
| App shell/provider/router | `src/Hosts/<Host>/src/app` or `src/Hosts/<Host>/src/router` | Host composition only; no business rules |
| Routable React Web page | `src/Hosts/<Host>/src/pages/<module>/<capability>` or route-group equivalent | Compose feature UI and route state; do not own reusable logic |
| Module feature component | `src/Hosts/<Host>/src/features/<module>/components` | Own React UI for one module only |
| Module feature hook | `src/Hosts/<Host>/src/features/<module>/hooks` | Expose use-case-oriented behavior for one module only |
| Module feature API wrapper | `src/Hosts/<Host>/src/features/<module>/api` | Wrap generated hooks/clients for one module only |
| Submodule/capability component | `src/Hosts/<Host>/src/features/<module>/<capability>/components` | Keep nested capability UI separate from the parent module’s generic UI |
| Submodule/capability hook | `src/Hosts/<Host>/src/features/<module>/<capability>/hooks` | Keep capability state, query composition, and invalidation local to that capability |
| Submodule/capability API wrapper | `src/Hosts/<Host>/src/features/<module>/<capability>/api` | Wrap generated hooks/clients for that capability and keep cache ownership local |
| Host-only reusable component | `src/Hosts/<Host>/src/components` or `src/Hosts/<Host>/src/ui` | Keep only host-owned reusable UI here |
| Generated API/client/hook/mock | `src/Hosts/<Host>/src/api/generated` | Generated only; never edit manually |
| Custom fetcher/mutator | `src/Hosts/<Host>/src/api` | Centralize base URL, auth, cookies/tokens, headers, cancellation, and ProblemDetails mapping |
| Asset/style | `src/Hosts/<Host>/src/assets` or `src/Hosts/<Host>/src/styles` | Host-owned frontend assets and styles |
| Test | Existing host test convention, colocated `*.test.ts(x)`, or `tests/Hosts/<Host>.Tests` | Verify the nearest module/submodule/page/component scope |

## Page and Component Ownership

- Put URL-bound composition in pages.
- Put reusable host-specific UI in host `components` or `ui`.
- Put module-specific React UI and hooks under `features/<module>`.
- Put submodule/capability-specific React UI and hooks under `features/<module>/<capability>`.
- Put reusable module-owned UI in `src/Modules/<Module>/Interface` for .NET/Razor module interfaces when that is the established module UI boundary.
- Put reusable cross-host UI in `src/Interfaces/Interfaces.<Name>` or the established shared UI package.
- Do not place `Medical` components under `Knowledge`, `Knowledge` components under `Medical`, or `Medical/Prescription` components in a generic/shared folder unless they are genuinely shared and have a named shared owner.
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
- sibling module/submodule internals; extract a shared owner instead of cross-importing internals

## Generated API Rule

Generated Web host code is disposable output. Place it under `src/api/generated`, keep it out of feature/page folders, and wrap it with feature hooks before pages consume it.

Pages should consume feature hooks and feature components. They should not know raw endpoint names unless the host has not introduced feature wrappers yet.
