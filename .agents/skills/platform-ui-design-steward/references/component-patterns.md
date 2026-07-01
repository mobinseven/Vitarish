# Component Visual Patterns — Framework Implementation

This file translates the abstract design philosophy in `design-rules.md` into concrete HeroUI component patterns. Each section maps a design principle to the most compatible framework option.

**The guiding spirit of this reference is feeling-to-pattern mapping.** Every component pattern must evoke a specific feeling. When the design rules change, update this file to reflect new framework choices. When the framework changes (e.g., HeroUI v3 Pro upgrade), update this file to reflect new capabilities.

## Feeling-to-Pattern Reference

| Feeling | Design Principle | Provable HeroUI Feature |
|---------|------------------|------------------------|
| Responsive feedback | Interaction Philosophy | Button `isPressed`, `isHovered`, `isFocusVisible` render props |
| Loading awareness | Interaction Philosophy | Button `isPending` state with `Spinner` |
| Prominence hierarchy | Clean Minimal Philosophy | Card `variant` (`transparent` → `tertiary`) for visual weight |
| Contextual clarity | Interaction Philosophy | Tooltip `placement` prop for positioned help |
| Accessibility safety | Interaction Philosophy | Button `isFocusVisible` for keyboard navigation |
| Active state | Interaction Philosophy | Button `:active` / `[data-pressed]` CSS states |

### Anti-feelings (must never evoke)

| Prohibited Feeling | Pattern to Avoid |
|--------------------|------------------|
| Industrial | Raw borders, dense tables without breathing room |
| Playful | Bouncy animations, bright multi-color palettes |
| Flat | Surfaces without depth hierarchy |
| Heavy | Solid opaque panels without translucency |

---

## Card

**Feeling:** Weightless, translucent, catching light — surfaces that float without heaviness.
**Design principle:** Surfaces should feel like frosted glass — translucent, catching ambient light, creating gentle depth without heaviness.

### Required Pattern

```tsx
import { Card } from "@heroui/react";

<Card className="glass-card">
  <Card.Header>
    <h2 className="text-lg font-semibold">Title</h2>
  </Card.Header>
  <Card.Content>
    {/* content */}
  </Card.Content>
</Card>
```

### Rules

1. **Always apply `glass-card` class** to HeroUI `Card` components.
2. **Use `SurfaceCard` primitive** when the card needs tone variants (`default`, `elevated`, `interactive`).
3. **Never apply `backdrop-filter` directly** — the `glass-card` class handles it.
4. **Card headers** use `text-lg font-semibold` for titles.
5. **Card content** uses `text-sm` for body text with `text-muted` for secondary text.
6. **Interactive cards** use `SurfaceCard` with `tone="interactive"` for hover lift effect.
7. **Cards conveying status or meaning** must include a tooltip describing the content or state.
8. **Do not use directional margin/padding classes** (`ml-*`, `mr-*`, `pl-*`, `pr-*`) — use logical properties (`ms-*`, `me-*`, `ps-*`, `pe-*`) for RTL/LTR support.

### Anti-Pattern

```tsx
// WRONG: No glass treatment
<Card className="p-6 border rounded-lg">

// WRONG: Hard-coded backdrop-filter
<Card className="p-6" style={{ backdropFilter: "blur(12px)" }}>

// WRONG: Raw color values
<Card className="bg-white/70 border-white/30">
```

---

## Button

**Feeling:** Alive, responsive, physically satisfying — immediate acknowledgment of input.
**Design principle:** Feedback should feel alive — immediate, responsive, and physically satisfying.

### Required Pattern

```tsx
import { Button } from "@heroui/react";

// Primary action (one per context)
<Button variant="primary" onPress={handler}>Save</Button>

// Secondary action
<Button variant="secondary" onPress={handler}>Cancel</Button>

// Danger action
<Button variant="danger" onPress={handler}>Delete</Button>
```

### Rules

1. **Use `onPress`**, not `onClick` (HeroUI v3 accessibility).
2. **Use semantic variants**: `primary`, `secondary`, `danger`, `ghost`, `outline`.
3. **Primary buttons** are solid accent color, no glass treatment.
4. **Secondary buttons** may use glass surface treatment.
5. **One primary button per context.** Multiple primaries create decision paralysis.
6. **Button labels** are concise: verb or verb+noun. No sentences.
7. **Icon-only buttons** must include a tooltip describing the action.
8. **Button icon placement** — use `ltr:order-2 rtl:order-1` or HeroUI's built-in icon position props (`startContent`/`endContent`) for RTL/LTR support.

### Anti-Pattern

```tsx
// WRONG: onClick instead of onPress
<Button onClick={handler}>Save</Button>

// WRONG: Raw color styling
<Button className="bg-violet-600 text-white hover:bg-violet-700">Save</Button>

// WRONG: Multiple primary buttons
<Button variant="primary">Save</Button>
<Button variant="primary">Continue</Button>
```

---

## Form (TextField, Select, TextArea)

**Feeling:** Breathing room, calm readability — generous spacing that invites rather than demands.
**Design principle:** Text should breathe — generous spacing, clear hierarchy, calm readability.

### Required Pattern

```tsx
import { TextField, Select } from "@heroui/react";

<TextField
  label="Email"
  placeholder="you@example.com"
  className="w-full"
/>

<Select
  label="Status"
  className="w-full"
>
  {/* options */}
</Select>
```

### Rules

1. **Always include `label`** — never use placeholder as the only label.
2. **Use `w-full`** for form fields in form layouts.
3. **Form spacing** uses `gap-4` between fields.
4. **Form layouts** use `<form className="flex flex-col gap-4">` or `SurfaceCard` wrapper.
5. **Helper text** uses `description` prop, not separate `<p>` elements.
6. **Error state** uses HeroUI's built-in `isInvalid` + `errorMessage` props.
7. **Form field labels** — HeroUI handles label positioning for RTL/LTR; do not hard-code left/right alignment.
8. **Helper/error text** — use `text-start` for alignment instead of `text-left`.

### Anti-Pattern

```tsx
// WRONG: No label
<TextField placeholder="Enter email" />

// WRONG: Hard-coded width
<TextField style={{ width: "300px" }} />

// WRONG: Custom error display instead of HeroUI
<div className="text-red-500">{error}</div>
<TextField />
```

---

## Modal / Dialog

**Feeling:** Depth, proximity, hierarchy — the most important content feels closest to the user.
**Design principle:** Depth creates hierarchy — the most important content feels closest to the user.

### Required Pattern

```tsx
import { Modal } from "@heroui/react";

<Modal isOpen={isOpen} onOpenChange={onOpenChange}>
  <Modal.Content>
    <Modal.Header>
      <h2 className="text-lg font-semibold">Confirm Action</h2>
    </Modal.Header>
    <Modal.Body>
      <p className="text-sm text-muted">Description text</p>
    </Modal.Body>
    <Modal.Footer>
      <Button variant="secondary" onPress={onClose}>Cancel</Button>
      <Button variant="primary" onPress={onConfirm}>Confirm</Button>
    </Modal.Footer>
  </Modal.Content>
</Modal>
```

### Rules

1. **Modal footer** uses `justify-end` with `gap-2` — button order adapts to layout direction via CSS logical properties.
2. **Modal body** uses `text-sm text-muted` for descriptions.
3. **Modal title** uses `text-lg font-semibold`.
4. **Glass backdrop** is handled by HeroUI's modal overlay — do not add custom backdrop styles.
5. **One modal at a time.** Do not stack modals.
6. **Do not hard-code left/right** — use logical properties (`ms-`, `me-`, `ps-`, `pe-`, `start-`, `end-`) for all directional spacing.

### Anti-Pattern

```tsx
// WRONG: Custom backdrop
<div className="fixed inset-0 bg-black/50 backdrop-blur-sm">

// WRONG: Hard-coded directional classes
<Modal.Footer className="flex justify-end gap-2">
  <Button variant="primary" className="mr-2">Save</Button>
  <Button variant="secondary">Cancel</Button>
</Modal.Footer>

// RIGHT: Logical properties
<Modal.Footer className="flex justify-end gap-2">
  <Button variant="primary" className="me-2">Save</Button>
  <Button variant="secondary">Cancel</Button>
</Modal.Footer>
```

---

## Table / Data Grid

**Feeling:** Earned presence, intentional density — every element serves a purpose without clutter.
**Design principle:** Every element on the screen should earn its place — data-dense but not cluttered.

### Required Pattern

```tsx
import { Table } from "@heroui/react";

<Table aria-label="Data table">
  <Table.Header>
    <Table.Column>Name</Table.Column>
    <Table.Column>Status</Table.Column>
  </Table.Header>
  <Table.Body>
    <Table.Row key="1">
      <Table.Cell>Item Name</Table.Cell>
      <Table.Cell>Active</Table.Cell>
    </Table.Row>
  </Table.Body>
</Table>
```

### Rules

1. **Always include `aria-label`** for accessibility.
2. **Table surface** uses glass treatment via container or class.
3. **Column headers** use `font-semibold text-sm`.
4. **Row hover** uses `--accent-soft` background.
5. **Empty state** shows a centered message with `text-muted`, not a blank table.
6. **Table cell alignment** — use `text-start` instead of `text-left` for RTL/LTR support.
7. **Column order** — HeroUI Table handles RTL column reversal automatically; do not override.

### Anti-Pattern

```tsx
// WRONG: No aria-label
<Table>

// WRONG: Custom table styling outside HeroUI
<table className="w-full border-collapse">
```

---

## Navigation / AppBar

**Feeling:** Calm, focused, purposeful — quiet guidance without noise.
**Design principle:** The interface should feel calm, focused, and purposeful — never busy or noisy.

### Required Pattern

Use the existing `AppBar` component or the `ds-app-bar` class.

```tsx
<div className="ds-app-bar">
  {/* nav items */}
</div>
```

### Rules

1. **AppBar** is fixed-position, centered, with pill shape and glass backdrop.
2. **Nav items** use `sidebar-nav-item` class for sidebar navigation.
3. **Active state** uses `sidebar-nav-item-active` with accent color and inline-start border (adapts to RTL/LTR).
4. **Never create custom fixed-position navigation** without using the established patterns.
5. **Do not hard-code left/right** — use logical properties for all directional styling.

---

## Page Layout

**Feeling:** Natural margins, intentional rest — whitespace that gives content room to exist.
**Design principle:** Whitespace is not emptiness — it is a design element. The page should feel like it has natural margins.

### Required Pattern

```tsx
import { AppShell, SectionHeader } from "@/design-system";

<AppShell>
  <SectionHeader
    eyebrow="Module Name"
    title="Page Title"
    description="Brief description"
  />
  {/* page content */}
</AppShell>
```

### Rules

1. **All pages** use `AppShell` for consistent page container and padding.
2. **Page sections** use `SectionHeader` for consistent heading hierarchy.
3. **Eyebrow** uses `text-sm font-semibold uppercase tracking-[0.3em] text-muted`.
4. **Section title** uses `text-3xl font-bold` or `clamp()` sizing.
5. **Page max-width** is `80rem` (via `ds-page-container`).
6. **Content max-width** is `56rem` (via `ds-page-container-content`).
7. **Do not hard-code padding/margin direction** — use logical properties (`ps-*`, `pe-*`, `ms-*`, `me-*`) for RTL/LTR support.

### Anti-Pattern

```tsx
// WRONG: Custom page wrapper
<main className="max-w-5xl mx-auto p-8">

// WRONG: Ad-hoc section headers
<h1 className="text-4xl font-bold mb-4">Title</h1>
<p className="text-gray-500 mb-8">Description</p>
```

---

## Status Badge

**Feeling:** Deliberate punctuation, emotional shorthand — color that communicates without words.
**Design principle:** Color as punctuation, not decoration — a deliberate brushstroke on a calm canvas.

### Required Pattern

```tsx
import { StatusBadge } from "@/design-system";

<StatusBadge tone="success">Active</StatusBadge>
<StatusBadge tone="muted">Inactive</StatusBadge>
```

### Rules

1. **Use `StatusBadge` primitive**, not custom inline badge styles.
2. **Tone options**: `accent` (default), `success`, `muted`.
3. **Status text** is concise: one word or short phrase.
4. **Never create custom badge components** that duplicate `StatusBadge` behavior.
5. **Status badges** must include a tooltip explaining the status state.

---

## Empty State

**Feeling:** Calm, not broken — minimal presence that guides without despair.
**Design principle:** Minimalism is the removal of everything that is not essential — empty states should feel calm, not broken.

### Required Pattern

```tsx
<div className="flex flex-col items-center gap-4 py-12 text-center">
  <p className="text-sm text-muted">No items found</p>
  <p className="text-xs text-muted">Try adjusting your search criteria</p>
</div>
```

### Rules

1. **Empty states** are centered vertically and horizontally.
2. **Primary message** uses `text-sm text-muted`.
3. **Secondary guidance** uses `text-xs text-muted`.
4. **Do not show an empty table/grid.** Replace with a clear empty state message.

---

## Glass Card (Reusable Pattern)

**Feeling:** Translucent depth, ambient light — surfaces that catch light without weight.
For components that need a glass surface without using HeroUI `Card`:

```tsx
<div className="glass-card p-6">
  {/* content */}
</div>
```

### Rules

1. **Always use `glass-card` class** — never manually compose backdrop-filter + background + border.
2. **Padding** is `p-6` minimum for standard cards, `p-8` for hero/featured cards.
3. **For interactive glass cards**, add hover transition manually or use `SurfaceCard` with `tone="interactive"`.

---

## Tooltip

**Feeling:** Clarity, kindness — making meaning accessible without assumption.
**Design principle:** Clarity is kindness — every component with meaning should make that meaning accessible.

### Required Pattern

```tsx
import { Tooltip } from "@heroui/react";

<Tooltip content="Description of the element">
  <Button variant="icon">
    <IconComponent />
  </Button>
</Tooltip>
```

### Rules

1. **Use `Tooltip` component** from HeroUI, not custom tooltip implementations.
2. **Tooltip content** should be concise: one sentence describing the element's purpose or state.
3. **Wrap the interactive element** with `Tooltip`, not a container div.
4. **Apply tooltips to all components conveying meaning**: icons, status badges, abbreviations, icon-only buttons, and elements where the meaning may not be immediately clear.
5. **Never use tooltips for required information** — only for supplementary context or clarification.
6. **Tooltip placement** — use `placement` prop to position tooltips appropriately; HeroUI handles RTL flipping automatically.

### Anti-Pattern

```tsx
// WRONG: No tooltip for icon-only button
<Button variant="icon">
  <TrashIcon />
</Button>

// WRONG: Custom tooltip implementation
<div className="tooltip">...</div>

// WRONG: Tooltip on text that should be self-explanatory
<Tooltip content="This is a button that saves">
  <Button variant="primary">Save</Button>
</Tooltip>
```

---

## General Visual Rules

### Spacing Consistency

- Form fields: `gap-4` between fields
- Card grids: `gap-5`
- Section gaps: `gap-6` to `gap-8`
- Inline elements: `gap-2` to `gap-3`
- Page sections: `gap-8`

### Text Color Hierarchy

- Primary text: `text-foreground` (or default)
- Secondary text: `text-muted`
- Accent text: `text-accent` or `text-[var(--accent)]`
- Disabled text: use opacity, not a separate color

### Border Treatment

- Card borders: `border border-[var(--border)]`
- Glass borders: handled by `glass-card` class
- Divider lines: `border-t border-[var(--border)]`
- Never use `border-gray-*` or `border-white/*`

### RTL/LTR Layout Support

All components must support both RTL (right-to-left) and LTR (left-to-right) layouts. Follow these rules:

- **Use logical CSS properties** — never hard-code left/right:
  - `ms-*` (margin-inline-start) instead of `ml-*`
  - `me-*` (margin-inline-end) instead of `mr-*`
  - `ps-*` (padding-inline-start) instead of `pl-*`
  - `pe-*` (padding-inline-end) instead of `pr-*`
  - `start-*` instead of `left-*`
  - `end-*` instead of `right-*`
  - `border-s-*` instead of `border-l-*`
  - `border-e-*` instead of `border-r-*`
- **Text alignment** — use `text-start` / `text-end` instead of `text-left` / `text-right`.
- **Flexbox layouts** — use `justify-start` / `justify-end` instead of `justify-left` / `justify-right`.
- **Icons** — place icons after text in LTR, before text in RTL. Use `ltr:order-2 rtl:order-1` or HeroUI's built-in RTL support.
- **Translations** — assume all user-facing text will be translated. Do not truncate based on hard-coded widths.
- **Bidirectional icons** — icons that indicate direction (arrows, chevrons, navigation) must flip in RTL mode. Use HeroUI's `FlipVertical` or CSS `transform: scaleX(-1)` with `rtl:` prefix.

### Tooltips

- **All components that convey meaning must include a tooltip** to ensure accessibility and clarity.
- Use HeroUI's `Tooltip` component to wrap elements that need explanation.
- Apply tooltips to: icons, status badges, abbreviations, buttons with icon-only content, and any component where the meaning may not be immediately clear.
- Tooltip text should be concise and describe the component's purpose or state.
