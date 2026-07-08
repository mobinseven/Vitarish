# ADR-0008: Share React UI Components Through a Dedicated UI Package

Date: 2026-07-08  
Status: Proposed  
App: Multiple React Web Apps  
Module: Platform  
Capability: Shared React UI  
Owner: Platform Architecture  
Related apps: Web, future React web apps  
Related modules: Platform, Authentication, Account, Journey, Clinic, Knowledge  
Supersedes: None  
Related:
- docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md
- docs/architecture/decisions/0007-standardize-react-localization.md
- docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md

## Context

Vitarish is moving toward multiple React web applications that rely on a single backend. Those apps should share major design artifacts without copying empty/error states, navigation patterns, form composition rules, icon usage, theme bridge details, and HeroUI/HeroUI Pro usage conventions into each app.

Keeping reusable UI artifacts inside one app or scattered across feature folders makes design drift likely. A new app can unknowingly create another version of the same state pattern or composition rule, and later changes must be repeated across apps and modules. It also makes it harder to reason about which UI artifacts are safe to reuse across Authentication, Account, Journey, Clinic, Knowledge, and future modules.

The existing React Web host already uses React, Next.js, HeroUI, HeroUI Pro, React Aria, Tailwind utilities, Font Awesome, Vitest, npm, and a central React localization boundary. Shared UI must respect those conventions while preparing for additional web apps that consume the same backend through app-owned API adapters.

The single backend remains the source of truth for data, workflows, authorization, and API contracts. The shared UI package must not become a backend client, domain model package, cross-app workflow layer, or custom design-system wrapper layer.

## Decision

Create a dedicated shared React UI package named `@vitarish/ui`.

The package is the source of truth for major reusable React presentation artifacts across Vitarish React web apps:

- design tokens and theme bridge utilities;
- raw HeroUI/HeroUI Pro usage conventions and documented composition patterns;
- shared empty, loading, error, navigation, and form composition contracts;
- icon usage helpers and presentation-only composition helpers;
- reusable component contracts and usage examples when direct HeroUI usage is not enough.

Raw HeroUI and HeroUI Pro components remain the default building blocks. `@vitarish/ui` must not introduce custom design-system wrappers such as `SurfaceCard` that hide or replace HeroUI's component model. When a shared artifact is needed, it must stay presentation-only and should either document a raw HeroUI composition pattern or provide a small helper for behavior that raw HeroUI does not already provide directly.

The initial package is private and repository-local. It will live in a frontend npm workspace beside the React web apps, not inside a single app. The package can be published later only after a separate release/versioning decision.

Each React web app imports shared presentation artifacts from `@vitarish/ui`. App and feature modules keep business-specific components, API adapters, route logic, localization resolution, and workflow composition inside the app that owns them.

The roadmap source of truth for implementing this decision is `docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md`.

## Naming Decision

Use `@vitarish/ui` as the shared package name.

Rationale:

- `@vitarish/ui` is short, product-scoped, and clear at import sites.
- The package name describes presentation ownership without tying it to Next.js, a single app, or a specific third-party component library.
- The name leaves room for future sibling packages such as `@vitarish/icons`, `@vitarish/theme`, `@vitarish/testing`, or generated backend client packages if those become necessary.

Rejected names:

- `@vitarish/react-ui`: too implementation-specific for imports and duplicates the React context already implied by the package contents.
- `@vitarish/design-system`: too broad for the first increment because this ADR starts with a reusable React package, not a complete multi-platform design-system program.
- `@vitarish/web-ui`: couples reusable artifacts to one web surface instead of making the package reusable across multiple web apps.
- `@vitarish/web/components`: reads like an app-owned path rather than a package-level product boundary.

## Workspace and Package Manager Decision

Keep `npm` as the package manager for React frontend workspaces and continue using `package-lock.json` as the lockfile source of truth.

When implementation starts, create a frontend npm workspace root above individual React apps so `@vitarish/ui` is app-neutral. The existing Web app should be migrated into that workspace without changing the backend source-of-truth model.

Approved target workspace shape:

```text
src/Hosts/
  package.json              # private npm workspace root for React web apps and packages
  package-lock.json         # npm lockfile source of truth for frontend workspace
  Web/                      # existing @vitarish/web app
    package.json
  packages/
    ui/
      package.json          # name: @vitarish/ui, private: true
      src/
        index.ts
  {future-web-app}/
    package.json
```

The exact future app folder names can be decided when the second app is created. The important boundary is that shared packages live beside apps, not inside one app.

Do not switch to `pnpm`, Yarn, Turborepo, Nx, or a separate repository as part of this ADR. Those may be revisited only if workspace scale, build performance, or publishing needs justify a separate decision.

## Single Backend Rule

All React web apps rely on the same backend as the source of truth for data, commands, authorization, validation, and business workflows.

`@vitarish/ui` does not call the backend directly. It receives data, state, localized display text, handlers, and navigation intents through props, slots, or children. Each app owns its own route handlers, generated API clients, React Query hooks, and backend-facing adapters.

Shared UI may expose generic state contracts such as `isPending`, `isDisabled`, `error`, `emptyState`, or `items`, but it must not expose or depend on backend DTOs, generated client types, domain entities, or feature workflow names.

## Import Boundary Rules

### Allowed imports into `@vitarish/ui`

`@vitarish/ui` may depend on presentation-only libraries and package-local implementation files:

- `react` and React types;
- HeroUI and HeroUI Pro primitives;
- React Aria presentation primitives;
- Tailwind merge/variant helpers;
- Font Awesome React/icon packages already approved by the Web host;
- package-local CSS, tokens, types, and test helpers.

### Forbidden imports into `@vitarish/ui`

`@vitarish/ui` must not import from:

- any app route/page folder such as `src/Hosts/*/src/app/**`;
- app feature folders such as `src/Hosts/*/src/features/**`;
- generated OpenAPI clients, generated DTOs, query keys, or `apiFetch`;
- React Query hooks or feature data hooks;
- backend Domain, Application, Contracts, Infrastructure, or API projects;
- feature-specific business types such as patient, clinic workflow, account workflow, journey, or knowledge entities;
- app localization catalogs or providers directly.

Shared UI components receive display labels, descriptions, actions, direction, and localized text through props, slots, or children. They do not resolve app localization keys internally.

### Allowed imports from React web app code

React web app pages, route components, feature UI, and tests may import from the public `@vitarish/ui` exports.

Apps and features must not deep-import package internals such as `@vitarish/ui/src/**` or another app's component files. If a shared artifact needs to be reused across apps, it must be exported by `@vitarish/ui` or remain app/feature-local.

### Cross-app import rule

A React web app must not import UI implementation files from another React web app. Cross-app reuse flows through `@vitarish/ui`.

### Promotion rule

A UI artifact may move into `@vitarish/ui` only when it satisfies all of these conditions:

1. It is presentation-only.
2. It has no generated API, React Query, route, backend, or feature workflow dependency.
3. Its public props use generic UI language instead of business nouns.
4. It can be tested without feature data or backend contracts.
5. It improves reuse across at least two modules/apps or protects a major design artifact from duplication.
6. It does not create a custom wrapper around a raw HeroUI/HeroUI Pro component that should be used directly.

## Boundaries

Allowed dependencies:

- React web app presentation code may consume `@vitarish/ui`.
- Feature UI may compose `@vitarish/ui` with app-owned adapters, API data, and localized strings.
- Tests may use package-level test helpers that do not depend on feature data.

Forbidden dependencies:

- `@vitarish/ui` must not depend on app routes, feature modules, generated clients, backend contracts, or application state/query infrastructure.
- Backend projects must not depend on `@vitarish/ui`.
- Generated API clients must not import `@vitarish/ui`.
- One React web app must not import another React web app's private UI files.
- `@vitarish/ui` must not become a dumping ground for feature-specific components.
- `@vitarish/ui` must not introduce custom design-system wrappers where raw HeroUI/HeroUI Pro components should be used directly.

## Consequences

Reusable design artifacts gain one stable source of truth across multiple React web apps. New apps can start with the approved Vitarish UI conventions instead of rebuilding shared design patterns.

The implementation introduces a frontend workspace/package boundary that must be validated in CI. It also adds package export discipline: reusable artifacts need documented props, stable names, and public exports. Some existing feature components will need adapter layers so business-specific data and backend-facing behavior stay outside the shared package.

The backend remains a single source of truth. UI reuse does not change API ownership, command handling, authorization, validation, or domain boundaries.

## Alternatives Considered

### Keep shared components under `src/Hosts/Web/src/components`

Rejected. A host-local folder is better than feature duplication, but it couples reusable UI to the first Web app and does not create an app-neutral package boundary for future React web apps.

### Continue app/feature-local duplication

Rejected. It optimizes for short-term speed but increases visual drift and multiplies maintenance work for common states, form composition rules, icon patterns, and documented raw HeroUI usage patterns across apps.

### Create a separate repository immediately

Rejected. A separate repository adds versioning, publishing, authentication, CI, and release overhead before the design boundary is proven.

### Switch to pnpm or Yarn workspaces now

Rejected. The current React Web host already uses npm and a package-lock workflow. Changing package managers is unnecessary churn for this decision.

### Use HeroUI/HeroUI Pro directly everywhere

Accepted as the default for raw UI primitives, but rejected as the only shared-UI governance model. Vitarish should use raw HeroUI/HeroUI Pro components directly instead of custom design-system wrappers such as `SurfaceCard`. `@vitarish/ui` still provides value by centralizing tokens, documented composition patterns, accessibility conventions, localization handoff rules, import boundaries, and small presentation-only helpers for behavior that raw HeroUI does not already provide directly.

### Share backend-facing feature packages through the UI package

Rejected. Shared UI must stay presentation-only. Backend-facing clients, commands, DTOs, and workflows remain app/platform integration concerns, not UI package concerns.

## Validation / Enforcement

- Add package-level typecheck and tests for `@vitarish/ui` before migrating production UI artifacts.
- Add workspace-level import-boundary validation scripts, run from `src/Hosts`, that block forbidden imports into `@vitarish/ui`.
- Add workspace-level validation that blocks app/feature code from deep-importing `@vitarish/ui` internals.
- Add workspace-level validation that blocks one React web app from importing another React web app's private UI files.
- Add workspace-level validation that flags custom wrapper proposals where raw HeroUI/HeroUI Pro components should be used directly.
- Require new reusable design artifacts to be added to `@vitarish/ui` or documented as intentionally app/feature-local.
- Keep `docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md` updated as the implementation source of truth.

## Notes

This ADR establishes the shared React UI direction, naming, workspace/package-manager choice, single-backend boundary, and import rules. It does not implement the package, migrate components, create a second web app, or publish a library.