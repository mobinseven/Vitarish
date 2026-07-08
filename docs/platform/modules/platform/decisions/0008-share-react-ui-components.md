# ADR-0008: Share React UI Components Through a Dedicated UI Package

Date: 2026-07-08  
Status: Proposed  
App: Web Host  
Module: Platform  
Capability: Shared React UI  
Owner: Platform Architecture  
Related apps: Web  
Related modules: Platform, Authentication, Account, Journey, Clinic, Knowledge  
Supersedes: None  
Superseded by: None  
Related:
- docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md
- docs/platform/modules/platform/decisions/0007-standardize-react-localization.md
- docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md

## Context

The React Web host is accumulating reusable UI decisions in feature folders and host-local component folders. Examples include layout surfaces, empty/error states, navigation cards, form controls, icon composition, theme bridge details, and reusable HeroUI/HeroUI Pro wrappers.

Keeping these artifacts distributed makes design drift likely. A feature can unknowingly create a second version of the same surface or state pattern, and later changes must be repeated in multiple places. It also makes it harder to reason about which components are safe to reuse across Authentication, Account, Journey, Clinic, Knowledge, and future modules.

The Web host already uses React, Next.js, HeroUI, HeroUI Pro, React Aria, Tailwind utilities, and a central React localization boundary. Shared UI must respect those existing boundaries without coupling design primitives to generated API clients, feature workflows, backend contracts, or Web route implementation details.

## Decision

Create a dedicated shared React UI package named `@vitarish/ui`.

The package is the source of truth for major reusable React design artifacts:

- design tokens and theme bridge utilities;
- shared layout surfaces and page section primitives;
- shared buttons, states, cards, navigation primitives, and form-field shells;
- icon wrappers and presentation-only composition helpers;
- reusable component contracts and usage examples.

The initial package is private and repository-local. It will live under the Web host workspace at `src/Hosts/Web/packages/ui` when implementation starts. The package can be published later only after a separate release/versioning decision.

The Web application imports shared presentation components from `@vitarish/ui`. Feature modules keep business-specific components in their own feature folders and compose them with `@vitarish/ui` primitives.

The roadmap source of truth for implementing this decision is `docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md`.

## Naming Decision

Use `@vitarish/ui` as the shared package name.

Rationale:

- `@vitarish/ui` is short, product-scoped, and clear at import sites.
- The package name describes presentation ownership without tying it to Next.js, a single app, or a specific third-party component library.
- The name leaves room for future sibling packages such as `@vitarish/icons`, `@vitarish/theme`, or `@vitarish/testing` if those become necessary.

Rejected names:

- `@vitarish/react-ui`: too implementation-specific for imports and duplicates the React context already implied by the package contents.
- `@vitarish/design-system`: too broad for the first increment because this ADR starts with a reusable React package, not a complete multi-platform design-system program.
- `@vitarish/web-ui`: couples reusable artifacts to the Web host instead of making the package the host-consumed boundary.

## Workspace and Package Manager Decision

Keep `npm` as the package manager for the React Web host and continue using `package-lock.json` as the lockfile source of truth.

When implementation starts, add npm workspaces to `src/Hosts/Web/package.json` and place the shared package at `src/Hosts/Web/packages/ui`.

Approved workspace shape:

```text
src/Hosts/Web/
  package.json              # npm workspace root and @vitarish/web package
  package-lock.json         # npm lockfile source of truth
  packages/
    ui/
      package.json          # name: @vitarish/ui, private: true
      src/
        index.ts
```

Do not switch to `pnpm`, Yarn, Turborepo, Nx, or a separate repository as part of this ADR. Those may be revisited only if workspace scale, build performance, or publishing needs justify a separate decision.

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

- `src/Hosts/Web/src/app/**` or route/page files;
- Web feature folders such as `features/**`;
- generated OpenAPI clients, generated DTOs, query keys, or `apiFetch`;
- React Query hooks or feature data hooks;
- backend Domain, Application, Contracts, Infrastructure, or API projects;
- feature-specific business types such as patient, clinic workflow, account workflow, journey, or knowledge entities;
- the Web localization catalog or provider directly.

Shared UI components receive display labels, descriptions, actions, and localized text through props, slots, or children. They do not resolve app localization keys internally.

### Allowed imports from Web application code

Web host pages, route components, feature UI, and tests may import from the public `@vitarish/ui` exports.

Feature modules must not deep-import package internals such as `@vitarish/ui/src/**` or another feature's component files. If a shared component needs to be reused, it must be exported by `@vitarish/ui` or remain feature-local.

### Promotion rule

A component may move into `@vitarish/ui` only when it satisfies all of these conditions:

1. It is presentation-only.
2. It has no generated API, React Query, route, or feature workflow dependency.
3. Its public props use generic UI language instead of business nouns.
4. It can be tested without feature data or backend contracts.
5. It improves reuse across at least two modules or protects a major design artifact from duplication.

## Boundaries

Allowed dependencies:

- Web-host React presentation code may consume `@vitarish/ui`.
- Feature UI may compose `@vitarish/ui` with feature-specific adapters and localized strings.
- Tests may use package-level test helpers that do not depend on feature data.

Forbidden dependencies:

- `@vitarish/ui` must not depend on Web routes, feature modules, generated clients, backend contracts, or application state/query infrastructure.
- Backend projects must not depend on `@vitarish/ui`.
- Generated API clients must not import `@vitarish/ui`.
- `@vitarish/ui` must not become a dumping ground for feature-specific components.

## Consequences

Reusable design artifacts gain one stable source of truth. The Web host can migrate duplicate surfaces and controls incrementally while keeping feature-specific workflows local.

The implementation introduces a frontend package boundary that must be validated in CI. It also adds package export discipline: reusable components need documented props, stable names, and public exports. Some existing feature components will need small adapter layers so business-specific data stays outside the shared package.

## Alternatives Considered

### Keep shared components under `src/Hosts/Web/src/components`

Rejected. A host-local folder is better than feature duplication, but it does not create a package boundary, public export contract, or dependency direction that tooling can validate.

### Continue feature-local duplication

Rejected. It optimizes for short-term speed but increases visual drift and multiplies maintenance work for common surfaces, states, form shells, and icon patterns.

### Create a separate repository immediately

Rejected. A separate repository adds versioning, publishing, authentication, CI, and release overhead before the design boundary is proven.

### Switch to pnpm or Yarn workspaces now

Rejected. The current Web host already uses npm and a package-lock workflow. Changing package managers is unnecessary churn for this decision.

### Use HeroUI/HeroUI Pro directly everywhere

Rejected. Third-party primitives are implementation details. Vitarish still needs product-owned component contracts, theme decisions, accessibility conventions, localization handoff rules, and import boundaries.

## Validation / Enforcement

- Add package-level typecheck and tests for `@vitarish/ui` before migrating production components.
- Add an import-boundary validation script that blocks forbidden imports into `@vitarish/ui`.
- Add validation that blocks app/feature code from deep-importing `@vitarish/ui` internals.
- Require new reusable design artifacts to be added to `@vitarish/ui` or documented as intentionally feature-local.
- Keep `docs/platform/modules/platform/roadmaps/0001-shared-react-ui-roadmap.md` updated as the implementation source of truth.

## Notes

This ADR establishes the shared React UI direction, naming, workspace/package-manager choice, and import rules. It does not implement the package, migrate components, or publish a library.