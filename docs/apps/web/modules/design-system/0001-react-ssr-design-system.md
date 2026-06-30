# React SSR Design System

## Scope

App: Web host
Module: Design system
Capability: React SSR visual foundation
Related apps: Web
Related modules: Authentication, Account, Home, Knowledge

## Source of truth

`src/Hosts/Web/src/app/globals.css` owns semantic tokens for background, surface, border, text, muted text, accent, status colors, focus rings, elevation, radius, spacing, and motion. React primitives in `src/Hosts/Web/src/design-system/` consume those tokens and keep page components from repeating long visual class strings.

## Approved primitives

- `AppShell`: page background, responsive page spacing, and maximum content width.
- `HeroPanel`: prominent landing/dashboard panel with SSR-safe CSS-only decoration.
- `SurfaceCard`: HeroUI `Card` wrapper for default, elevated, and interactive surfaces.
- `SectionHeader`: reusable section title/description pattern.
- `ActionGroup`: responsive grouping for primary and secondary actions.
- `StatusBadge`: semantic status or workspace readiness badge.

## Usage rules

- Prefer semantic CSS variables and design-system primitives before adding raw Tailwind color classes.
- Keep HeroUI v3 compound components inside the surface primitive, such as `Card.Header` and `Card.Content`.
- Use CSS variables and media queries for theme, motion, and focus behavior; do not depend on client-only styling decisions during SSR.
- Keep feature components responsible for feature intent and copy. Global visual policy belongs in the design system.
- Existing `auth-bg` and `glass-card` aliases remain as compatibility classes but are formalized to the same token-backed rules as `AppShell` and `SurfaceCard`.

## Light, dark, responsive, and RTL readiness

Light and dark tokens are defined together so each semantic variable has parity. Spacing uses logical page/container primitives and `inset-inline-*` in decorative elements where direction matters, which keeps future Persian and RTL layout work from requiring page-local background fixes. Mobile layouts stack action groups and section headers; tablet and desktop layouts progressively align content horizontally.

## HeroUI Pro adoption path

Future HeroUI Pro components should be wrapped at this design-system boundary first when they provide reusable host-level layout or surface patterns. Module-specific Pro components should remain inside the owning feature and consume these tokens rather than becoming a second visual source of truth.

## Manual QA notes

- Light mode: verify `/home` and `/login` use the same token-backed app background and glass/surface card treatment.
- Dark mode: toggle the `dark` class via the theme provider and verify contrast, focus rings, and surface separation.
- Mobile: verify `/home` stacks hero actions and module cards without horizontal overflow.
- Desktop/wide: verify `/home` hero panel uses two columns and cards retain consistent spacing.
- RTL/Persian readiness: verify decorative orbs and spacing do not assume left-only layout; future copy localization remains outside these primitives.

## Current adoption

The current Web host pages and shared components consume these primitives or token-backed utility classes for authentication, account settings, journey gates, Knowledge Q&A, and the home dashboard. New UI should not reintroduce page-local `auth-bg`, `glass-card`, `bg-default-*`, or raw status-color utility decisions when a design-system primitive or semantic token covers the same need.
