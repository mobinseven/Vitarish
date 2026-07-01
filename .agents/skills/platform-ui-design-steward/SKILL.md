---
name: platform-ui-design-steward
description: Enforce the React Web host's visual policy. Current policy: HeroUI v3.2.1 off-the-shelf appearance only. Removes custom design identity, glassmorphism, custom CSS tokens, visual wrapper primitives, and component-level visual class overrides unless a narrow functional exception is approved.
---

# Platform UI Design Steward

## Purpose

Use this skill to keep every user-facing React Web host screen visually aligned with the current HeroUI v3.2.1 on-the-shelf look.

This skill is now an **off-the-shelf framework fidelity steward**, not a custom design-system invention skill. Its job is to prevent the Web host from drifting into a separate Vitarish visual language unless a future accepted architecture decision explicitly changes that policy.

The default decision is:

> Use HeroUI's built-in visual language exactly. Do not add a parallel product visual identity.

## Source References

Always apply these references together:

1. `references/design-rules.md` — product-level design policy. This is intentionally abstract and defines the off-the-shelf visual intent.
2. `references/component-patterns.md` — concrete implementation rules for HeroUI v3.2.1, Tailwind v4, Next.js, and current Web host code.
3. `references/heroui-v3.2.1-off-the-shelf-reset.md` — exhaustive reset inventory and code-removal rules for eliminating the existing custom visual layer.

## Verified HeroUI v3.2.1 Baseline

The following facts are embedded into this skill from the official HeroUI v3.2.1 documentation checked on 2026-07-01. Do not rely on memory when these facts conflict with code in the repo.

- HeroUI docs currently identify the Web docs version as `v 3.2.1`.
- HeroUI v3 requires React 19+ and Tailwind CSS v4.
- HeroUI quick install for React Web is:
  - `@heroui/styles`
  - `@heroui/react`
- The main CSS baseline is exactly:
  - `@import "tailwindcss";`
  - `@import "@heroui/styles";`
- Import order matters: Tailwind must be imported before HeroUI styles.
- HeroUI v3 dark mode is CSS-driven; components read theme variables from the root element.
- A HeroUI provider is not needed for HeroUI v3 styling.
- Built-in dark mode is activated with `.dark` or `data-theme="dark"` on `<html>`.
- The page canvas must use `bg-background text-foreground`.
- In Next.js App Router, use `next-themes` only as the theme class controller when persistence/system preference support is needed.
- The documented Next.js provider shape is `ThemeProvider` from `next-themes` with `attribute="class"`, `defaultTheme="system"`, `enableSystem`, and `disableTransitionOnChange`.
- Because `next-themes` mutates `<html>` before hydration, root layout keeps `suppressHydrationWarning` on `<html>`.
- HeroUI styling mechanisms include `className`, inline `style`, BEM/global CSS overrides, wrapper components, CSS-in-JS, and custom theme variables. These are customization mechanisms. Under the current policy, visual use of those mechanisms is prohibited unless an explicit exemption is approved.

Official docs used for these embedded rules:

- https://heroui.com/en/docs/react/getting-started/quick-start
- https://heroui.com/en/docs/react/getting-started/dark-mode
- https://heroui.com/en/docs/react/getting-started/styling
- https://heroui.com/en/docs/react/getting-started/theming

## Use When

Use this skill when creating, modifying, or reviewing anything under `src/Hosts/Web/` that affects user-visible UI, including:

- React pages and layouts.
- HeroUI components.
- Page shells, navigation, app bars, sidebars, login/account screens, module pages, dashboards, tables, forms, empty states, alerts, badges, cards, dialogs, and loading states.
- `src/Hosts/Web/src/app/globals.css`.
- `src/Hosts/Web/src/app/layout.tsx`.
- `src/Hosts/Web/src/components/providers/*`.
- Any file importing from `@/design-system`.
- Any file using `className`, inline `style`, CSS variables, custom BEM overrides, Tailwind color utilities, custom shadows, custom radii, gradients, blur, glass, or transitions.

Apply it especially when:

- Removing the previous custom glassmorphism design.
- Refactoring `ds-*`, `glass-card`, `auth-bg`, `text-muted`, `--surface`, `--accent`, `color-mix`, `oklch`, or any custom visual token usage.
- Reviewing whether a component looks like HeroUI's default documentation examples.
- Deciding whether a `className` is structural layout or visual customization.
- Replacing custom design-system primitives with plain HeroUI components.

## Do Not Use For

- Feature composition, hooks, query invalidation, mutations, or data ownership → `platform-feature-ui-integration-steward`.
- File placement and module ownership → `platform-artifact-placement-steward`.
- Exact HeroUI component API, props, and examples beyond the embedded rules → `heroui-react`.
- Hard-coded text and localization → `platform-localization-maintenance`.
- Frontend/backend dependency boundaries → `platform-frontend-backend-isolation`.
- OpenAPI contracts and generated clients → `platform-openapi-contract-steward` / `platform-generated-react-client-steward`.

## Current Visual Policy

### 1. HeroUI Default Is the Design

Do not invent a Vitarish look. Do not reinterpret HeroUI. Do not add product-specific visual language.

The target is the visual result a developer gets after following HeroUI v3.2.1 quick start and using HeroUI components with their built-in styles.

### 2. Custom Visual CSS Is Not Allowed

Do not define or use custom global visual tokens for the Web host, including custom background, foreground, surface, muted, accent, status, radius, shadow, focus, motion, glass, or page-spacing variables.

Do not define custom visual classes such as `ds-*`, `glass-card`, `auth-bg`, `sidebar-*`, custom app bars, custom panels, custom badges, custom hero sections, custom orb decorations, custom gradients, custom glass layers, or custom focus rings.

### 3. `className` Is Presumed Suspicious

HeroUI docs explicitly support `className` as a customization method. Therefore, under the off-the-shelf policy:

- `className` on a HeroUI component is forbidden by default.
- A narrow exception may exist for non-visual structural layout only.
- Structural layout means sizing and flow only, such as `w-full`, `flex-1`, `min-w-0`, or container grid placement.
- Visual styling means colors, text color, background, border, radius, shadow, blur, opacity, transform, transition, font weight, letter spacing, padding that changes component anatomy, hover styling, focus styling, dark-mode styling, or state colors. Visual styling is prohibited.

### 4. Wrappers Must Not Change Appearance

Wrapper components are allowed only when they do not alter HeroUI's visual output.

Allowed wrapper responsibilities:

- Data mapping.
- Accessibility wiring.
- Form integration.
- Routing.
- Error normalization.
- Conditional rendering.
- Feature-owned composition.

Forbidden wrapper responsibilities:

- Adding default `className`.
- Adding custom variants.
- Applying custom tokens.
- Applying custom backgrounds, borders, shadows, radius, blur, gradients, transitions, hover/focus styles, or typography.
- Rebranding HeroUI components as a Vitarish design system.

### 5. Theme Control Must Be Official and Minimal

Use built-in HeroUI theme variables and `next-themes` root class control only.

Do not create a second theme system in `globals.css`.
Do not override HeroUI variables to create a custom brand theme.
Do not keep backend/user theme settings that apply custom CSS variables to the React host unless they only set HeroUI's built-in `light`, `dark`, or `system` theme through the documented controller.

## Mandatory Review Questions

For every UI file, answer these questions:

1. Does it import from `@/design-system`? If yes, can that import be removed and replaced with plain HeroUI or non-visual local composition?
2. Does it use any `ds-*`, `glass-card`, `auth-bg`, or `sidebar-*` class? If yes, it violates this skill.
3. Does `globals.css` contain anything beyond Tailwind and HeroUI imports plus truly non-visual necessities? If yes, justify or remove.
4. Does a HeroUI component receive a visual `className`? If yes, remove it.
5. Does code use inline `style` for visual appearance? If yes, remove it.
6. Does code override CSS variables such as `--background`, `--foreground`, `--surface`, `--accent`, `--border`, `--focus-ring`, or `--shadow-*`? If yes, remove it.
7. Does code use Tailwind visual utilities for color, radius, border, shadow, gradients, blur, opacity, transform, transition, hover/focus, typography, or spacing inside HeroUI component anatomy? If yes, remove it unless a documented functional exemption exists.
8. Does the page canvas use HeroUI's `bg-background text-foreground` rather than custom background CSS? If not, fix it.
9. Does theme control use a single controller? In Next.js, this must be `next-themes`; do not mix with HeroUI `useTheme` in the same app.
10. Does the result match HeroUI's documented shelf behavior more closely after the change? If not, the change is not aligned with this skill.

## Hard Stops

Stop and correct immediately if any of these appear in Web host UI code:

- `glass-card`
- `auth-bg`
- `ds-app-shell`
- `ds-centered-shell`
- `ds-page-container`
- `ds-surface-card`
- `ds-hero-panel`
- `ds-section-header`
- `ds-status-badge`
- `sidebar-nav-item`
- `sidebar-nav-item-active`
- `--surface`
- `--surface-muted`
- `--surface-glass`
- `--accent-soft`
- `--focus-ring`
- `--shadow-soft`
- `--shadow-lifted`
- `--radius-surface`
- `--radius-hero`
- `color-mix(` in app-owned CSS for visual design
- `oklch(` in app-owned CSS for custom visual design
- `backdrop-filter`
- `blur-`
- `shadow-` on HeroUI components
- `rounded-` on HeroUI components
- `bg-gradient-`
- `from-`, `via-`, `to-` used for visual gradients
- `dark:` visual overrides that duplicate or replace HeroUI's built-in dark theme
- custom BEM overrides of HeroUI classes
- wrapper components that exist only to restyle HeroUI

## Exemption Mechanism

Exemptions are rare and narrow.

A valid exemption must be placed beside the code it affects:

```tsx
// UI-DESIGN-EXEMPTION: <rule name>. <specific functional reason>. This does not change HeroUI visual identity.
```

A valid exemption must satisfy all conditions:

1. It names the exact rule being exempted.
2. It explains a functional need, not a taste preference.
3. It does not create or preserve a custom visual identity.
4. It is local to one component or one element.
5. It cannot be solved by a built-in HeroUI prop, built-in variant, or structural wrapper.

Invalid exemptions:

- "Looks better."
- "Matches the old Vitarish style."
- "Needed for brand identity."
- "Used elsewhere."
- "Design system primitive requires it."
- "Glassmorphism is desired."
- "The page looks empty without it."

## Required Review Output

When reviewing or planning UI work, include:

```md
## UI Design Review

### Component or area: <name>
### Policy: HeroUI v3.2.1 off-the-shelf appearance
### Framework baseline checked: Yes / No

| Check | Status |
| --- | --- |
| Uses plain HeroUI component appearance | Pass / Fail — <detail> |
| No custom visual global CSS | Pass / Fail — <detail> |
| No `ds-*`, `glass-card`, or custom design-system primitive | Pass / Fail — <detail> |
| No visual `className` on HeroUI components | Pass / Fail — <detail> |
| Theme follows official Next.js + next-themes pattern | Pass / Fail — <detail> |
| Accessibility preserved | Pass / Fail — <detail> |

### Findings

| Severity | Issue | Required code change |
| --- | --- | --- |
| Must-fix / Should-fix | <description> | <action> |

### Decision: accept / fix required / reject
```

## Handoff

Use this skill after artifact placement is confirmed and before or alongside feature integration.

Route to other skills when:

| Situation | Route to |
| --- | --- |
| Component file placement is unclear | `platform-artifact-placement-steward` |
| Component needs data/hooks | `platform-feature-ui-integration-steward` |
| HeroUI component API is unclear | `heroui-react` |
| Component has hard-coded user text | `platform-localization-maintenance` |
| Component touches backend imports | `platform-frontend-backend-isolation` |

## Evidence of Use

When applying this skill, include this line in the plan or final response:

```txt
Applied skill: platform-ui-design-steward
```

## Style

Be strict, direct, and practical. Treat custom visual styling as technical debt unless explicitly approved. Prefer deleting visual code over replacing it with a different custom visual pattern.
