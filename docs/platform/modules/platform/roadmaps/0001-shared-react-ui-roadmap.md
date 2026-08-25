# Shared React Web UI Roadmap

Status: Proposed  
Owner: Platform Architecture  
Scope: Web host family shared React UI roadmap  
Last reviewed: 2026-07-10

## Purpose

Centralize major reusable React Web design artifacts and align React Web feature packaging so multiple Vitarish React web apps can share Web-owned UI, feature, transport, and localization surfaces while relying on the same backend for data, commands, authorization, validation, and workflows.

This document is the durable roadmap for implementing ADR-0008. Implementation PRs should update this file when scope, sequencing, package shape, app structure, host workspace shape, or enforcement rules change. GitHub issues, PR comments, and chat notes are not the source of truth for this roadmap.

## Decisions Already Made by ADR-0008

| Topic | Decision |
|---|---|
| Shared package name | `@vitarish/ui` |
| Initial visibility | Private repository-local package |
| Initial package location | `src/Hosts/Web/shared/ui` |
| Package manager | npm |
| Workspace model | npm workspace rooted at `src/Hosts/Web` |
| Lockfile source of truth | Web workspace `src/Hosts/Web/package-lock.json` |
| App model | Multiple React web apps under `src/Hosts/Web/apps/{web-app}` consume the same backend |
| Backend rule | Backend remains the single data/workflow/API source of truth |
| UI import direction | Web apps import `@vitarish/ui`; `@vitarish/ui` does not import apps/features/backend clients |
| Feature package rule | Current `private-practice-general` workflow feature folders become `src/Hosts/Web/shared/<feature>-web` packages; `home` and `landing` stay app-owned route/shell surfaces |
| Transport rule | One Web transport package owns `apiFetch`, CSRF, tenant header, credentials, base URL, cancellation, and transport error normalization |
| Host boundary | `src/Hosts` groups host families; Web-specific packages live under `src/Hosts/Web/shared` |
| Localization handoff | Core Web localization owns locale/provider/formatting; feature packages own message namespaces; shared components receive localized display text |
| HeroUI rule | Raw HeroUI/HeroUI Pro components remain the default; no custom design-system wrappers such as `SurfaceCard` |

## Target Host Workspace Shape

```text
src/Hosts/
  Web/
    package.json              # private npm workspace root for Web apps and Web packages
    package-lock.json         # npm lockfile source of truth for the Web workspace
    shared/
      web-api-client/
        package.json          # name: @vitarish/api-client, private: true
        src/
          index.ts
      web-localization/
        package.json          # name: @vitarish/localization, private: true
        src/
          index.ts
      web-ui/
        package.json          # name: @vitarish/ui, private: true
        src/
          index.ts
      account-web/
        package.json          # name: @vitarish/account, private: true
        src/
          index.ts
      auth-web/
        package.json          # name: @vitarish/auth, private: true
        src/
          index.ts
      clinic-web/
        package.json          # name: @vitarish/clinic, private: true
        src/
          index.ts
      journey-web/
        package.json          # name: @vitarish/journey, private: true
        src/
          index.ts
      sea-web/
        package.json          # name: @vitarish/sea, private: true
        src/
          index.ts
    apps/
      {web-app}/
        package.json
      {future-web-app}/
        package.json
  {future-host-family}/
    ...
```

The current repository uses `src/Hosts/Web` as the Web host family workspace root with apps under `src/Hosts/Web/apps/*` and shared packages under `src/Hosts/Web/shared/*`. Workflow feature extraction is complete; app-local workflow folders are forbidden. The current `home` and `landing` folders are app-owned route/shell composition and are not package targets unless reusable workflow behavior is later extracted from them.

## Guiding Principles

- One source of truth for major reusable React Web design artifacts.
- One backend source of truth for application data, commands, validation, authorization, and business workflows.
- Host-family separation: Web packages and Web apps live under `src/Hosts/Web`; future native or other hosts get separate host-family folders.
- Presentation-only shared Web UI package; no generated API clients, feature workflows, backend contracts, host runtime dependencies, or route dependencies.
- Feature-specific React Web implementation lives in feature packages, not app route trees.
- Generated clients are package-internal to the owning feature package by default.
- One Web transport package owns `apiFetch`; feature packages consume it instead of duplicating fetch infrastructure.
- Use raw HeroUI/HeroUI Pro components directly instead of creating custom design-system wrappers where direct HeroUI composition is sufficient.
- Centralize tokens, documented composition patterns, accessibility conventions, localization handoff rules, and small presentation-only helpers only when they do not hide HeroUI's component model.
- Incremental migration over big-bang rewrite.
- Keep generic Web presentation in `@vitarish/ui`; keep feature UI, hooks, generated clients, wrappers, read models, and localization namespaces in the owning feature package.

## Current State

The existing Web workspace already has React/Next apps, npm workspace scripts, React 19, Next.js, HeroUI, HeroUI Pro, React Aria, Tailwind helpers, Font Awesome, Vitest, validation scripts, `src/Hosts/Web/shared/ui`, and `src/Hosts/Web/shared/knowledge`. Workflow feature implementation for `private-practice-general` still exists under app feature folders and must migrate into feature packages.

Known package candidates from recent Web work include:

- account layout, generated-client wrappers, hooks, localization, and widget composition rules;
- clinic generated-client wrappers, state, detail-grid, and navigation-grid usage patterns;
- dashboard shell/navigation composition rules;
- empty/loading/error state patterns;
- icon usage conventions;
- form-field shells and date/input presentation conventions;
- documented raw HeroUI Pro card/list/action patterns.

The candidate list is not exhaustive. The first implementation step must inventory the current Web source tree before moving components.

## Phase 0 — Decision and Inventory

Status: Complete

Deliverables:

- ADR-0008 updated and linked from platform ADR indexes.
- This roadmap updated as the implementation source of truth.
- Inventory duplicated or reusable UI artifacts under the current Web app source tree.
- Classify each candidate as one of:
  - `shared-now`: presentation-only and safe to move;
  - `feature-adapter-needed`: reusable shape exists but business/data coupling must be split first;
  - `app-local`: intentionally not shared;
  - `defer`: reusable candidate but not worth moving in the first package increment.

Exit criteria:

- Candidate inventory exists in this roadmap or a linked task document.
- First package increment is selected.
- Current Web app target child folder name is selected: `apps/private-practice-general`.

## Phase 1 — Web Workspace Foundation

Status: Not started

Deliverables:

- Promote `src/Hosts/Web` to the private npm workspace root for the Web host family.
- Move the current Web app into a child app folder under `src/Hosts/Web/apps/{web-app}`.
- Move Web lockfile ownership to `src/Hosts/Web/package-lock.json` through npm commands.
- Create `src/Hosts/Web/shared/ui/package.json` with:
  - `name: "@vitarish/ui"`;
  - `version: "0.1.0"`;
  - `private: true`;
  - explicit `exports` for public entry points;
  - React and design-library dependency strategy documented in the package.
- Add `src/Hosts/Web/shared/ui/src/index.ts` as the public export boundary.
- Add TypeScript configuration for the package.

Exit criteria:

- `npm install` succeeds from `src/Hosts/Web`.
- The existing Web app can resolve `@vitarish/ui`.
- No production component migration is required yet.

## Phase 2 — Package Contract and Enforcement

Status: Not started

Deliverables:

- Add package-level typecheck for `@vitarish/ui`.
- Add package-level component tests for exported presentation artifacts.
- Add workspace-level import-boundary validation scripts, run from `src/Hosts/Web`, that block `@vitarish/ui` from importing:
  - Web app route/page folders;
  - Web app feature folders;
  - generated OpenAPI clients and DTOs;
  - React Query hooks;
  - backend projects;
  - app localization catalogs/providers;
  - non-Web host implementation files.
- Add workspace-level import-boundary validation that blocks `@vitarish/api-client` from importing React UI, localization catalogs, generated clients, apps, feature packages, or backend projects.
- Add workspace-level import-boundary validation that blocks feature packages from importing app route/page folders, app-private feature folders, another feature package's internals, backend projects, duplicate fetchers, or app-local localization catalogs.
- Add workspace-level validation that blocks web apps from deep-importing `@vitarish/ui/src/**`.
- Add workspace-level validation that blocks web apps from deep-importing feature package internals.
- Add workspace-level validation that blocks one web app from importing another web app's private UI files.
- Add workspace-level validation that keeps generated clients package-internal and generated-only by default.
- Add workspace-level validation that flags duplicate `apiFetch` implementations outside `@vitarish/api-client`.
- Add workspace-level validation that flags custom wrapper proposals where raw HeroUI/HeroUI Pro components should be used directly.

Exit criteria:

- CI can prove the Web UI package is presentation-only.
- Public imports use `@vitarish/ui` package exports only.
- Boundary validation runs from the Web workspace root, not from an individual app directory or from the platform-wide `src/Hosts` root.

## Phase 3 — First Shared Web UI Artifacts

Status: Not started

Recommended first increment:

- theme tokens and Web theme bridge helpers;
- empty/loading/error state contracts;
- icon usage conventions and presentation-only icon helpers;
- form-field composition helpers that do not replace raw HeroUI form components;
- documented raw HeroUI/HeroUI Pro card, list, action, and navigation patterns.

Migration rule:

Move only artifacts that satisfy ADR-0008 promotion rules. If a reusable component currently depends on feature data, split it into:

- a generic `@vitarish/ui` presentation artifact; and
- a feature package adapter that maps generated-client data, localization, permissions, and route actions into generic props.

Do not migrate a component into `@vitarish/ui` when the result would be only a custom wrapper around a raw HeroUI/HeroUI Pro component that should be used directly.

Exit criteria:

- At least one existing Web feature consumes `@vitarish/ui` through public exports.
- No backend/client imports exist in `@vitarish/ui`.
- Components or presentation artifacts have tests or usage examples.

## Phase 4 — Multi-App Web Readiness

Status: Not started

Deliverables:

- Document the recipe for creating a new React web app in the Web host workspace.
- Document how each Web app connects to the single backend through feature-owned generated clients/adapters and the shared Web transport package.
- Move cross-Web-app UI artifacts from the first Web app into `@vitarish/ui` as they become generic and do not duplicate raw HeroUI behavior.
- Confirm `@vitarish/ui` has no dependency on the first Web app's routes, app providers, feature package internals, generated clients, localization catalogs, or non-Web host code.

Exit criteria:

- A future React app can be created without copying UI implementation files from the first Web app.
- Cross-Web-app design reuse flows through `@vitarish/ui`.
- Backend access remains package-owned for each feature and points to the single backend contract through the shared Web transport package.

## Phase 5 — Governance and Optional Publishing

Status: Not started

Deliverables:

- Add component ownership and review rules.
- Add component naming and export rules.
- Add a changelog strategy for shared Web UI changes.
- Decide whether package publishing is needed.

Exit criteria:

- Shared Web UI changes are reviewable as product-level design decisions.
- Publishing is either explicitly rejected or covered by a future ADR.

## Non-Goals

- Do not create a second backend.
- Do not define native, desktop, worker, or other non-Web host UI package structures.
- Do not create a cross-platform design system package.
- Do not move backend contracts into the Web UI package.
- Do not put generated API clients inside `@vitarish/ui`.
- Do not make `@vitarish/ui` resolve app routes or backend URLs.
- Do not introduce custom design-system wrappers where raw HeroUI/HeroUI Pro components should be used directly.
- Do not switch package managers in this roadmap.
- Do not publish the package before a separate versioning/release decision.

## Implementation Decisions (ADR-0008 Plan)

| Decision | Value |
|---|---|
| Current Web app child folder | `src/Hosts/Web/apps/private-practice-general` |
| Second app child folder | `src/Hosts/Web/apps/knowledge-qa-demo` |
| Private-practice doc root | `docs/apps/private-practice-general` |
| Knowledge QA demo doc root | `docs/apps/knowledge-qa-demo` |
| Former app identity | `clinic-owner` — historical reference only |
| Private-practice npm package | `@vitarish/private-practice-general` |
| Knowledge QA demo npm package | `@vitarish/knowledge-qa-demo` |

## Open Questions

| Question | Default until decided |
|---|---|
| Should icons become a separate Web package later? | Keep icon helpers in `@vitarish/ui` until reuse pressure justifies `@vitarish/web-icons`. |
| Should theme tokens become a separate Web package later? | Keep tokens in `@vitarish/ui` until non-React or non-Web consumers need them. |
| Should generated backend clients become shared packages? | Closed by ADR-0008: generated clients are owned by feature packages by default, with `apiFetch` owned by the shared Web transport package. |
| Should future native hosts share UI artifacts with Web? | No by default; require a separate cross-host design-system ADR. |

## Progress Log

| Date | Change |
|---|---|
| 2026-07-08 | Created source-of-truth roadmap for ADR-0008 and multi-app shared UI target. |
| 2026-07-08 | Aligned roadmap with raw HeroUI usage rule and workspace-level boundary validation feedback. |
| 2026-07-08 | Aligned roadmap with host-family-first structure under `src/Hosts/Web`. |
| 2026-07-09 | Closed open questions: current app = `private-practice-general`, second app = `knowledge-qa-demo`. |
| 2026-07-09 | Phase 0 marked complete. Implementation decisions recorded. |
| 2026-07-10 | Aligned roadmap with ADR-0008 package-per-feature rule, shared Web transport ownership, and ADR-0007 localization composition. |
