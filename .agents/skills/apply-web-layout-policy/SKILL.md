---
name: apply-web-layout-policy
description: Applies Vitarish's shared responsive Web layout policy in planning, implementation, diagnosis, and review. Use for route or page shells, DashboardFrame or HeroUI AppLayout, main landmarks, outer document/frame/workspace scrolling, viewport sizing, canonical gutters or widths, public chrome, sidebar or aside behavior, centered states, and immersive workspaces. Do not use for feature-internal grids, ordinary component spacing, dialogs, portals, virtualization, or intentional workspace pane internals when the outer layout contract is unchanged.
---

# Apply Web Layout Policy

Keep route geometry in the shared policy owner while preserving HeroUI Pro's native app-shell mechanics.

## Canonical sources

Read the layout selection guide in `src/Hosts/Web/shared/web-ui/README.md` and the Web commands and ownership pointers in `docs/codebase-map.md`. Inspect only the selected shared primitive and the affected composition after that. Do not copy their token or API tables into this skill.

## Workflow

1. Preserve the requested mode: plan, implement, diagnose, or review. Do not turn analysis into an edit without authorization.
2. Classify the surface as standalone or framed, then choose document, frame-page, or workspace scrolling.
3. Name the `<main>` owner, primary page-axis scroll owner, and `PageWidth` before changing or recommending code.
4. Reuse HeroUI Pro navigation, sidebar, aside, focus, and breakpoint mechanics. Select the existing `@vitarish/web-ui` layout capability that matches the classification.
5. If no capability fits, demonstrate the shared gap and propose the smallest focused addition to `@vitarish/web-ui`. Never weaken layout lint or introduce app-local viewport ownership.
6. For implementation handoff, run the relevant layout lint, boundary/localization checks, typecheck, unit tests, production build, and structural or visual browser tests listed in `docs/codebase-map.md`.

Keep feature grids and intentional workspace-internal pane scrolling out of scope unless they cause the outer contract violation being investigated.
