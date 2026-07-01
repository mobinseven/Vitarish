# HeroUI v3.2.1 Off-the-Shelf Reset Reference

This reference records the code-level reset policy for removing the previous custom visual layer from `src/Hosts/Web`. It describes code changes only, not process, screenshots, PR sequencing, or release steps.

## Target State

The Web host must look like a normal HeroUI v3.2.1 application using HeroUI's shipped styles. The visual layer comes from `@heroui/styles` and `@heroui/react`, not from Vitarish-owned CSS, wrappers, glass effects, or custom tokens.

Global CSS baseline:

```css
@import "tailwindcss";
@import "@heroui/styles";
```

Root body baseline:

```tsx
<body className="bg-background text-foreground">
```

Next.js theme controller baseline:

```tsx
<ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
  {children}
</ThemeProvider>
```

## Embedded HeroUI v3.2.1 Documentation Facts

Official HeroUI docs checked on 2026-07-01 establish this baseline:

- Docs version: `v 3.2.1`.
- Requirements: React 19+ and Tailwind CSS v4.
- Packages: `@heroui/styles` and `@heroui/react`.
- CSS imports: Tailwind first, HeroUI styles second.
- Dark mode: CSS-driven from the root element.
- Styling provider: no HeroUI provider is required for HeroUI v3 styling.
- Built-in themes respond to `.light`, `.dark`, `data-theme="light"`, and `data-theme="dark"`.
- Page canvas: `bg-background text-foreground`.
- Next.js theme persistence/system behavior: use `next-themes`.
- App Router provider: `attribute="class"`, `defaultTheme="system"`, `enableSystem`, `disableTransitionOnChange`.
- Root layout: keep `suppressHydrationWarning` on `<html>` when `next-themes` controls the root.
- Styling customization mechanisms include `className`, inline `style`, Tailwind utilities, BEM/global CSS overrides, wrapper components, custom variables, and CSS-in-JS. Visual use of those mechanisms is prohibited by this repo policy unless narrowly exempted.

Official references:

- https://heroui.com/en/docs/react/getting-started/quick-start
- https://heroui.com/en/docs/react/getting-started/theming
- https://heroui.com/en/docs/react/getting-started/dark-mode
- https://heroui.com/en/docs/react/getting-started/styling

## Required Code Changes

### Global CSS

`src/Hosts/Web/src/app/globals.css` should contain only the official imports unless a non-visual necessity is approved.

Remove custom theme tokens, custom light/dark blocks, custom page-shell classes, custom focus rings, custom glass classes, custom app bars, custom sidebars, custom badges, custom callouts, custom card styles, custom shadows, custom radii, custom gradients, custom blur, `oklch(...)`, `color-mix(...)`, and `backdrop-filter`.

### Root Layout

`src/Hosts/Web/src/app/layout.tsx` must put `bg-background text-foreground` on `<body>` and keep `suppressHydrationWarning` on `<html>`.

Remove root-level custom background wrappers, custom persistent glass navigation, and body color/background rules owned by app CSS.

### Providers

`src/Hosts/Web/src/components/providers/HeroUIProvider.tsx` may keep React Query setup. Theme control must use `next-themes` as the single root theme controller. Do not mix it with HeroUI `useTheme` for root mutation. Theme values are limited to `light`, `dark`, and `system`.

### Local Design System

Active UI code must stop using visual primitives from `src/Hosts/Web/src/design-system`, including `AppShell`, `CenteredSurface`, `SurfaceCard`, `HeroPanel`, `SectionHeader`, `ActionGroup`, `StatusBadge`, and visual-only `classNames` composition.

Replace them with plain HeroUI components and non-visual structural wrappers.

### Visual className

`className` on a HeroUI component is forbidden by default. It is allowed only when structural and necessary, such as `w-full`, `flex-1`, or `min-w-0`.

Visual classes include color, background, border, radius, shadow, blur, opacity, transition, transform, hover/focus/active styling, typography used as branding, dark-mode overrides, gradients, and component anatomy spacing.

## Component Reset Rules

- Cards: use plain HeroUI `Card`; remove `glass-card`, `ds-surface-card`, custom backgrounds, borders, shadows, blur, radius, and hover lift.
- Buttons: use HeroUI `Button` with `onPress`, `isPending`, `isDisabled`, and built-in variants; remove manual color, hover, shadow, radius, transition, and custom button wrappers.
- Links/navigation: use HeroUI `Link` or semantic navigation; remove `ds-app-bar`, fixed pill navigation, glass backdrops, sidebars, active borders, and custom hover colors.
- Forms: use HeroUI form components and APIs for labels, required state, invalid state, errors, and descriptions; remove custom input backgrounds, borders, focus rings, glass panels, and placeholder-only labels.
- Alerts/status: use HeroUI `Alert` or semantic text; remove `ds-callout`, `StatusBadge`, custom status pills, dots, and color tokens.
- Tables: use HeroUI `Table` for tabular data and include `aria-label`; remove card-as-table grids, row dividers, hover colors, and glass wrappers.
- Modals: use HeroUI `Modal`; remove custom backdrops, overlays, blur, and visual slot classes.
- Loading/empty states: use HeroUI `Spinner`, `Button isPending`, plain text, or plain HeroUI containers; remove custom panels, decorative gradients, and muted visual systems.
- Page layout: use structural wrappers only; do not use `AppShell`, `CenteredSurface`, `auth-bg`, hero panels, decorative backgrounds, orb layers, or glass sections.

## Known Current Reset Areas

- Foundation: `globals.css`, `layout.tsx`, provider setup.
- Design system: `src/Hosts/Web/src/design-system/**`.
- App chrome: `src/Hosts/Web/src/components/app-bar/AppBar.tsx`.
- Auth: login/logout pages, auth method selector, login page card, status message.
- Account: theme page and option selector.
- Home: `HomeDashboard.tsx`.
- Knowledge: `src/Hosts/Web/src/features/knowledge/qa/components/**` and chat primitives.
- Clinic: `src/Hosts/Web/src/features/clinic/components/**`.

## Completion Definition

The reset is complete when active UI code no longer uses custom global visual CSS, local visual design-system primitives, visual `className` overrides on HeroUI components, inline visual styles, custom theme variables, custom app bars, sidebars, hero panels, cards, badges, callouts, chat panels, or glass/gradient/orb decorations. The remaining UI should look like normal HeroUI documentation examples.
