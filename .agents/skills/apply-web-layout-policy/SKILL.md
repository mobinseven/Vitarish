---
name: apply-web-layout-policy
description: Owns Web planning, implementation, diagnosis, and review of route shells, DashboardFrame, AppLayout, main landmarks, sidebar, aside, outer scrolling, viewport sizing, public chrome, gutters, centered states, and immersive workspaces. Do not use for feature-internal grids.
---

# Apply Web Layout Policy

Read `src/Hosts/Web/shared/web-ui/README.md` and relevant Web pointers in `docs/codebase-map.md`; then inspect only the selected primitive and composition.

## Rules

1. Preserve task mode: plan, implement, diagnose, or review.
2. Classify the surface as standalone/framed and document/frame-page/workspace scrolling.
3. Name the `<main>`, primary page-axis scroll owner, and `PageWidth`.
4. Reuse HeroUI Pro shell mechanics and the matching `@vitarish/web-ui` capability.
5. When none fits, prove the shared gap and add the smallest focused shared capability; never weaken layout lint or create app-local viewport ownership.
6. Validate with applicable layout lint, boundaries, localization, typecheck, tests, production build, and structural/visual browser checks from `docs/codebase-map.md`.

Feature grids and intentional workspace-internal pane scrolling remain out of scope unless they cause the outer violation.
