# Shared React Web UI Roadmap

Status: Proposed  
Owner: Platform Architecture  
Scope: Web host family shared React UI roadmap  
Last reviewed: 2026-07-08

## Purpose

Centralize major reusable React Web design artifacts so multiple Vitarish React web apps can share one Web-owned UI surface while relying on the same backend for data, commands, authorization, validation, and workflows.

This document is the durable roadmap for implementing ADR-0008. Implementation PRs should update this file when scope, sequencing, package shape, app structure, host workspace shape, or enforcement rules change. GitHub issues, PR comments, and chat notes are not the source of truth for this roadmap.

## Decisions Already Made by ADR-0008

| Topic | Decision |
|---|---|
| Shared package name | `@vitarish/web-ui` |
| Initial visibility | Private repository-local package |
| Initial package location | `src/Hosts/Web/Packages/Ui` |
| Package manager | npm |
| Workspace model | npm workspace rooted at `src/Hosts/Web` |
| Lockfile source of truth | Web workspace `src/Hosts/Web/package-lock.json` |
| App model | Multiple React web apps under `src/Hosts/Web/{web-app}` consume the same backend |
| Backend rule | Backend remains the single data/workflow/API source of truth |
| UI import direction | Web apps import `@vitarish/web-ui`; `@vitarish/web-ui` does not import apps/features/backend clients |
| Host boundary | `src/Hosts` groups host families; Web-specific packages live under `src/Hosts/Web` |
| Localization handoff | App/feature code passes localized display text into shared components |
| HeroUI rule | Raw HeroUI/HeroUI Pro components remain the default; no custom design-system wrappers such as `SurfaceCard` |

## Target Host Workspace Shape

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

The current repository has the React Web app at `src/Hosts/Web`. The target shape promotes `src/Hosts/Web` to the Web host family workspace root. Implementation must move the current Web app into a child folder under `src/Hosts/Web/{web-app}` before or during workspace creation.

## Guiding Principles

- One source of truth for major reusable React Web design artifacts.
- One backend source of truth for application data, commands, validation, authorization, and business workflows.
- Host-family separation: Web packages and Web apps live under `src/Hosts/Web`; future native or other hosts get separate host-family folders.
- Presentation-only shared Web UI package; no generated API clients, feature workflows, backend contracts, host runtime dependencies, or route dependencies.
- Use raw HeroUI/HeroUI Pro components directly instead of creating custom design-system wrappers where direct HeroUI composition is sufficient.
- Centralize tokens, documented composition patterns, accessibility conventions, localization handoff rules, and small presentation-only helpers only when they do not hide HeroUI's component model.
- Incremental migration over big-bang rewrite.
- Reusable by at least two Web modules/apps or important enough to protect as a Web design artifact.
- Keep feature-specific business UI close to the owning app/feature until it becomes generic.

## Current State

The existing Web app already has a React/Next package with npm scripts, React 19, Next.js, HeroUI, HeroUI Pro, React Aria, Tailwind helpers, Font Awesome, Vitest, and validation scripts. Shared Web UI is not yet separated into a Web host package boundary.

Known reusable candidates from recent Web work include:

- account layout and widget composition rules;
- clinic state, detail-grid, and navigation-grid usage patterns;
- dashboard shell/navigation composition rules;
- empty/loading/error state patterns;
- icon usage conventions;
- form-field shells and date/input presentation conventions;
- documented raw HeroUI Pro card/list/action patterns.

The candidate list is not exhaustive. The first implementation step must inventory the current Web source tree before moving components.

## Phase 0 — Decision and Inventory

Status: In progress

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
- Current Web app target child folder name is selected.

## Phase 1 — Web Workspace Foundation

Status: Not started

Deliverables:

- Promote `src/Hosts/Web` to the private npm workspace root for the Web host family.
- Move the current Web app into a child app folder under `src/Hosts/Web/{web-app}`.
- Move Web lockfile ownership to `src/Hosts/Web/package-lock.json` through npm commands.
- Create `src/Hosts/Web/Packages/Ui/package.json` with:
  - `name: "@vitarish/web-ui"`;
  - `version: "0.1.0"`;
  - `private: true`;
  - explicit `exports` for public entry points;
  - React and design-library dependency strategy documented in the package.
- Add `src/Hosts/Web/Packages/Ui/src/index.ts` as the public export boundary.
- Add TypeScript configuration for the package.

Exit criteria:

- `npm install` succeeds from `src/Hosts/Web`.
- The existing Web app can resolve `@vitarish/web-ui`.
- No production component migration is required yet.

## Phase 2 — Package Contract and Enforcement

Status: Not started

Deliverables:

- Add package-level typecheck for `@vitarish/web-ui`.
- Add package-level component tests for exported presentation artifacts.
- Add workspace-level import-boundary validation scripts, run from `src/Hosts/Web`, that block `@vitarish/web-ui` from importing:
  - Web app route/page folders;
  - Web app feature folders;
  - generated OpenAPI clients and DTOs;
  - React Query hooks;
  - backend projects;
  - app localization catalogs/providers;
  - non-Web host implementation files.
- Add workspace-level validation that blocks web apps from deep-importing `@vitarish/web-ui/src/**`.
- Add workspace-level validation that blocks one web app from importing another web app's private UI files.
- Add workspace-level validation that flags custom wrapper proposals where raw HeroUI/HeroUI Pro components should be used directly.

Exit criteria:

- CI can prove the Web UI package is presentation-only.
- Public imports use `@vitarish/web-ui` package exports only.
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

- a generic `@vitarish/web-ui` presentation artifact; and
- an app/feature adapter that maps backend data, localization, permissions, and route actions into generic props.

Do not migrate a component into `@vitarish/web-ui` when the result would be only a custom wrapper around a raw HeroUI/HeroUI Pro component that should be used directly.

Exit criteria:

- At least one existing Web feature consumes `@vitarish/web-ui` through public exports.
- No backend/client imports exist in `@vitarish/web-ui`.
- Components or presentation artifacts have tests or usage examples.

## Phase 4 — Multi-App Web Readiness

Status: Not started

Deliverables:

- Document the recipe for creating a new React web app in the Web host workspace.
- Document how each Web app connects to the single backend through app-owned generated clients/adapters.
- Move cross-Web-app UI artifacts from the first Web app into `@vitarish/web-ui` as they become generic and do not duplicate raw HeroUI behavior.
- Confirm `@vitarish/web-ui` has no dependency on the first Web app's routes, app providers, localization catalog, generated clients, or non-Web host code.

Exit criteria:

- A future React app can be created without copying UI implementation files from the first Web app.
- Cross-Web-app design reuse flows through `@vitarish/web-ui`.
- Backend access remains app-owned and points to the single backend contract.

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
- Do not put generated API clients inside `@vitarish/web-ui`.
- Do not make `@vitarish/web-ui` resolve app routes or backend URLs.
- Do not introduce custom design-system wrappers where raw HeroUI/HeroUI Pro components should be used directly.
- Do not switch package managers in this roadmap.
- Do not publish the package before a separate versioning/release decision.

## Open Questions

| Question | Default until decided |
|---|---|
| What is the current Web app child folder name? | Choose during the Web workspace foundation implementation PR. |
| What is the second React web app name/path? | Keep placeholder `{future-web-app}` until an app-specific ADR or implementation task names it. |
| Should icons become a separate Web package later? | Keep icon helpers in `@vitarish/web-ui` until reuse pressure justifies `@vitarish/web-icons`. |
| Should theme tokens become a separate Web package later? | Keep tokens in `@vitarish/web-ui` until non-React or non-Web consumers need them. |
| Should generated backend clients become shared packages? | Keep clients app-owned unless a separate API-client package ADR is accepted. |
| Should future native hosts share UI artifacts with Web? | No by default; require a separate cross-host design-system ADR. |

## Progress Log

| Date | Change |
|---|---|
| 2026-07-08 | Created source-of-truth roadmap for ADR-0008 and multi-app shared UI target. |
| 2026-07-08 | Aligned roadmap with raw HeroUI usage rule and workspace-level boundary validation feedback. |
| 2026-07-08 | Aligned roadmap with host-family-first structure under `src/Hosts/Web`. |