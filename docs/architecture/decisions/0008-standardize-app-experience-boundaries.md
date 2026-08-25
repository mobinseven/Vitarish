# ADR-0008: Standardize App Experience Boundaries

Date: 2026-07-09
Status: Accepted
Scope: Platform-wide app documentation, React Web feature packaging, React Web experience reuse, and app boundary governance
Owner: Platform Architecture
Related hosts: Web, future host families
Related apps: Current Web app, future platform apps
Related modules: Platform, Authentication, Account, Journey, Clinic, Knowledge, Sea
Supersedes: None
Related:
- docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md
- docs/architecture/decisions/0007-standardize-react-localization.md
- docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md

## Decision

Standardize app experience governance around four connected sources of truth:

1. App documentation uses flat app-owned collections.
2. App journey maps derive from app decision trees.
3. React Web feature implementation lives behind mandatory Web workspace feature packages.
4. Generic React Web presentation and shared transport/localization infrastructure live behind narrow Web workspace package boundaries.

The decision source of truth for app experience structure is this ADR. Implementation roadmaps and task docs may plan migrations, but they must not create competing placement rules.

## App Documentation Structure

Each app owns a flat documentation root:

```text
docs/apps/<App>/
  decision-trees/
  journeys/
  tests/
  decisions/
  roadmaps/
  tasks/
  runbooks/
```

Create a collection folder when the first document in that collection exists. Do not create empty folders only for symmetry.

Files inside each collection are flat. Do not create `modules/`, `capabilities/`, feature subfolders, or any other nested folder under `docs/apps/<App>/`. Use clear filenames, numeric prefixes, tags, and indexes when grouping is needed.

App documentation responsibilities:

- `decision-trees/` owns app-level branching choices, persona-specific paths, workflow gates, and decision logic that shape app UX.
- `journeys/` owns user journey maps derived from one or more decision trees.
- `tests/` owns app-level test plans, coverage audits, hot scenarios, and journey verification docs.
- `decisions/` owns app-scoped ADRs that do not apply platform-wide.
- `roadmaps/` owns app-scoped delivery sequencing.
- `tasks/` owns app-scoped implementation slices.
- `runbooks/` owns app-scoped operational procedures.

Global indexes under `docs/indexes/` remain navigation only. They must point to the owning app document and must not become a second source of truth.

## Decision Tree to Journey to Test Traceability

Decision trees are the first app UX planning artifact when a workflow has branching, prioritization, mandatory gates, recovery paths, or persona-specific behavior.

Journey maps must cite the decision tree or trees they derive from. A journey map may cover one path, one persona, or one workflow slice, but it must not invent branching rules that are absent from its cited decision tree.

Test docs must cite the journeys they verify. A test doc must state which success, empty, failure, permission, cancellation, idempotency, concurrency, and recovery scenarios are covered or intentionally deferred.

The traceability direction is:

```text
decision tree -> journey map -> test doc
```

Do not reverse this chain. Tests can reveal missing journeys, and journeys can reveal missing decisions, but the fix is to update the owning upstream document rather than duplicate rules downstream.

## Platform Documentation Boundary

Platform module docs remain app-agnostic and module-scoped under `docs/platform/modules/<Module>/...`.

App docs must not redefine platform module behavior. When an app journey uses platform behavior, the app document links to the platform module document and explains only the app-specific presentation, sequencing, copy intent, role context, and edge-case handling.

Do not create app module mirrors under `docs/apps/<App>/modules/<Module>/...`. Existing nested app module docs are legacy content and must be migrated into the flat app collections when they are next touched or when a documentation migration task is scheduled.

## Web Workspace Package Boundaries

The Web host family owns npm workspace conventions, package manager choice, lockfile, app folders, and Web-specific shared workspace packages. Web packages live under `src/Hosts/Web/shared/*`; Web app route trees live under `src/Hosts/Web/apps/<app>/src/app/**`. Do not place Web shared workspace packages directly under `src/Hosts/packages` or inside a single app's `src/components`.

The private-practice React workflow features are owned by Web workspace packages under `src/Hosts/Web/shared/<feature>-web`. App-local workflow folders under `src/Hosts/Web/apps/private-practice-general/src/features` are forbidden; only app-owned route/shell composition such as `home` and `landing` remains there. The workflow package set is:

| Current feature folder | Target package folder | Package name |
| --- | --- | --- |
| `account` | `src/Hosts/Web/shared/account` | `@vitarish/account` |
| `auth` | `src/Hosts/Web/shared/auth` | `@vitarish/auth` |
| `clinic` | `src/Hosts/Web/shared/clinic` | `@vitarish/clinic` |
| Communications module clients and reusable UI | `src/Hosts/Web/shared/communications` | `@vitarish/communications` |
| `journey` | `src/Hosts/Web/shared/journey` | `@vitarish/journey` |
| `sea` | `src/Hosts/Web/shared/sea` | `@vitarish/sea` |

`home` and `landing` are app-owned route/shell composition folders, not feature package targets. `home` currently composes the app dashboard with app routes, app authentication state, and app localization. `landing` currently owns the app root marketing page, brand assets, and presentation sections. If either later gains reusable workflow behavior, backend API access, generated clients, or cross-app feature state, extract that behavior into the narrow owning package instead of creating a package for route composition.

`@vitarish/knowledge` remains an approved feature package and must align with these package rules as it evolves.

Feature packages own feature-specific React Web concerns:

- generated OpenAPI clients under a package-internal `api/generated` boundary;
- feature API wrappers and transport mapping under a package-internal `api/client` boundary;
- React Query hooks, query key usage, invalidation, and loading/error mapping for that feature;
- feature read models and presentation models that do not become backend business models;
- feature components, page/surface components, forms, dialogs, and workflow UI;
- feature localization namespaces and catalogs, following ADR-0007.

Each concern must stay in its own package-internal boundary. Generated code remains generated-only and must not be edited manually. Public package exports should expose feature page/surface components, feature hooks, read models, and localization namespace registration; they should not expose raw generated paths by default.

App route files stay app-owned:

```text
src/Hosts/Web/apps/<app>/src/app/**/*
```

Route files and app-owned route/shell folders compose providers, layouts, metadata, route params, navigation, and package page/surface components. They must not become workflow feature implementation folders and must not duplicate feature API wrappers, generated clients, React Query hooks, localization catalogs, or feature UI internals.

Create a dedicated shared React Web UI package named `@vitarish/ui`.

The package is the source of truth for generic reusable React presentation artifacts inside the Web host family:

- design tokens and Web theme bridge utilities;
- documented raw HeroUI and HeroUI Pro composition patterns;
- shared empty, loading, error, navigation, and form composition contracts;
- icon usage helpers and presentation-only composition helpers;
- reusable component contracts and examples when direct HeroUI usage is not enough.

Raw HeroUI and HeroUI Pro components remain the default building blocks. `@vitarish/ui` must not introduce custom design-system wrappers that hide or replace HeroUI's component model. When a shared artifact is needed, it must stay presentation-only and should either document a raw HeroUI composition pattern or provide the smallest helper for behavior that raw HeroUI does not already provide directly.

Create one Web transport package for generated-client mutators and shared browser transport mechanics. Use `@vitarish/api-client` unless a later implementation ADR chooses a narrower name. This package owns `apiFetch`, `ApiError`, CSRF/antiforgery bootstrapping, tenant host header propagation, credentials policy, base URL handling, cancellation adaptation, and transport error normalization. Feature packages consume this package for their generated clients and must not define their own `apiFetch` copies or competing CSRF, tenant header, credentials, or error-mapping behavior.

Create or use the core Web localization package described by ADR-0007 for locale/provider/formatting composition. Feature packages own their message namespaces; `@vitarish/ui` receives localized display text through props, slots, or children instead of importing feature or app catalogs.

The target Web workspace shape is:

```text
src/Hosts/
  Web/
    package.json
    package-lock.json
    shared/
      web-api-client/
        package.json
        src/
          index.ts
      web-localization/
        package.json
        src/
          index.ts
      web-ui/
        package.json
        src/
          index.ts
      account-web/
        package.json
        src/
          index.ts
      auth-web/
        package.json
        src/
          index.ts
      clinic-web/
        package.json
        src/
          index.ts
      communications-web/
        package.json
        src/
          index.ts
      journey-web/
        package.json
        src/
          index.ts
      sea-web/
        package.json
        src/
          index.ts
      knowledge-web/
        package.json
        src/
          index.ts
    apps/
      private-practice-general/
        package.json
        src/
          app/
```

## Single Backend Rule

All apps rely on the backend as the source of truth for data, commands, authorization, validation, and business workflows.

Feature packages may call the backend only through package-owned generated clients and feature API wrappers that consume the shared Web transport package. Generated clients are transport artifacts, not business-rule owners. Feature wrappers may map transport status, stable API codes, and generated DTOs into feature read models or hook states, but they must not reimplement backend authorization, validation, data transitions, or workflow rules.

`@vitarish/ui` does not call the backend directly. It receives data, state, localized display text, handlers, and navigation intents through props, slots, or children. It must not import generated clients, feature packages, `apiFetch`, backend contracts, or React Query hooks.

Each app owns its route handlers, app-level providers, app shell, app navigation, and final route composition. Apps consume feature package exports rather than deep-importing feature internals or recreating feature API/localization/UI boundaries.

Shared Web UI may expose generic state contracts such as `isPending`, `isDisabled`, `error`, `emptyState`, or `items`, but it must not expose or depend on backend DTOs, generated client types, domain entities, app decision trees, journey names, or feature workflow names.

`apiFetch` must not localize messages. It may normalize transport failures into stable machine-readable error shapes. Presentation code maps stable backend or transport codes to localized copy through the localization boundary.

## Import Boundary Rules

`@vitarish/ui` may depend on Web presentation-only libraries and package-local implementation files:

- `react` and React types;
- HeroUI and HeroUI Pro primitives;
- React Aria presentation primitives;
- Tailwind merge or variant helpers;
- Font Awesome React and icon packages already approved by the Web host;
- package-local CSS, tokens, types, and test helpers.

`@vitarish/ui` must not import from:

- Web app route or page folders;
- Web app feature folders;
- generated OpenAPI clients, generated DTOs, query keys, or `apiFetch`;
- React Query hooks or feature data hooks;
- backend Domain, Application, Contracts, Infrastructure, or API projects;
- feature packages such as `@vitarish/account`, `@vitarish/clinic`, `@vitarish/sea`, or `@vitarish/knowledge`;
- feature-specific business types such as patient, clinic workflow, account workflow, journey, or knowledge entities;
- app localization catalogs or providers directly;
- other host families such as native, desktop, worker, or future non-Web hosts.

`@vitarish/api-client` may depend only on runtime/browser transport primitives and package-local implementation files. It must not import React components, feature packages, localization catalogs, generated clients, backend projects, app route files, or `@vitarish/ui`. It owns transport behavior only: base URL, credentials, tenant header, CSRF/antiforgery, cancellation adaptation, and transport error normalization.

Feature packages may import:

- package-local generated clients, generated DTOs, query keys, and MSW mocks;
- `@vitarish/api-client` for the generated-client mutator and shared transport errors;
- `@tanstack/react-query` for feature hooks;
- `@vitarish/ui` for generic presentation artifacts;
- `@vitarish/localization` for locale, formatting, provider contracts, and namespace registration;
- other feature packages only through public exports and only when a real feature composition requires it.

Feature packages must not import:

- Web app route or page folders;
- another Web app's private UI files;
- another feature package's internal `src/**` paths;
- backend Domain, Application, Infrastructure, API, or server-only projects;
- backend business-rule implementations;
- app-owned localization catalogs or providers;
- duplicate `apiFetch` implementations or app-local fetchers for module API calls.

Generated clients, generated DTOs, generated hooks, generated query keys, and generated MSW mocks must stay package-internal by default. They must not import localization, React presentation components, app route files, feature UI components, or backend implementation code.

React Web apps may import from public `@vitarish/ui`, `@vitarish/api-client`, `@vitarish/localization`, and feature package exports. They must not deep-import package internals or another app's private UI files. Cross-web-app generic presentation reuse flows through `@vitarish/ui`; cross-web-app feature reuse flows through the owning feature package.

## Promotion Rule

A UI artifact may move into `@vitarish/ui` only when it satisfies all of these conditions:

1. It is Web presentation-only.
2. It has no generated API, React Query, route, backend, host runtime, or feature workflow dependency.
3. Its public props use generic UI language instead of business nouns.
4. It can be tested without feature data or backend contracts.
5. It satisfies a current generic Web presentation need or protects an accepted host-wide design artifact from duplication.
6. It does not create a custom wrapper around a raw HeroUI or HeroUI Pro component that should be used directly.

## Consequences

App docs become user-experience-owned instead of backend-module-shaped. Decision trees, journeys, and tests gain a clear derivation chain, which reduces duplicated UX rules and makes test coverage gaps easier to audit.

Existing nested app module docs require migration. That migration is intentionally not part of this ADR rewrite because moving all files requires link repair across indexes, audits, and visualizers. New app docs must use the flat structure immediately.

Reusable Web design artifacts gain one stable source of truth across React web apps. Feature-specific React UI, generated clients, API wrappers, hooks, read models, and localization namespaces gain package-level owners instead of being anchored to the first app. The backend remains the source of truth for data, commands, authorization, validation, and business workflows.

The single Web transport package prevents duplicated CSRF, tenant header, credentials, base URL, cancellation, and transport error behavior while still keeping display localization out of transport infrastructure.

The structure keeps the platform host root open for future host families. Web-specific npm, React, Next.js, HeroUI, Orval, TanStack Query, localization, and lockfile decisions stay inside `src/Hosts/Web` instead of leaking into all hosts.

The cost is stricter documentation discipline, a migration path for legacy app module docs, package migration work for existing app feature folders, and CI validation for app docs, feature packages, generated-client boundaries, localization boundaries, transport ownership, and Web UI import boundaries.

## Alternatives Considered

### Keep ADR-0008 as a shared UI component decision

Rejected. The prior title was too narrow for the actual decisions and hid platform-wide app documentation and app experience governance concerns.

### Split documentation structure and Web UI reuse into separate ADRs

Rejected for this correction. The immediate architecture problem is inconsistent app experience ownership. Flat app docs and shared Web presentation boundaries are both enforcement mechanisms for the same ownership rule. Future package publishing, release versioning, or host-family-specific UI models can use separate ADRs.

### Keep app docs nested by module

Rejected. App journeys frequently cross backend modules. Module-nested app docs duplicate platform taxonomy, scatter user workflows, and encourage downstream journeys to redefine upstream app decisions.

### Put every app document into one folder

Rejected. One flat folder removes nesting but loses artifact responsibility. Separate flat collections keep decision trees, journeys, tests, decisions, roadmaps, tasks, and runbooks distinct.

### Migrate every legacy app module document immediately

Deferred. The migration is necessary, but it is a content and link-repair task. This ADR establishes the rule first so future moves have a stable target.

### Create a cross-host UI package

Rejected. React Web presentation reuse is not a native, desktop, worker, or cross-host abstraction. Cross-host UI sharing requires a separate decision.

### Use raw HeroUI and HeroUI Pro directly everywhere

Accepted as the default for primitives, but rejected as the only governance model. Shared tokens, composition patterns, accessibility conventions, and small presentation-only helpers still need one Web source of truth.

### Keep feature packages optional until two apps need them

Rejected. The first React app already has feature folders that mix UI, generated clients, hooks, and localization under the app boundary. Waiting for a second app keeps the first app as the de facto source of truth and makes later package extraction riskier.

### Keep generated clients app-owned and pass backend access into feature packages through ports

Rejected. App-provided backend ports force feature packages to define API abstractions around clients they do not own, while the app remains responsible for feature transport details. Feature packages may own generated clients and wrappers as long as generated output stays package-internal and transport mechanics come from the single Web transport package.

### Duplicate `apiFetch` in each feature package

Rejected. CSRF, tenant header propagation, credentials, base URL handling, cancellation adaptation, and transport error normalization need one Web source of truth. Duplicating them would create inconsistent security and failure behavior.

### Switch package managers now

Rejected. The current React Web host already uses npm and a `package-lock.json` workflow. Changing package managers is unnecessary churn for this decision.

## Validation / Enforcement

- Add a documentation validation rule that blocks new files under `docs/apps/*/modules/**`.
- Add a documentation validation rule that blocks nested folders under `docs/apps/<App>/{decision-trees,journeys,tests,decisions,roadmaps,tasks,runbooks}/`.
- Add a documentation validation rule that journey maps cite at least one app decision tree when the journey contains branching, gates, recovery, or persona-specific behavior.
- Add a documentation validation rule that app test docs cite the journeys they verify.
- Add package-level typecheck and tests for `@vitarish/ui` before migrating production UI artifacts.
- Add Web workspace import-boundary validation that blocks forbidden imports into `@vitarish/ui`.
- Add Web workspace import-boundary validation that blocks forbidden imports into `@vitarish/api-client`.
- Add Web workspace import-boundary validation that blocks feature packages from importing app route/page folders, app-private feature folders, backend projects, another feature package's internals, or app-local fetch/localization implementations.
- Add Web workspace validation that blocks app code from deep-importing `@vitarish/ui` internals.
- Add Web workspace validation that blocks app code from deep-importing feature package internals.
- Add Web workspace validation that blocks one React Web app from importing another React Web app's private UI files.
- Add Web workspace validation that detects duplicate `apiFetch` implementations outside the Web transport package.
- Add Web workspace validation that keeps generated clients, generated DTOs, generated hooks, generated query keys, and generated MSW mocks package-internal and generated-only.
- Add React localization validation for feature package namespaces and app route composition.
- Require new generic Web design artifacts to be added to `@vitarish/ui` or documented as intentionally app-local.

## Non-Goals

- Do not migrate all existing nested app docs in this ADR rewrite.
- Do not migrate existing React feature code into packages in this ADR revision.
- Do not redefine platform module behavior inside app docs.
- Do not define native, desktop, worker, or other non-Web host UI package structures.
- Do not create a cross-platform design-system package.
- Do not move backend contracts into `@vitarish/ui`.
- Do not move backend business rules into feature packages, generated clients, localization catalogs, or `@vitarish/api-client`.
- Do not put generated API clients inside `@vitarish/ui`.
- Do not make `@vitarish/ui` resolve app routes, localization keys, backend URLs, or workflow decisions.
- Do not localize display messages in `apiFetch`, generated clients, generated DTOs, or query keys.
- Do not introduce custom design-system wrappers where raw HeroUI or HeroUI Pro components should be used directly.
- Do not switch package managers in this ADR.
- Do not publish the package before a separate versioning and release decision.
