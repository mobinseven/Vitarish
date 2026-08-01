---
name: platform-web-layout-policy
description: Owns platform Web route shells, main/scroll ownership, viewport sizing, public chrome, centered states, and immersive workspaces. Skip feature-internal grids.
---

# Platform Web Layout Policy

Read `src/Hosts/Web/shared/web-ui/README.md` and relevant Web pointers in `docs/codebase-map.md`; then inspect only the selected platform primitive and host composition.

## Rules

1. Preserve task mode: plan, implement, diagnose, or review.
2. Classify the surface as standalone/framed and document/frame-page/workspace scrolling.
3. Name the `<main>`, primary page-axis scroll owner, and shared width policy.
4. Reuse HeroUI Pro shell mechanics and the matching `@vitarish/web-ui` capability across hosts.
5. When none fits, prove the platform gap and add the smallest focused shared capability; never weaken layout lint or create host-local viewport ownership.
6. Validate with applicable layout lint, boundaries, localization, typecheck, tests, production build, and structural/visual browser checks from `docs/codebase-map.md`.

Feature grids and intentional workspace-internal pane scrolling remain out of scope unless they cause the outer platform-layout violation.
