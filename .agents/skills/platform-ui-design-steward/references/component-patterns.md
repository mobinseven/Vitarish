# Component Visual Patterns — HeroUI v3.2.1 Off-the-Shelf Implementation

This file translates `design-rules.md` into concrete implementation rules for `src/Hosts/Web`.

The current policy is strict: **use HeroUI v3.2.1 exactly as the shelf/default visual system**. Component composition may be feature-specific, but visual appearance must come from HeroUI, not from Vitarish-owned custom classes, custom CSS variables, global overrides, or wrapper components that restyle the library.

## Official HeroUI v3.2.1 Facts Embedded in This Reference

The official HeroUI Web documentation checked on 2026-07-01 states:

- The current docs version displayed by HeroUI is `v 3.2.1`.
- Requirements: React 19+ and Tailwind CSS v4.
- Quick install: `@heroui/styles` and `@heroui/react`.
- Main CSS import order:
  1. `@import "tailwindcss";`
  2. `@import "@heroui/styles";`
- Import order matters; Tailwind must come first.
- HeroUI v3 dark mode is CSS-driven; components read theme variables from the root element.
- HeroUI does not need a HeroUI provider for styling.
- Built-in themes respond to `.light` / `.dark` classes and `data-theme="light"` / `data-theme="dark"` attributes.
- The app canvas should use `bg-background text-foreground`.
- In Next.js, `next-themes` is the documented controller for persistence, system preference support, and no flash before hydration.
- The documented App Router provider uses `ThemeProvider` from `next-themes` with `attribute="class"`, `defaultTheme="system"`, `enableSystem`, and `disableTransitionOnChange`.
- Root layout keeps `suppressHydrationWarning` on `<html>` because `next-themes` updates that element before hydration.
- HeroUI styling options include `className`, inline `style`, BEM classes, global CSS overrides, wrapper components, render props, CSS-in-JS, and custom theme variables. These are customization mechanisms. Under this repo policy, visual use of those mechanisms is prohibited.

Official references:

- https://heroui.com/en/docs/react/getting-started/quick-start
- https://heroui.com/en/docs/react/getting-started/dark-mode
- https://heroui.com/en/docs/react/getting-started/styling
- https://heroui.com/en/docs/react/getting-started/theming

---

## Global Baseline

### `package.json`

The Web host should depend on HeroUI's current required packages:

```json
{
  "dependencies": {
    "@heroui/react": "^3.2.1",
    "@heroui/styles": "^3.2.1",
    "next-themes": "^0.4.6"
  }
}
```

Rules:

1. Keep `@heroui/react`.
2. Keep `@heroui/styles`.
3. Keep `next-themes` only because this is a Next.js App Router host that needs theme persistence/system preference support.
4. Do not add a separate theme library.
5. Do not add a custom visual-variant library for restyling HeroUI.
6. `tailwind-variants` must not be used to create custom visual wrappers around HeroUI components. If it remains installed for a transitive or future non-visual reason, no Web host component may use it to create a custom design layer.

### `globals.css`

The off-the-shelf baseline is intentionally tiny:

```css
@import "tailwindcss";
@import "@heroui/styles";
```

Rules:

1. Tailwind import first.
2. HeroUI styles import second.
3. Do not define app-owned visual CSS variables.
4. Do not override HeroUI semantic variables.
5. Do not define custom light/dark theme blocks.
6. Do not define global custom component classes.
7. Do not define `@layer components` overrides for HeroUI BEM classes.
8. Do not define custom focus ring CSS.
9. Do not define custom reduced-motion CSS unless a specific accessibility gap remains after relying on HeroUI and browser defaults.
10. Do not define page-shell CSS, surface CSS, glass CSS, badge CSS, sidebar CSS, app-bar CSS, hero-panel CSS, card CSS, or table CSS.
11. Remove all previous visual tokens and classes from `globals.css`.

Delete these categories from `globals.css`:

- `:root` visual token block.
- `.dark` custom token block.
- `html` / `body` custom background or color rules.
- `@custom-variant dark` when it exists only for custom styling.
- `:focus-visible` custom outline rules.
- `@media (prefers-reduced-motion)` custom transition reset unless it is justified as a global accessibility requirement unrelated to visual identity.
- `.ds-*`.
- `.auth-bg`.
- `.glass-card`.
- `.sidebar`.
- `.sidebar-*`.
- Any `oklch(...)` custom color definition.
- Any `color-mix(...)` visual design rule.
- Any `backdrop-filter`.
- Any gradient, orb, glow, blur, shadow, radius, or custom spacing token.

### `layout.tsx`

The root layout should apply the official HeroUI canvas utility to `<body>`:

```tsx
<html lang="en" suppressHydrationWarning>
  <body className="bg-background text-foreground">
    <Providers>{children}</Providers>
  </body>
</html>
```

Rules:

1. Keep `suppressHydrationWarning` on `<html>` when using `next-themes`.
2. Add `className="bg-background text-foreground"` to `<body>`.
3. Do not set custom body background/color in CSS.
4. Do not place custom fixed glass app bars or decorative page wrappers in the root layout.
5. Root layout may include application-level providers. It must not apply visual branding beyond HeroUI's canvas utilities.

### Providers

The Next.js provider should use `next-themes` as the single theme controller:

```tsx
"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";

export function Providers({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: { staleTime: 5 * 60 * 1000, retry: 1 },
        },
      }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider
        attribute="class"
        defaultTheme="system"
        enableSystem
        disableTransitionOnChange
      >
        {children}
      </ThemeProvider>
    </QueryClientProvider>
  );
}
```

Rules:

1. Use one theme controller per app.
2. In Next.js, use `next-themes`, not HeroUI's plain React `useTheme` controller.
3. Do not mix `next-themes` and HeroUI `useTheme` to mutate the root theme.
4. Use `defaultTheme="system"` for the default off-the-shelf behavior.
5. Use `enableSystem`.
6. Use `disableTransitionOnChange`.
7. Keep theme persistence but do not persist custom theme names unless a future accepted decision introduces them.
8. Theme values are limited to `light`, `dark`, and `system`.

---

## Allowed and Forbidden Styling

### Allowed Structural Classes

Structural classes are allowed on ordinary HTML wrapper elements when they do not change HeroUI component appearance.

Examples:

```tsx
<main className="min-h-screen bg-background text-foreground">
  <div className="mx-auto max-w-5xl p-6">
    {children}
  </div>
</main>
```

Allowed structural categories:

- `flex`, `grid`, `block`, `inline`, `hidden`.
- `flex-col`, `items-*`, `justify-*`, `content-*`.
- `gap-*`, `space-*` when used on layout wrappers.
- `w-*`, `min-w-*`, `max-w-*`, `h-*`, `min-h-*`, `max-h-*` when used for layout.
- `mx-auto`, `container`, `overflow-*`.
- Responsive prefixes for layout only.
- `bg-background text-foreground` for the page canvas only, because HeroUI explicitly documents this.

### Forbidden Visual Classes

Forbidden on HeroUI components and forbidden as reusable app visual policy:

- Color: `bg-*`, `text-*`, `border-*`, `ring-*`, `outline-*`, `from-*`, `via-*`, `to-*`, `fill-*`, `stroke-*`.
- Custom semantic text classes: `text-muted`, `text-foreground` outside the documented page canvas use, `text-default-*` when used to force local visual tone.
- Border shape and depth: `rounded-*`, `border`, `shadow-*`, `ring-*`.
- Glass and atmosphere: `backdrop-*`, `blur-*`, `opacity-*`, gradients, glows, orbs.
- Motion and state: `transition*`, `duration-*`, `ease-*`, `hover:*`, `focus:*`, `active:*`, `group-hover:*`, `data-[*]` styling when used visually.
- Typography as visual branding: `font-black`, custom letter spacing, uppercase tracking used as a visual identity pattern, custom heading scales beyond semantic HTML needs.
- Custom spacing inside HeroUI anatomy when it changes the component's default look: `p-*`, `px-*`, `py-*`, `m-*`, `rounded-*` on HeroUI component roots/slots.
- Any `dark:` visual override that changes the built-in HeroUI dark theme.
- Any inline `style` that changes visual appearance.

### `className` Decision Rule

`className` on a HeroUI component is disallowed unless every class in it is structural and necessary.

Allowed example:

```tsx
<Button className="w-full">Save</Button>
```

This is allowed only when the button must fill a form container.

Forbidden examples:

```tsx
<Button className="bg-violet-600 text-white hover:bg-violet-700">Save</Button>
<Card className="glass-card p-6">
<Card className="rounded-3xl shadow-xl border-white/20">
<Input className="bg-white/70">
<Alert className="mb-4 border border-danger-200">
```

If a class changes the visual appearance of a HeroUI component, remove it.

---

## Design-System Primitive Removal

The previous local design-system primitives are visual wrappers and must not be used for off-the-shelf HeroUI appearance.

Remove imports from `@/design-system` when they reference visual primitives:

- `AppShell`
- `CenteredSurface`
- `SurfaceCard`
- `HeroPanel`
- `SectionHeader`
- `ActionGroup`
- `StatusBadge`

Remove or stop exporting visual primitive files when no longer used:

- `src/Hosts/Web/src/design-system/primitives/AppShell.tsx`
- `src/Hosts/Web/src/design-system/primitives/CenteredSurface.tsx`
- `src/Hosts/Web/src/design-system/primitives/SurfaceCard.tsx`
- `src/Hosts/Web/src/design-system/primitives/HeroPanel.tsx`
- `src/Hosts/Web/src/design-system/primitives/SectionHeader.tsx`
- `src/Hosts/Web/src/design-system/primitives/ActionGroup.tsx`
- `src/Hosts/Web/src/design-system/primitives/StatusBadge.tsx`
- `src/Hosts/Web/src/design-system/primitives/classNames.ts`
- `src/Hosts/Web/src/design-system/primitives/index.ts`
- `src/Hosts/Web/src/design-system/index.ts`

A replacement wrapper is allowed only when it is non-visual. For example, a feature-specific component that fetches data and renders HeroUI components is allowed; a wrapper that adds default classes to HeroUI is not.

---

## Component Patterns

## Card

Required pattern:

```tsx
import { Card } from "@heroui/react";

<Card>
  <Card.Header>
    <h2>Title</h2>
  </Card.Header>
  <Card.Content>
    Content
  </Card.Content>
</Card>
```

Rules:

1. Use plain HeroUI `Card`.
2. Do not use `glass-card`.
3. Do not use `ds-surface-card`.
4. Do not add custom border, background, blur, radius, shadow, gradient, or padding classes to the card root.
5. Use HeroUI compound slots as intended.
6. If spacing is required, prefer an external structural wrapper around the card, not visual customization of the card itself.
7. Do not create `SurfaceCard`, `GlassCard`, `DashboardCard`, or similar wrappers whose purpose is visual styling.

Forbidden:

```tsx
<Card className="glass-card">
<Card className="ds-surface-card ds-surface-card-elevated">
<Card className="bg-white/70 border-white/30 shadow-2xl backdrop-blur-xl">
```

## Button

Required pattern:

```tsx
import { Button } from "@heroui/react";

<Button onPress={handler}>Save</Button>
<Button variant="secondary" onPress={cancel}>Cancel</Button>
<Button variant="danger" onPress={deleteItem}>Delete</Button>
<Button isPending={isSaving} onPress={save}>Save</Button>
```

Rules:

1. Use HeroUI `Button`.
2. Use `onPress`, not `onClick`, for HeroUI buttons.
3. Use built-in `variant` values only when they express actual action semantics.
4. Use `isPending` for pending actions.
5. Use `isDisabled` for disabled actions.
6. Do not style primary color manually.
7. Do not add hover, focus, active, transition, shadow, radius, or color classes.
8. Do not create custom button wrappers for visual variants.
9. Icon-only buttons require an accessible label and, where appropriate, HeroUI tooltip usage.

Forbidden:

```tsx
<Button className="bg-violet-600 text-white shadow-lg hover:bg-violet-700">
<Button className="rounded-full px-6">
<Button onClick={handler}>
```

## Link and Navigation

Required pattern:

```tsx
import { Link } from "@heroui/react";

<Link href="/knowledge">Knowledge</Link>
```

Rules:

1. Use HeroUI `Link` for links when using HeroUI link appearance.
2. Use Next.js `Link` only where routing constraints require it; do not restyle it to mimic a custom navigation system.
3. Remove custom fixed pill app bars.
4. Remove `ds-app-bar`.
5. Remove `sidebar`, `sidebar-nav-item`, and `sidebar-nav-item-active`.
6. Navigation layout may use structural wrappers, but visual appearance must remain plain HeroUI.
7. Do not add custom active-state borders, custom accent backgrounds, or glass backdrops.

Forbidden:

```tsx
<header className="ds-app-bar">
<nav className="sidebar">
<a className="sidebar-nav-item sidebar-nav-item-active">
```

## Form Fields

Required pattern:

```tsx
import { Input, Label, TextField } from "@heroui/react";

<TextField name="email" isRequired>
  <Label>Email</Label>
  <Input value={email} onChange={(event) => setEmail(event.target.value)} />
</TextField>
```

Rules:

1. Use HeroUI form components and their documented accessibility model.
2. Always provide a visible label or an accessible label.
3. Use built-in invalid/error APIs when available.
4. Do not use placeholder text as the only label.
5. Do not wrap inputs in custom glass panels.
6. Do not add custom background, border, radius, focus ring, text color, or padding classes to fields.
7. Layout wrappers may use structural classes such as `flex`, `grid`, `gap-*`, and `w-full`.

Forbidden:

```tsx
<Input className="bg-white/70 border-white/30 shadow-sm">
<TextField className="rounded-xl">
<p className="text-red-500">Custom error</p>
```

## Alert and Status Messages

Required pattern:

```tsx
import { Alert } from "@heroui/react";

<Alert status="danger">
  <Alert.Indicator />
  <Alert.Content>Something went wrong.</Alert.Content>
</Alert>
```

Rules:

1. Use HeroUI `Alert`.
2. Use HeroUI status props.
3. Do not add custom margin, border, background, icon color, or status classes unless structural placement is unavoidable.
4. Do not create custom status callouts.
5. Remove `StatusBadge` when it is a custom visual primitive.
6. Use a HeroUI-supported status display or plain text when badge semantics are not needed.

Forbidden:

```tsx
<Alert className="mb-4 border border-danger-200">
<section className="ds-callout ds-callout-danger">
<StatusBadge tone="success">Ready</StatusBadge>
```

## Table and Data Display

Required pattern:

```tsx
import { Table } from "@heroui/react";

<Table aria-label="Patients">
  <Table.Header>
    <Table.Column>Name</Table.Column>
  </Table.Header>
  <Table.Body>
    <Table.Row key="1">
      <Table.Cell>Jane</Table.Cell>
    </Table.Row>
  </Table.Body>
</Table>
```

Rules:

1. Use HeroUI `Table` for tabular data.
2. Always include `aria-label`.
3. Do not build custom table-like cards unless HeroUI has no matching component for the use case.
4. Do not add custom borders, row dividers, hover colors, muted text colors, or glass containers.
5. Empty states can be simple plain text or a plain HeroUI component; do not create custom decorative empty states.

Forbidden:

```tsx
<Card className="glass-card">
  <div className="grid grid-cols-4 border-b border-default-200">
```

## Modal / Dialog

Required pattern:

```tsx
import { Modal } from "@heroui/react";

<Modal isOpen={isOpen} onOpenChange={setIsOpen}>
  <Modal.Content>
    <Modal.Header>Confirm</Modal.Header>
    <Modal.Body>Are you sure?</Modal.Body>
    <Modal.Footer>
      <Button variant="secondary" onPress={cancel}>Cancel</Button>
      <Button variant="danger" onPress={confirm}>Delete</Button>
    </Modal.Footer>
  </Modal.Content>
</Modal>
```

Rules:

1. Use HeroUI `Modal`.
2. Do not build custom backdrops.
3. Do not add custom glass overlays or blur.
4. Do not add visual classes to modal slots.
5. Use HeroUI semantics for close behavior and focus handling.

## Spinner and Loading State

Required pattern:

```tsx
import { Spinner } from "@heroui/react";

<Spinner />
```

Rules:

1. Use HeroUI `Spinner`.
2. Use `Button isPending` for button-owned pending states.
3. Loading containers may use structural alignment classes.
4. Do not add custom spinner colors, glow, panel, or glass card wrappers.

## Empty State

Required pattern:

```tsx
<p>No items found.</p>
```

or, when a container is needed:

```tsx
<Card>
  <Card.Content>No items found.</Card.Content>
</Card>
```

Rules:

1. Keep empty states plain.
2. Do not create custom empty-state illustrations, glass panels, or gradient sections.
3. Do not force muted custom typography.
4. Use direct text and ordinary HeroUI structure.

## Page Layout

Required baseline:

```tsx
<main className="min-h-screen bg-background text-foreground">
  <div className="mx-auto max-w-5xl p-6">
    {children}
  </div>
</main>
```

Rules:

1. Use structural layout only.
2. Do not use `AppShell`.
3. Do not use `CenteredSurface`.
4. Do not use hero panels, decorative backgrounds, or glass page backgrounds.
5. `bg-background text-foreground` is allowed because HeroUI documents it as the theme-aware page canvas.
6. If the root `<body>` already owns `bg-background text-foreground`, page-level repetition is optional and should be minimized.
7. Use plain semantic headings and text; do not create custom heading systems.

Forbidden:

```tsx
<AppShell>
<main className="auth-bg">
<section className="ds-hero-panel">
<div className="absolute ... rounded-full ... blur-3xl">
```

---

## Current Repo Reset Targets

The following current Web host areas are known to contain custom visual policy and must be reset when touched:

### Global Foundation

- `src/Hosts/Web/src/app/globals.css`
  - Replace with the two official imports unless a non-visual necessity remains.
- `src/Hosts/Web/src/app/layout.tsx`
  - Add `className="bg-background text-foreground"` to `<body>`.
  - Remove persistent custom app-bar rendering if it exists only as visual chrome.
- `src/Hosts/Web/src/components/providers/HeroUIProvider.tsx`
  - Keep query provider.
  - Configure `next-themes` as documented.
  - Do not add a HeroUI provider for styling.

### Local Design System

- Remove or stop using all visual exports from `src/Hosts/Web/src/design-system`.
- Delete or leave unused only until cleanup if no imports remain.
- Do not replace with new visual wrappers.

### App Bar and Navigation

- `src/Hosts/Web/src/components/app-bar/AppBar.tsx`
  - Remove `ds-app-bar`.
  - Replace with plain structural header or HeroUI navigation components without custom visual styling.

### Authentication

- `src/Hosts/Web/src/app/login/page.tsx`
  - Replace `CenteredSurface` with plain layout and HeroUI `Card` if a card is needed.
- `src/Hosts/Web/src/components/auth/LoginPageCard.tsx`
  - Remove `CenteredSurface`.
  - Remove custom skeleton background classes.
- `src/Hosts/Web/src/components/auth/AuthMethodSelector.tsx`
  - Remove `ds-surface-card`.
  - Remove custom link hover/background/foreground/transition classes.
- `src/Hosts/Web/src/app/logout/page.tsx`
  - Remove `CenteredSurface`.
  - Keep HeroUI `Button`, `Alert`, and plain structure.
- `src/Hosts/Web/src/components/auth/StatusMessage.tsx`
  - Remove visual `className` from `Alert` unless purely structural and necessary.

### Account

- `src/Hosts/Web/src/app/account/theme/page.tsx`
  - Keep `useTheme` from `next-themes`.
  - Theme values limited to `light`, `dark`, `system`.
  - Ensure saved settings only drive the official theme controller, not custom CSS variables.
- `src/Hosts/Web/src/components/account/OptionSelector.tsx`
  - Remove custom option-card visual styling.
  - Prefer HeroUI built-in controls.

### Home

- `src/Hosts/Web/src/features/home/components/HomeDashboard.tsx`
  - Remove `auth-bg`.
  - Remove custom hero gradient section.
  - Remove decorative orbs.
  - Remove custom violet/cyan/fuchsia gradients.
  - Remove custom call-to-action link button styling.
  - Replace custom cards with plain HeroUI `Card`.
  - Replace page-specific visual hierarchy with plain headings and HeroUI components.

### Knowledge

- `src/Hosts/Web/src/features/knowledge/qa/components/KnowledgeUserQaSurface.tsx`
  - Remove `AppShell`.
  - Use structural layout only.
- `KnowledgeCorpusRail.tsx`
  - Replace `SurfaceCard` with plain `Card`.
  - Remove `railButtonClass`.
  - Use plain HeroUI `Button` or selection component behavior.
- `KnowledgeQuestionComposer.tsx`
  - Replace `SurfaceCard` with plain `Card`.
  - Avoid custom heading/eyebrow visual classes.
- `KnowledgeAnswerList.tsx`
  - Replace `SurfaceCard` with plain `Card`.
  - Use plain HeroUI buttons.
- `KnowledgeAnswerCard.tsx`
  - Remove custom chat card styling, callouts, source panels, badges, and muted text classes.
- `chat/KnowledgeChatPrimitives.tsx`
  - Remove `SurfaceCard`.
  - Remove `ds-inline-panel`, `ds-chat-body`, `ds-callout`, and custom status classes.
  - Use plain `Card`, `Alert`, text, and structural wrappers.

### Clinic

- `PatientsGrid.tsx`
  - Replace custom card/table layout with HeroUI `Table` where tabular.
  - Remove `glass-card`.
  - Remove custom borders/dividers/text color classes.
- `PatientRow.tsx`
  - Remove `ds-surface-card`.
  - Avoid using Card as a styled row when a table row is semantically correct.
- `PatientEditForm.tsx`
  - Remove `glass-card`.
- `ClinicOwnerPatientsPage.tsx`
  - Remove `SectionHeader`.
  - Remove `glass-card`.
  - Use plain headings and HeroUI components.
- Any clinic detail/search/overview components using `glass-card`, `ds-*`, custom status colors, or custom panels must be reset the same way.

---

## Search Terms That Must Trend Toward Zero

The reset is incomplete while these terms remain in user-facing Web host UI code:

```txt
glass-card
auth-bg
ds-
SurfaceCard
CenteredSurface
AppShell
HeroPanel
SectionHeader
ActionGroup
StatusBadge
classNames(
--surface
--accent
--focus-ring
--shadow
--radius
color-mix(
oklch(
backdrop-filter
blur-
shadow-
bg-gradient
from-violet
via-
to-cyan
text-muted
sidebar-nav
```

Some strings may remain in documentation that explains their removal, but they must not remain as active UI implementation patterns.

---

## Review Decision

A UI change passes this reference only when:

1. It uses HeroUI components without custom visual restyling.
2. It uses official global CSS imports.
3. It uses official theme control.
4. It removes or avoids local visual wrappers.
5. It keeps accessibility intact.
6. It reduces the amount of custom visual code.
7. It makes the screen look more like HeroUI documentation, not more like a Vitarish custom theme.
