# Central Responsive Layout Policy Layer

## Goal

Create a thin layout policy layer in `@vitarish/web-ui` and migrate both React apps to it. The layer standardizes outer viewport geometry, primary scrolling, gutters, safe areas, content widths, and landmark ownership while reusing HeroUI Pro for app-shell mechanics and CSS for layout.

Planning only; implementation is not authorized in this turn.

## Scope

Included:

- `@vitarish/web-ui`, both React apps, and layout-owned code in `auth-web`, `journey-web`, and `knowledge-web`.
- Shared layout validation, Playwright coverage, CI, documentation, and one project skill.

Excluded:

- Feature grids, ordinary component spacing, dialogs, portals, virtualization, and intentional workspace-internal pane scrolling.
- Backend behavior, contracts, generated clients, persistence, authorization, secrets, native hosts, and runtime production configuration.
- `/owner/sea/**`, including initial migration and layout-lint scope.
- Bottom navigation and speculative layout strategies.

ADR-0008 remains authoritative; no new ADR is required.

## Ownership and Invariants

- HeroUI Pro `AppLayout` remains the sole owner of dashboard chrome, its `<main>`, sidebar provider, responsive navigation, keyboard/focus behavior, and optional contextual aside.
- `DashboardFrame` is a thin adapter that selects Vitarish policy; it must not recreate HeroUI mechanics or render another `<main>`.
- Every route has exactly one main landmark and one primary page-axis scroll owner. Intentional workspace panes may scroll internally without competing with document or frame scrolling.
- Apps and feature packages must not own raw full-viewport sizing, canonical outer gutters/widths, or app chrome.
- Standalone capabilities own `<main>`; capabilities rendered inside `DashboardFrame` are neutral regions.
- Layout uses logical properties, safe-area insets, and source-level overflow fixes. Do not conceal defects with global horizontal overflow suppression.
- Non-interactive primitives remain React Server Component compatible and use no JavaScript viewport measurement.
- The policy layer remains unaware of routes, authentication, localization catalogs, generated clients, and business workflows.
- Do not add a second UI or app-shell framework alongside HeroUI Pro.

Primary implementation reference: [HeroUI Pro AppLayout](https://heroui.pro/docs/react/components/app-layout).

## Scroll Models

| Model | Use | Primary page-axis scroll owner |
| --- | --- | --- |
| Document | Public pages and standalone centered states | Browser document |
| Frame page | Normal dashboard pages and framed gates | HeroUI `AppLayout` main region |
| Workspace | Full-height dashboard or standalone tools | Designated workspace root or intentional workspace panes |

`DashboardFrame` must explicitly select `page` or `workspace`; an immersive child cannot correct a still-scrolling parent main region.

## Shared Contract

### CSS

Export `@vitarish/web-ui/layout.css` and import it exactly once through each app's root global stylesheet.

Define prefixed tokens for:

- Inline gutters: `1rem`, `1.5rem` from `640px`, and `2rem` from `1024px`, plus physical safe-area insets.
- Page block spacing: `1.5rem` start on mobile, `2rem` start on desktop, and `2.5rem` end.
- Widths: `compact = 28rem`, `reading = 48rem`, `content = 80rem`, and `fluid`.

Also provide:

- `100vh` followed by `100dvh` for exact viewport roots; document layouts use minimum height and may grow.
- Required `min-inline-size: 0` and `min-block-size: 0` flex/grid boundaries.
- An `auto minmax(0, 1fr) auto` public header/main/footer grid.
- `scrollbar-gutter: stable` on persistent primary scrollers where supported.
- Package export/side-effect metadata and `viewport-fit=cover` in both Next.js apps.

### Components

| Capability | Contract |
| --- | --- |
| `PublicDocumentShell` | Neutral document root for optional header, one standalone main, and optional footer; browser document scrolls |
| `PageContainer` | Applies canonical gutters and typed `compact`, `reading`, `content`, or `fluid` width; defaults to `content` |
| `ContainedPage` | Normal framed page spacing plus `PageContainer`; accepts `PageWidth`, including `fluid` |
| `CenteredRegion` | Centers gate/loading content within available frame space without viewport units |
| `ImmersiveRegion` | Fills a frame in `workspace` mode and contains the workspace scroller or panes |
| `StandaloneDocumentPage` | Owns `<main>` for public/document content |
| `StandaloneCenteredPage` | Owns `<main>`, fills at least the viewport, centers while content fits, and falls back to document scrolling |
| `StandaloneImmersivePage` | Owns `<main>` and an exact viewport workspace boundary; inner workspace owns overflow |

`FullWidthPage` is unnecessary; use `ContainedPage width="fluid"`.

All components forward appropriate refs/HTML props, expose stable diagnostic data attributes, and prevent nested consumers from selecting `<main>` polymorphically.

### `DashboardFrame`

Add explicit policy props:

- `mobileNavigation: "sidebar" | "none"`
- `contentMode: "page" | "workspace"`

Requirements:

- Use the `<main>` and sidebar provider already rendered by HeroUI `AppLayout`.
- In `page` mode, make the HeroUI main region the primary scroller.
- In `workspace` mode, keep the HeroUI main region non-scrolling and let `ImmersiveRegion` contain workspace overflow.
- Preserve the native optional aside and standard `AppLayout.AsideTrigger`; do not recreate its state, transition, focus, or breakpoint behavior.
- Preserve HeroUI's sidebar variants, mobile behavior, native breakpoints, semantic colors, and interaction behavior.
- Keep one stable skip-link/main-focus target.

The private-practice `DashboardShell` defaults to sidebar navigation and page mode, with deliberate route overrides. Do not introduce generic strategy objects or compatibility aliases.

## Route Mapping

| Surface | Composition |
| --- | --- |
| Private-practice marketing | `PublicDocumentShell` + `StandaloneDocumentPage`; sections use `PageContainer` |
| Standalone auth, access, session, and loading states in either app | `StandaloneCenteredPage` |
| Home, account, clinic owner/user, workspace selection, and other normal dashboard pages | `DashboardShell(contentMode="page")` + `ContainedPage` |
| Wide dashboard content | `DashboardShell(contentMode="page")` + `ContainedPage width="fluid"` |
| Journey gates | `DashboardShell(contentMode="page")` + `CenteredRegion` |
| Private-practice Knowledge QA | `DashboardShell(contentMode="workspace")` + `ImmersiveRegion` |
| Knowledge QA demo public/document state | `PublicDocumentShell` + `StandaloneDocumentPage` |
| Knowledge QA demo protected workspace | `StandaloneImmersivePage` |

## Implementation

### 1. Establish the shared policy

- Inventory all in-scope route roots, outer wrappers, main landmarks, primary scrollers, public/app chrome, current viewport rules, and HeroUI behavior already supplied by `AppLayout`.
- Confirm the installed HeroUI Pro API and actual navigation/aside breakpoints before encoding tests.
- Implement the stylesheet, components, exports, `DashboardFrame` modes, native aside pass-through, app stylesheet imports, and viewport metadata.
- Add focused component/style contract tests for semantics, refs/props, widths, fallback order, safe areas, and server-compatible rendering.

### 2. Migrate in-scope surfaces

Migrate within one PR in small compiling commits:

1. Private-practice marketing and standalone states.
2. Normal dashboard pages and journey gates.
3. Private-practice Knowledge QA workspace.
4. Knowledge QA demo routes.
5. Shared `auth-web`, `journey-web`, and `knowledge-web` roots.

For each group, apply the route mapping and remove superseded wrappers, nested `<main>` elements, raw full-viewport sizing, and repeated outer width/gutter combinations. Preserve feature grids, internal workspace panes, guards, authorization, and business behavior.

### 3. Enforce the boundary

Add a syntax-aware root `lint:layout` validator over included TSX/CSS sources. It must reject outside the designated shared owners:

- Raw `<main>` elements.
- Screen/full-viewport height utilities and `100vh`/`100svh`/`100lvh`/`100dvh` declarations.
- Direct HeroUI Pro `AppLayout` imports.
- Deterministic legacy outer-container signatures removed by the migration.
- Missing or duplicate `@vitarish/web-ui/layout.css` imports in app global styles.

Use structured future exceptions containing rule, exact path, owner, reason, tracking issue, and expiry, but finish with zero exceptions in the included scope. `/owner/sea/**` remains outside the include set rather than becoming an exception.

Test valid and invalid fixtures with Node's built-in test runner and report rule ID, location, reason, and preferred shared capability. Run this validator before expensive builds and browser tests.

### 4. Add deterministic layout tests

- Add pinned root `@playwright/test` and Chromium-only configuration.
- Build and serve both production Next.js apps on ports `3000` and `3001`.
- Run a local fixture HTTP service for deterministic authentication, journey, and Knowledge responses, and point both servers to it. Browser interception alone is insufficient for SSR requests.
- Require no backend, live account, secret, tenant data, or uncontrolled external network.

Cover public document, centered, contained dashboard, mobile-sidebar, contextual-aside, dashboard-workspace, and standalone-workspace states with:

- `390x844`, `768x1024`, and `1440x900` viewports.
- English LTR and Persian RTL representatives.
- One pixel below and at the actual HeroUI navigation/aside breakpoints.
- A `320x800` reflow/enlarged-text case.

Assert one main, the expected primary scroll owner, no competing document/frame scroll, no horizontal overflow, correct fixed chrome, public header/main/footer fill, native sidebar/aside behavior, and keyboard/focus essentials.

Commit stable Linux-Chromium snapshots for representative geometry. Pin versions, await fonts, disable motion/caret, use deterministic fixtures, mask only volatile data, cap `maxDiffPixelRatio` at `0.005`, and upload reports/traces/diffs only on CI failure. Baseline updates use a separate pinned-Linux command and never run automatically in CI.

### 5. Add skill and documentation support

Create one project skill, `apply-web-layout-policy`, in the repository's canonical skill location; update an equivalent existing skill instead of duplicating it.

Its trigger description must cover planning, implementation, diagnosis, and review involving route/page shells, `DashboardFrame`/`AppLayout`, main landmarks, outer scrolling, viewport sizing, gutters/widths, public chrome, sidebar/aside behavior, centered states, and immersive workspaces.

Keep the skill concise and point it to the `@vitarish/web-ui` selection guide and `docs/codebase-map.md` rather than copying tokens or API tables. Its workflow must:

1. Preserve the requested task mode and authorization boundary.
2. Classify the surface as standalone/framed and document/frame-page/workspace.
3. Name the main owner, primary scroll owner, and `PageWidth` before changing or recommending code.
4. Reuse HeroUI mechanics and select an existing policy capability.
5. Propose a focused shared capability when evidence shows a gap; never weaken lint or add app-local viewport ownership.
6. Run the relevant validators, build, and layout tests before implementation handoff.

Add one short root `AGENTS.md` trigger and route matching cases from existing planning/implementation/review skills without copying the workflow. Validate the skill using the repository's normal skill validator plus isolated positive, negative, planning, implementation, diagnosis, and review cases.

Update the `@vitarish/web-ui` README with the selection table, scroll models, HeroUI ownership boundary, and representative compositions. Update `docs/codebase-map.md` with the canonical owner, project skill, lint, structural-test, visual-test, and baseline-update commands.

## Definition of Done

- Every in-scope route follows the mapping and has exactly one main landmark and one primary page-axis scroll owner.
- HeroUI `AppLayout` remains the sole dashboard main/sidebar/aside implementation owner.
- Public chrome, normal pages, centered states, fluid pages, and immersive workspaces behave correctly across the required viewport and direction matrix.
- Layout lint passes with zero included-scope exceptions; boundary, localization, typecheck, unit, build, structural, and visual checks pass.
- No in-scope feature retains raw full-viewport ownership, canonical outer wrappers, nested main landmarks, or direct `AppLayout` imports.
- `apply-web-layout-policy` validates, routes correctly, and references canonical sources without duplicating them.
- No backend, contract, persistence, authorization, secret, or production runtime behavior changes.
- `DashboardFrame` source breaks are migrated atomically without compatibility aliases; rollback remains code-only and data-safe.
