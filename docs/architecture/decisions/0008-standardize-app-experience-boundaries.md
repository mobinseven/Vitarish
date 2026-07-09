# ADR-0008: Standardize App Experience Boundaries

Date: 2026-07-09
Status: Accepted
Scope: Platform-wide app documentation, React Web experience reuse, and app boundary governance
Owner: Platform Architecture
Related hosts: Web, future host families
Related apps: Current Web app, future platform apps
Related modules: Platform, Authentication, Account, Journey, Clinic, Knowledge
Supersedes: None
Related:
- docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md
- docs/architecture/decisions/0007-standardize-react-localization.md
- docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md

## Decision

Standardize app experience governance around three connected sources of truth:

1. App documentation uses flat app-owned collections.
2. App journey maps derive from app decision trees.
3. Shared React Web UI lives behind a Web-host package boundary.

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

## Web UI Package Boundary

Create a dedicated shared React Web UI package named `@vitarish/web-ui`.

The package is the source of truth for reusable React presentation artifacts inside the Web host family:

- design tokens and Web theme bridge utilities;
- documented raw HeroUI and HeroUI Pro composition patterns;
- shared empty, loading, error, navigation, and form composition contracts;
- icon usage helpers and presentation-only composition helpers;
- reusable component contracts and examples when direct HeroUI usage is not enough.

Raw HeroUI and HeroUI Pro components remain the default building blocks. `@vitarish/web-ui` must not introduce custom design-system wrappers that hide or replace HeroUI's component model. When a shared artifact is needed, it must stay presentation-only and should either document a raw HeroUI composition pattern or provide the smallest helper for behavior that raw HeroUI does not already provide directly.

The initial package is private and repository-local. It lives inside the Web host workspace:

```text
src/Hosts/
  Web/
    package.json
    package-lock.json
    shared/
      web-ui/
        package.json
        src/
          index.ts
      knowledge-web/
        package.json
        src/
          index.ts
    <web-app>/
      package.json
```

The Web host family owns npm workspace conventions, package manager choice, lockfile, app folders, and Web-specific shared workspace packages. Do not place Web shared workspace packages directly under `src/Hosts/packages` or inside a single app's `src/components`.

Feature-specific shared Web packages may live under `src/Hosts/Web/shared/<feature>-web` when the reusable artifact is not generic enough for `@vitarish/web-ui` and at least two Web apps consume it. Their npm package names use the form `@vitarish/<feature>-web`. The first approved feature package is `@vitarish/knowledge-web` for normal-user Knowledge QA UI and state orchestration.

Feature-specific shared Web packages may define feature read models, React state orchestration, React Query hooks, and port interfaces for app-provided backend access. They must not import app route files, app feature folders, generated API clients, `apiFetch`, app localization providers/catalogs, backend code, or another app's private files.

## Single Backend Rule

All apps rely on the backend as the source of truth for data, commands, authorization, validation, and business workflows.

`@vitarish/web-ui` does not call the backend directly. It receives data, state, localized display text, handlers, and navigation intents through props, slots, or children. Shared feature packages also do not call backend transport directly; they receive backend access through explicit app-provided ports. Each app owns its route handlers, generated API clients, `apiFetch` adapters, localization mapping, and final workflow composition.

Shared Web UI may expose generic state contracts such as `isPending`, `isDisabled`, `error`, `emptyState`, or `items`, but it must not expose or depend on backend DTOs, generated client types, domain entities, app decision trees, journey names, or feature workflow names.

## Import Boundary Rules

`@vitarish/web-ui` may depend on Web presentation-only libraries and package-local implementation files:

- `react` and React types;
- HeroUI and HeroUI Pro primitives;
- React Aria presentation primitives;
- Tailwind merge or variant helpers;
- Font Awesome React and icon packages already approved by the Web host;
- package-local CSS, tokens, types, and test helpers.

`@vitarish/web-ui` must not import from:

- Web app route or page folders;
- Web app feature folders;
- generated OpenAPI clients, generated DTOs, query keys, or `apiFetch`;
- React Query hooks or feature data hooks;
- backend Domain, Application, Contracts, Infrastructure, or API projects;
- feature-specific business types such as patient, clinic workflow, account workflow, journey, or knowledge entities;
- app localization catalogs or providers directly;
- other host families such as native, desktop, worker, or future non-Web hosts.

React Web apps may import from public `@vitarish/web-ui` exports and approved shared feature package exports. They must not deep-import package internals or another app's private UI files. Cross-web-app generic presentation reuse flows through `@vitarish/web-ui`; cross-web-app feature reuse flows through an approved shared feature package.

## Promotion Rule

A UI artifact may move into `@vitarish/web-ui` only when it satisfies all of these conditions:

1. It is Web presentation-only.
2. It has no generated API, React Query, route, backend, host runtime, or feature workflow dependency.
3. Its public props use generic UI language instead of business nouns.
4. It can be tested without feature data or backend contracts.
5. It improves reuse across at least two Web modules or apps, or protects a major Web design artifact from duplication.
6. It does not create a custom wrapper around a raw HeroUI or HeroUI Pro component that should be used directly.

## Consequences

App docs become user-experience-owned instead of backend-module-shaped. Decision trees, journeys, and tests gain a clear derivation chain, which reduces duplicated UX rules and makes test coverage gaps easier to audit.

Existing nested app module docs require migration. That migration is intentionally not part of this ADR rewrite because moving all files requires link repair across indexes, audits, and visualizers. New app docs must use the flat structure immediately.

Reusable Web design artifacts gain one stable source of truth across React web apps. The backend remains the source of truth for data, commands, authorization, validation, and business workflows.

The structure keeps the platform host root open for future host families. Web-specific npm, React, Next.js, HeroUI, and lockfile decisions stay inside `src/Hosts/Web` instead of leaking into all hosts.

The cost is stricter documentation discipline, a migration path for legacy app module docs, and CI validation for app docs and Web UI import boundaries.

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

### Switch package managers now

Rejected. The current React Web host already uses npm and a `package-lock.json` workflow. Changing package managers is unnecessary churn for this decision.

## Validation / Enforcement

- Add a documentation validation rule that blocks new files under `docs/apps/*/modules/**`.
- Add a documentation validation rule that blocks nested folders under `docs/apps/<App>/{decision-trees,journeys,tests,decisions,roadmaps,tasks,runbooks}/`.
- Add a documentation validation rule that journey maps cite at least one app decision tree when the journey contains branching, gates, recovery, or persona-specific behavior.
- Add a documentation validation rule that app test docs cite the journeys they verify.
- Add package-level typecheck and tests for `@vitarish/web-ui` before migrating production UI artifacts.
- Add Web workspace import-boundary validation that blocks forbidden imports into `@vitarish/web-ui`.
- Add Web workspace validation that blocks app code from deep-importing `@vitarish/web-ui` internals.
- Add Web workspace validation that blocks one React Web app from importing another React Web app's private UI files.
- Require new reusable Web design artifacts to be added to `@vitarish/web-ui` or documented as intentionally app-local.

## Non-Goals

- Do not migrate all existing nested app docs in this ADR rewrite.
- Do not redefine platform module behavior inside app docs.
- Do not define native, desktop, worker, or other non-Web host UI package structures.
- Do not create a cross-platform design-system package.
- Do not move backend contracts into `@vitarish/web-ui`.
- Do not put generated API clients inside `@vitarish/web-ui`.
- Do not make `@vitarish/web-ui` resolve app routes, localization keys, backend URLs, or workflow decisions.
- Do not introduce custom design-system wrappers where raw HeroUI or HeroUI Pro components should be used directly.
- Do not switch package managers in this ADR.
- Do not publish the package before a separate versioning and release decision.
