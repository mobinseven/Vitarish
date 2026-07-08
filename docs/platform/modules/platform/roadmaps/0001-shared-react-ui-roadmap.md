# Shared React UI Roadmap

Status: Proposed  
Owner: Platform Architecture  
Source of truth for: ADR-0008 shared React UI implementation  
Last updated: 2026-07-08

## Purpose

Centralize major reusable React design artifacts so multiple Vitarish React web apps can share one product-owned UI surface while relying on the same backend for data, commands, authorization, validation, and workflows.

This document is the durable roadmap for implementing ADR-0008. Implementation PRs should update this file when scope, sequencing, package shape, app structure, or enforcement rules change. GitHub issues, PR comments, and chat notes are not the source of truth for this roadmap.

## Decisions Already Made by ADR-0008

| Topic | Decision |
|---|---|
| Shared package name | `@vitarish/ui` |
| Initial visibility | Private repository-local package |
| Initial package location | `src/Hosts/packages/ui` |
| Package manager | npm |
| Workspace model | npm workspaces rooted above React web apps |
| Lockfile source of truth | frontend workspace `package-lock.json` |
| App model | Multiple React web apps consume the same backend |
| Backend rule | Backend remains the single data/workflow/API source of truth |
| UI import direction | Apps import `@vitarish/ui`; `@vitarish/ui` does not import apps/features/backend clients |
| Localization handoff | App/feature code passes localized display text into shared components |

## Target Workspace Shape

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

The current repository has the React Web package under `src/Hosts/Web`. The target shape moves frontend package management to the shared workspace root so future web apps do not depend on the first Web app as their package owner.

## Guiding Principles

- One source of truth for major reusable React design artifacts.
- One backend source of truth for application data, commands, validation, authorization, and business workflows.
- Presentation-only shared UI package; no generated API clients, feature workflows, backend contracts, or route dependencies.
- Product-owned contracts wrap third-party primitives where design consistency matters.
- Incremental migration over big-bang rewrite.
- Reusable by at least two modules/apps or important enough to protect as a design artifact.
- Keep feature-specific business UI close to the owning app/feature until it becomes generic.

## Current State

The existing Web host already has a React/Next package with npm scripts, React 19, Next.js, HeroUI, HeroUI Pro, React Aria, Tailwind helpers, Font Awesome, Vitest, and validation scripts. Shared UI is not yet separated into an app-neutral package boundary.

Known reusable candidates from recent Web work include:

- account layout surfaces and account widgets;
- clinic state surfaces, detail grids, and navigation grids;
- dashboard shell/navigation primitives;
- empty/loading/error state patterns;
- icon wrapper conventions;
- form-field shells and date/input presentation conventions;
- card/list/action patterns built on HeroUI Pro.

The candidate list is not exhaustive. The first implementation step must inventory the current Web source tree before moving components.

## Phase 0 — Decision and Inventory

Status: In progress

Deliverables:

- ADR-0008 added and linked from platform ADR indexes.
- This roadmap added as the implementation source of truth.
- Inventory duplicated or reusable UI artifacts under `src/Hosts/Web/src/**`.
- Classify each candidate as one of:
  - `shared-now`: presentation-only and safe to move;
  - `feature-adapter-needed`: reusable shape exists but business/data coupling must be split first;
  - `app-local`: intentionally not shared;
  - `defer`: reusable candidate but not worth moving in the first package increment.

Exit criteria:

- Candidate inventory exists in this roadmap or a linked task document.
- First package increment is selected.

## Phase 1 — Frontend Workspace Foundation

Status: Not started

Deliverables:

- Add `src/Hosts/package.json` as the private npm workspace root.
- Move frontend lockfile ownership to `src/Hosts/package-lock.json` through npm commands.
- Keep the existing Web app as a workspace package under `src/Hosts/Web`.
- Create `src/Hosts/packages/ui/package.json` with:
  - `name: "@vitarish/ui"`;
  - `version: "0.1.0"`;
  - `private: true`;
  - explicit `exports` for public entry points;
  - React and design-library dependency strategy documented in the package.
- Add `src/Hosts/packages/ui/src/index.ts` as the public export boundary.
- Add TypeScript configuration for the package.

Exit criteria:

- `npm install` succeeds from `src/Hosts`.
- The existing Web app can resolve `@vitarish/ui`.
- No production component migration is required yet.

## Phase 2 — Package Contract and Enforcement

Status: Not started

Deliverables:

- Add package-level typecheck for `@vitarish/ui`.
- Add package-level component tests for exported primitives.
- Add import-boundary validation that blocks `@vitarish/ui` from importing:
  - app route/page folders;
  - app feature folders;
  - generated OpenAPI clients and DTOs;
  - React Query hooks;
  - backend projects;
  - app localization catalogs/providers.
- Add validation that blocks web apps from deep-importing `@vitarish/ui/src/**`.
- Add validation that blocks one web app from importing another web app's private UI files.

Exit criteria:

- CI can prove the UI package is presentation-only.
- Public imports use `@vitarish/ui` package exports only.

## Phase 3 — First Shared Components

Status: Not started

Recommended first increment:

- primitive layout surface;
- section/header/action area primitives;
- empty/loading/error state primitives;
- icon wrapper conventions;
- generic card/list primitives that do not reference a business module.

Migration rule:

Move only components that satisfy ADR-0008 promotion rules. If a reusable component currently depends on feature data, split it into:

- a generic `@vitarish/ui` component; and
- an app/feature adapter that maps backend data, localization, permissions, and route actions into generic props.

Exit criteria:

- At least one existing Web feature consumes `@vitarish/ui` through public exports.
- No backend/client imports exist in `@vitarish/ui`.
- Components have tests or usage examples.

## Phase 4 — Multi-App Readiness

Status: Not started

Deliverables:

- Document the recipe for creating a new React web app in the frontend workspace.
- Document how each app connects to the single backend through app-owned generated clients/adapters.
- Move cross-app UI artifacts from the first Web app into `@vitarish/ui` as they become generic.
- Confirm `@vitarish/ui` has no dependency on the first Web app's routes, app providers, localization catalog, or generated clients.

Exit criteria:

- A future React app can be created without copying UI implementation files from `src/Hosts/Web`.
- Cross-app design reuse flows through `@vitarish/ui`.
- Backend access remains app-owned and points to the single backend contract.

## Phase 5 — Governance and Optional Publishing

Status: Not started

Deliverables:

- Add component ownership and review rules.
- Add component naming and export rules.
- Add a changelog strategy for shared UI changes.
- Decide whether package publishing is needed.

Exit criteria:

- Shared UI changes are reviewable as product-level design decisions.
- Publishing is either explicitly rejected or covered by a future ADR.

## Non-Goals

- Do not create a second backend.
- Do not move backend contracts into the UI package.
- Do not put generated API clients inside `@vitarish/ui`.
- Do not make `@vitarish/ui` resolve app routes or backend URLs.
- Do not switch package managers in this roadmap.
- Do not publish the package before a separate versioning/release decision.

## Open Questions

| Question | Default until decided |
|---|---|
| What is the second React web app name/path? | Keep placeholder `{future-web-app}` until an app-specific ADR or implementation task names it. |
| Should icons become a separate package later? | Keep icon wrappers in `@vitarish/ui` until reuse pressure justifies `@vitarish/icons`. |
| Should theme tokens become a separate package later? | Keep tokens in `@vitarish/ui` until non-React or non-web consumers need them. |
| Should generated backend clients become shared packages? | Keep clients app-owned unless a separate API-client package ADR is accepted. |

## Progress Log

| Date | Change |
|---|---|
| 2026-07-08 | Created source-of-truth roadmap for ADR-0008 and multi-app shared UI target. |