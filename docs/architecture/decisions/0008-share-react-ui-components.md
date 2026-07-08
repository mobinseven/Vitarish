# ADR-0008: Share React Web UI Components Through a Web Host Package

Date: 2026-07-08  
Status: Proposed  
Scope: Web host family  
Module: Platform  
Capability: Shared React Web UI  
Owner: Platform Architecture  
Related hosts: Web, future host families  
Related apps: Current Web app, future React web apps  
Related modules: Platform, Authentication, Account, Journey, Clinic, Knowledge  
Supersedes: None  
Related:
- docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md
- docs/architecture/decisions/0007-standardize-react-localization.md
- docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md

## Context

Vitarish is moving toward multiple React web applications that rely on a single backend. These web apps should share major reusable presentation artifacts without copying empty/error states, navigation patterns, form composition rules, icon usage, theme bridge details, and HeroUI/HeroUI Pro usage conventions into each app.

The Platform may later support other host families, such as native mobile, desktop, worker, or other host types. A shared React UI package is specific to the Web host family and must not occupy a structure that implies all host families share the same frontend package manager, runtime, component model, or UI library.

The existing React Web host already uses React, Next.js, HeroUI, HeroUI Pro, React Aria, Tailwind utilities, Font Awesome, Vitest, npm, and a central React localization boundary. Shared Web UI must respect those conventions while preparing for additional React web apps that consume the same backend through app-owned API adapters.

The single backend remains the source of truth for data, workflows, authorization, validation, and API contracts. The shared Web UI package must not become a backend client, domain model package, cross-app workflow layer, cross-host UI abstraction, or custom design-system wrapper layer.

## Decision

Create a dedicated shared React Web UI package named `@vitarish/web-ui`.

The package is the source of truth for major reusable React presentation artifacts inside the Web host family:

- design tokens and Web theme bridge utilities;
- raw HeroUI/HeroUI Pro usage conventions and documented composition patterns;
- shared empty, loading, error, navigation, and form composition contracts;
- icon usage helpers and presentation-only composition helpers;
- reusable component contracts and usage examples when direct HeroUI usage is not enough.

Raw HeroUI and HeroUI Pro components remain the default building blocks. `@vitarish/web-ui` must not introduce custom design-system wrappers such as `SurfaceCard` that hide or replace HeroUI's component model. When a shared artifact is needed, it must stay presentation-only and should either document a raw HeroUI composition pattern or provide a small helper for behavior that raw HeroUI does not already provide directly.

The initial package is private and repository-local. It will live inside the Web host workspace, beside the React web apps, not directly under the platform-wide `Hosts` root and not inside a single app. The package can be published later only after a separate release/versioning decision.

Each React web app imports shared presentation artifacts from `@vitarish/web-ui`. App and feature modules keep business-specific components, API adapters, route logic, localization resolution, and workflow composition inside the app that owns them.

The roadmap source of truth for implementing this decision is `docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md`.

## Host Workspace Decision

Use a host-family-first structure:

```text
src/Hosts/
  Web/
    package.json              # private npm workspace root for Web apps and Web packages
    package-lock.json         # npm lockfile source of truth for the Web workspace
    Packages/
      Ui/
        package.json          # name: @vitarish/web-ui, private: true
        src/
          index.ts
    {web-app}/
      package.json
    {future-web-app}/
      package.json
  {future-host-family}/
    ...
```

The `src/Hosts` folder groups host families. Each host family owns its own workspace conventions, package manager, lockfile, app folders, and host-specific shared packages. The Web host family owns the npm workspace at `src/Hosts/Web`.

The current repository already has Web app files under `src/Hosts/Web`. Implementation must migrate the current app into a child app folder under `src/Hosts/Web/{web-app}` before or during workspace creation. The exact app folder name is an implementation decision, but the final shape must reserve `src/Hosts/Web` as the Web host family workspace root.

Do not create shared React packages directly under `src/Hosts/Packages`, `src/Hosts/packages`, or `src/Hosts/Web/src/components`. The first placement implies a cross-host package, and the last placement couples shared UI to one app.

## Naming Decision

Use `@vitarish/web-ui` as the shared package name.

Rationale:

- `@vitarish/web-ui` makes the host-family scope explicit.
- The package is React/Web-specific and should not imply reuse by future native or desktop hosts.
- The name remains short and clear at import sites.
- The name leaves room for future sibling packages such as `@vitarish/web-icons`, `@vitarish/web-testing`, generated backend client packages, or non-Web host packages if those become necessary.

Rejected names:

- `@vitarish/ui`: too broad once the Platform may support native or other host families.
- `@vitarish/react-ui`: too implementation-specific and less aligned with the host-family structure than `web-ui`.
- `@vitarish/design-system`: too broad for the first increment because this ADR starts with a reusable Web package, not a complete multi-platform design-system program.
- `@vitarish/web/components`: reads like an app-owned path rather than a package-level host boundary.

## Package Manager Decision

Keep `npm` as the package manager for the Web host workspace and continue using `package-lock.json` as the Web workspace lockfile source of truth.

Do not place the npm workspace root at `src/Hosts`. Future host families may use different build systems or package managers. The Platform host root must not force native, desktop, worker, or other hosts into the Web package-management model.

Do not switch to `pnpm`, Yarn, Turborepo, Nx, or a separate repository as part of this ADR. Those may be revisited only if Web workspace scale, build performance, or publishing needs justify a separate decision.

## Single Backend Rule

All React web apps rely on the same backend as the source of truth for data, commands, authorization, validation, and business workflows.

`@vitarish/web-ui` does not call the backend directly. It receives data, state, localized display text, handlers, and navigation intents through props, slots, or children. Each web app owns its own route handlers, generated API clients, React Query hooks, and backend-facing adapters.

Shared Web UI may expose generic state contracts such as `isPending`, `isDisabled`, `error`, `emptyState`, or `items`, but it must not expose or depend on backend DTOs, generated client types, domain entities, or feature workflow names.

Future non-Web hosts may also rely on the same backend, but their UI sharing model is outside this ADR and must be decided separately.

## Import Boundary Rules

### Allowed imports into `@vitarish/web-ui`

`@vitarish/web-ui` may depend on Web presentation-only libraries and package-local implementation files:

- `react` and React types;
- HeroUI and HeroUI Pro primitives;
- React Aria presentation primitives;
- Tailwind merge/variant helpers;
- Font Awesome React/icon packages already approved by the Web host;
- package-local CSS, tokens, types, and test helpers.

### Forbidden imports into `@vitarish/web-ui`

`@vitarish/web-ui` must not import from:

- Web app route/page folders such as `src/Hosts/Web/*/src/app/**`;
- Web app feature folders such as `src/Hosts/Web/*/src/features/**`;
- generated OpenAPI clients, generated DTOs, query keys, or `apiFetch`;
- React Query hooks or feature data hooks;
- backend Domain, Application, Contracts, Infrastructure, or API projects;
- feature-specific business types such as patient, clinic workflow, account workflow, journey, or knowledge entities;
- app localization catalogs or providers directly;
- other host families such as `src/Hosts/Native/**`, `src/Hosts/Desktop/**`, or future non-Web host folders.

Shared Web UI components receive display labels, descriptions, actions, direction, and localized text through props, slots, or children. They do not resolve app localization keys internally.

### Allowed imports from React web app code

React web app pages, route components, feature UI, and tests may import from the public `@vitarish/web-ui` exports.

Apps and features must not deep-import package internals such as `@vitarish/web-ui/src/**` or another web app's component files. If a shared artifact needs to be reused across web apps, it must be exported by `@vitarish/web-ui` or remain app/feature-local.

### Cross-app and cross-host import rules

A React web app must not import UI implementation files from another React web app. Cross-web-app reuse flows through `@vitarish/web-ui`.

A Web app or Web package must not import UI implementation files from a non-Web host. Cross-host reuse requires a separate platform package and ADR.

### Promotion rule

A UI artifact may move into `@vitarish/web-ui` only when it satisfies all of these conditions:

1. It is Web presentation-only.
2. It has no generated API, React Query, route, backend, host runtime, or feature workflow dependency.
3. Its public props use generic UI language instead of business nouns.
4. It can be tested without feature data or backend contracts.
5. It improves reuse across at least two Web modules/apps or protects a major Web design artifact from duplication.
6. It does not create a custom wrapper around a raw HeroUI/HeroUI Pro component that should be used directly.

## Boundaries

Allowed dependencies:

- React web app presentation code may consume `@vitarish/web-ui`.
- Feature UI may compose `@vitarish/web-ui` with app-owned adapters, API data, and localized strings.
- Tests may use package-level test helpers that do not depend on feature data.

Forbidden dependencies:

- `@vitarish/web-ui` must not depend on app routes, feature modules, generated clients, backend contracts, application state/query infrastructure, or non-Web host implementations.
- Backend projects must not depend on `@vitarish/web-ui`.
- Generated API clients must not import `@vitarish/web-ui`.
- One React web app must not import another React web app's private UI files.
- Web packages must not import native, desktop, or other non-Web host UI implementation files.
- `@vitarish/web-ui` must not become a dumping ground for feature-specific components.
- `@vitarish/web-ui` must not introduce custom design-system wrappers where raw HeroUI/HeroUI Pro components should be used directly.

## Consequences

Reusable Web design artifacts gain one stable source of truth across multiple React web apps. New Web apps can start with the approved Vitarish Web UI conventions instead of rebuilding shared design patterns.

The structure keeps the Platform host root open for future host families. Web-specific npm, React, Next.js, HeroUI, and package-lock decisions stay inside `src/Hosts/Web` instead of leaking into all hosts.

The implementation requires moving the current Web app out of the `src/Hosts/Web` root and into a child app folder. That migration is intentional because `src/Hosts/Web` becomes the Web host family workspace root.

The implementation introduces a Web workspace/package boundary that must be validated in CI. It also adds package export discipline: reusable artifacts need documented props, stable names, and public exports. Some existing feature components will need adapter layers so business-specific data and backend-facing behavior stay outside the shared package.

The backend remains a single source of truth. Web UI reuse does not change API ownership, command handling, authorization, validation, or domain boundaries.

## Alternatives Considered

### Keep shared components under `src/Hosts/Web/src/components`

Rejected. A host-local folder is better than feature duplication, but it couples reusable UI to the first Web app and does not create an app-neutral package boundary for future React web apps.

### Put shared packages directly under `src/Hosts/packages` or `src/Hosts/Packages`

Rejected. The package is React/Web-specific. Putting it directly under the platform-wide host root makes it look cross-host and can force Web package-management assumptions onto future native or other hosts.

### Use `src/Hosts/Web/Packages` beside Web apps

Accepted. This keeps shared Web packages close to Web apps while preserving `src/Hosts` for host-family separation.

### Continue app/feature-local duplication

Rejected. It optimizes for short-term speed but increases visual drift and multiplies maintenance work for common states, form composition rules, icon patterns, and documented raw HeroUI usage patterns across apps.

### Create a separate repository immediately

Rejected. A separate repository adds versioning, publishing, authentication, CI, and release overhead before the Web design boundary is proven.

### Switch to pnpm or Yarn workspaces now

Rejected. The current React Web host already uses npm and a package-lock workflow. Changing package managers is unnecessary churn for this decision.

### Use HeroUI/HeroUI Pro directly everywhere

Accepted as the default for raw UI primitives, but rejected as the only shared-UI governance model. Vitarish should use raw HeroUI/HeroUI Pro components directly instead of custom design-system wrappers such as `SurfaceCard`. `@vitarish/web-ui` still provides value by centralizing tokens, documented composition patterns, accessibility conventions, localization handoff rules, import boundaries, and small presentation-only helpers for behavior that raw HeroUI does not already provide directly.

### Share backend-facing feature packages through the Web UI package

Rejected. Shared Web UI must stay presentation-only. Backend-facing clients, commands, DTOs, and workflows remain app/platform integration concerns, not UI package concerns.

## Validation / Enforcement

- Add package-level typecheck and tests for `@vitarish/web-ui` before migrating production UI artifacts.
- Add workspace-level import-boundary validation scripts, run from `src/Hosts/Web`, that block forbidden imports into `@vitarish/web-ui`.
- Add workspace-level validation that blocks app/feature code from deep-importing `@vitarish/web-ui` internals.
- Add workspace-level validation that blocks one React web app from importing another React web app's private UI files.
- Add workspace-level validation that blocks Web packages from importing non-Web host UI implementation files.
- Add workspace-level validation that flags custom wrapper proposals where raw HeroUI/HeroUI Pro components should be used directly.
- Require new reusable Web design artifacts to be added to `@vitarish/web-ui` or documented as intentionally app/feature-local.
- Keep `docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md` updated as the implementation source of truth.

## Non-Goals

- Do not create a second backend.
- Do not define native, desktop, worker, or other non-Web host UI package structures.
- Do not create a cross-platform design system package.
- Do not move backend contracts into the Web UI package.
- Do not put generated API clients inside `@vitarish/web-ui`.
- Do not make `@vitarish/web-ui` resolve app routes or backend URLs.
- Do not introduce custom design-system wrappers where raw HeroUI/HeroUI Pro components should be used directly.
- Do not switch package managers in this ADR.
- Do not publish the package before a separate versioning/release decision.