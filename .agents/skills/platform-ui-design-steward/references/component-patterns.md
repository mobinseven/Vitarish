# Component Visual Patterns — Framework Implementation

This file translates the abstract design principles in `design-rules.md` into concrete framework choices.

The current implementation rule is:

```txt
journey state -> user feeling -> design principle -> HeroUI component/prop -> off-the-shelf styling
```

The goal is not to create a custom design system. The goal is to choose the right HeroUI v3.2.1 component, state, prop, variant, accessibility behavior, layout structure, and copy while preserving HeroUI's default visual appearance.

Detailed removal rules for the previous custom visual layer live in `heroui-v3.2.1-off-the-shelf-reset.md`.

---

## Official Framework Baseline

HeroUI v3.2.1 documentation checked on 2026-07-01 establishes the baseline used by this reference:

- React 19+.
- Tailwind CSS v4.
- `@heroui/styles`.
- `@heroui/react`.
- Main CSS imports Tailwind first, then HeroUI styles.
- HeroUI v3 styling is CSS-driven.
- No HeroUI provider is required for styling.
- Built-in light/dark themes are controlled from the root element.
- Next.js App Router uses `next-themes` for persisted/system theme selection.
- The page canvas uses HeroUI's built-in background and foreground semantics.
- `className`, inline style, BEM/global overrides, custom variables, wrapper components, and CSS-in-JS are customization mechanisms. Under the current platform policy, visual use of these mechanisms is not allowed unless explicitly exempted.

Official references:

- https://heroui.com/en/docs/react/getting-started/quick-start
- https://heroui.com/en/docs/react/getting-started/dark-mode
- https://heroui.com/en/docs/react/getting-started/styling
- https://heroui.com/en/docs/react/getting-started/theming

---

## Feeling-to-Pattern Reference

| Journey feeling | Design principle | Framework response | Off-the-shelf styling rule |
| --- | --- | --- | --- |
| Safe | Surface Philosophy / Interaction Philosophy | Card, Alert, clear labels, disabled states | Use default component appearance |
| Oriented | Typography / Space Philosophy | Heading, Breadcrumbs when available, Link, simple navigation | No custom app shell or visual chrome |
| In control | Interaction Philosophy | Button, Menu, Tabs, Select, explicit pending/disabled states | Use built-in states and variants |
| Confident | Interaction / Clean Minimal Philosophy | One primary action, concise copy, validation feedback | No custom CTA gradients or shadows |
| Warned | State Feedback Philosophy | Alert with warning/danger status, confirmation Modal | No custom callout classes |
| Recovered | State Feedback / Interaction Philosophy | Alert, retry Button, field invalid state | No raw custom error panels |
| Patient | Motion / State Feedback Philosophy | Spinner, Button pending state, loading copy | No custom loading decoration |
| Empty but not blocked | Clean Minimal Philosophy | Plain empty text or simple Card if needed | No decorative empty-state panel |
| Focused | Space / Clean Minimal Philosophy | Narrow flow, one primary action, simple form | No competing visual panels |
| Comparing | Space / Hierarchy Philosophy | Table, Tabs, Select, list pattern | No card-as-table visual styling |
| Exploring | Interaction / Space Philosophy | Tabs, Accordion, Menu, Link, Card | No decorative discovery surfaces |
| Confirming | Interaction / State Feedback Philosophy | Modal, Alert, primary/secondary Button pair | No custom backdrop or modal style |
| Correcting | State Feedback Philosophy | Form invalid state, error message, description | No placeholder-only guidance |
| Successful | State Feedback Philosophy | Alert success or concise success text | No custom status pill/dot system |
| Blocked | Interaction / State Feedback Philosophy | Disabled controls, Alert, explanation, next step | No hidden or unexplained blocked state |
| Detail-seeking | Interaction Philosophy | Tooltip, Accordion, Modal, inline description | No custom tooltip implementation |

---

## General Pattern Rules

1. **Start from feeling.** Name the journey feeling before selecting a component.
2. **Select a framework component.** Use HeroUI components before local wrappers.
3. **Use component state.** Use built-in pending, disabled, invalid, selected, danger, warning, and success states before styling.
4. **Use copy and structure.** Clarity often comes from wording, grouping, and action order rather than visual treatment.
5. **Keep default appearance.** Do not add a custom visual class to make the feeling stronger.
6. **Use structural layout only.** Layout wrappers may arrange components; they must not restyle components.
7. **Preserve accessibility.** Labels, focus behavior, keyboard behavior, errors, descriptions, and semantics are part of the pattern.

---

## Global App Baseline

**Feeling:** Stable, predictable, framework-native.

**Design principle:** Framework-Native Fidelity.

### Required Pattern

Use the official HeroUI CSS baseline, root theme behavior, and page canvas described in the framework docs. Keep the app-level visual baseline minimal.

### Rules

1. Main CSS imports Tailwind first and HeroUI styles second.
2. Do not define a parallel app-owned visual theme.
3. Do not override HeroUI visual variables for brand appearance.
4. The root page canvas uses HeroUI background/foreground semantics.
5. Next.js theme control uses a single root controller for light/dark/system.
6. App-level providers may provide data, theme selection, and framework context. They must not create custom visuals.

### Anti-Pattern

- Custom light/dark CSS variable blocks.
- Product-owned surface, accent, shadow, radius, motion, or focus tokens.
- Global custom classes for cards, app bars, sidebars, callouts, badges, or panels.
- Local CSS that makes the app look unlike standard HeroUI.

---

## Page Layout

**Feeling:** Oriented, focused, calm.

**Design principle:** Space Philosophy / Clean Minimal Philosophy.

### Required Pattern

Use semantic page structure and structural layout wrappers. Keep layout wrappers visually neutral.

```tsx
<main>
  <section>
    <h1>Page title</h1>
    {/* HeroUI components */}
  </section>
</main>
```

### Rules

1. Use headings to orient the user.
2. Use structural wrappers for width, flow, grouping, and responsive layout.
3. Use one primary task path per focused page section.
4. Do not use local page-shell primitives that restyle the page.
5. Do not add decorative backgrounds to make a page feel designed.
6. If a page feels sparse, improve grouping, copy, and component selection first.

### Anti-Pattern

```tsx
<AppShell>
<CenteredSurface>
<section className="custom-hero custom-gradient custom-shadow">
```

---

## Navigation

**Feeling:** Oriented, returning, in control.

**Design principle:** Interaction Philosophy / Space Philosophy.

### Required Pattern

Use framework links/navigation components or semantic navigation with neutral layout.

```tsx
<nav aria-label="Main navigation">
  {/* HeroUI Link or framework navigation component */}
</nav>
```

### Rules

1. Navigation should tell the user where they can go and where they are.
2. Use framework link/navigation behavior before local styling.
3. Active/current state should be accessible.
4. Do not create custom fixed pill app bars or glass navigation.
5. Do not create custom sidebar visual systems.

### Anti-Pattern

```tsx
<header className="ds-app-bar">
<nav className="sidebar">
<a className="sidebar-nav-item sidebar-nav-item-active">
```

---

## Card

**Feeling:** Safe, grouped, reviewable.

**Design principle:** Surface Philosophy.

### Required Pattern

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

### Rules

1. Use a Card when content needs containment, grouping, or review.
2. Use plain HeroUI Card appearance.
3. Do not add custom surface classes.
4. Do not add custom glass, blur, gradient, shadow, border, radius, or hover-lift styling.
5. If content does not need a surface, use semantic HTML instead of a Card.

### Anti-Pattern

```tsx
<Card className="glass-card">
<Card className="ds-surface-card ds-surface-card-elevated">
<Card className="custom-background custom-shadow custom-radius">
```

---

## Button

**Feeling:** In control, confident, recovering, confirming.

**Design principle:** Interaction Philosophy.

### Required Pattern

```tsx
import { Button } from "@heroui/react";

<Button onPress={save}>Save</Button>
<Button variant="secondary" onPress={cancel}>Cancel</Button>
<Button variant="danger" onPress={deleteItem}>Delete</Button>
<Button isPending={isSaving} onPress={save}>Save</Button>
```

### Rules

1. Use HeroUI Button for actions.
2. Use `onPress` for HeroUI button interaction.
3. Use built-in action semantics and state props.
4. Use one primary action per decision context.
5. Use `isPending` for waiting action state.
6. Use `isDisabled` for unavailable action state.
7. Do not make a button feel stronger with custom color, gradient, radius, shadow, hover, or transition classes.
8. Icon-only actions require accessible names and may need Tooltip.

### Anti-Pattern

```tsx
<Button className="custom-primary custom-hover custom-shadow">Save</Button>
<Button onClick={save}>Save</Button>
```

---

## Form Fields

**Feeling:** Focused, correcting, confident.

**Design principle:** Typography Philosophy / State Feedback Philosophy.

### Required Pattern

```tsx
import { Input, Label, TextField } from "@heroui/react";

<TextField name="email" isRequired>
  <Label>Email</Label>
  <Input value={email} onChange={handleEmailChange} />
</TextField>
```

### Rules

1. Use framework form components and accessibility model.
2. Always provide a visible label or accessible label.
3. Do not use placeholder as the only label.
4. Use built-in invalid/error APIs.
5. Use descriptions/help text through framework-supported patterns.
6. Place error feedback close to the field it explains.
7. Do not add custom backgrounds, borders, focus rings, shadows, or glass wrappers.

### Anti-Pattern

```tsx
<Input className="custom-input custom-focus-ring">
<p className="custom-error-text">Error</p>
```

---

## Alert / Status Feedback

**Feeling:** Warned, recovered, successful, blocked.

**Design principle:** State Feedback Philosophy.

### Required Pattern

```tsx
import { Alert } from "@heroui/react";

<Alert status="danger">
  <Alert.Indicator />
  <Alert.Content>Something went wrong.</Alert.Content>
</Alert>
```

### Rules

1. Use framework status components for success, danger, warning, and information states.
2. Status feedback must tell the user what happened and what to do next.
3. Use status close to the relevant action or content.
4. Do not invent custom callout classes, status badges, pills, dots, or color tokens.
5. Plain semantic text is acceptable when the state is minor and does not need a full status component.

### Anti-Pattern

```tsx
<section className="ds-callout ds-callout-danger">
<StatusBadge tone="success">Ready</StatusBadge>
<span className="custom-status-dot" />
```

---

## Table / Data Grid

**Feeling:** Comparing, reviewing, scanning.

**Design principle:** Space Philosophy / Hierarchy and Depth Philosophy.

### Required Pattern

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

### Rules

1. Use Table for tabular data.
2. Always include an accessible label.
3. Use framework sorting/selection/pagination patterns when available.
4. Do not build table-like card grids when the content is tabular.
5. Do not add custom row borders, hover colors, muted text systems, or glass wrappers.

### Anti-Pattern

```tsx
<Card className="glass-card">
  <div className="custom-grid-table custom-row-hover">
```

---

## Modal / Dialog

**Feeling:** Confirming, warned, detail-seeking.

**Design principle:** Interaction Philosophy / State Feedback Philosophy.

### Required Pattern

```tsx
import { Modal } from "@heroui/react";

<Modal isOpen={isOpen} onOpenChange={setIsOpen}>
  <Modal.Content>
    <Modal.Header>Confirm action</Modal.Header>
    <Modal.Body>Are you sure?</Modal.Body>
    <Modal.Footer>
      <Button variant="secondary" onPress={cancel}>Cancel</Button>
      <Button variant="danger" onPress={confirm}>Delete</Button>
    </Modal.Footer>
  </Modal.Content>
</Modal>
```

### Rules

1. Use Modal for interruptions, confirmation, and focused details.
2. Use the framework's focus and dismissal behavior.
3. Do not create custom modal overlays, backdrops, blur, glass, or slot visuals.
4. Confirmation dialogs must state the consequence and provide clear actions.
5. Avoid stacked modals.

### Anti-Pattern

```tsx
<div className="custom-backdrop custom-blur">
<div className="custom-modal custom-glass">
```

---

## Tooltip / Disclosure

**Feeling:** Detail-seeking, uncertain, exploring.

**Design principle:** Interaction Philosophy.

### Required Pattern

```tsx
import { Tooltip } from "@heroui/react";

<Tooltip content="What this action does">
  <Button aria-label="More information">?</Button>
</Tooltip>
```

### Rules

1. Use Tooltip for short contextual explanation.
2. Use Accordion or Modal for longer detail.
3. Tooltip content should be concise.
4. Tooltip must not hide required information.
5. Do not create a custom tooltip implementation.

---

## Loading State

**Feeling:** Patient, waiting, still in control.

**Design principle:** Motion Philosophy / State Feedback Philosophy.

### Required Pattern

```tsx
import { Spinner } from "@heroui/react";

<Spinner />
<Button isPending={isSaving} onPress={save}>Save</Button>
```

### Rules

1. Use Spinner for general waiting states.
2. Use Button pending state for action-owned waiting.
3. Loading copy should explain what is happening when the wait is meaningful.
4. Do not add custom loading cards, glow, animated decoration, or custom spinners.

---

## Empty State

**Feeling:** Empty but not blocked, recovering, oriented.

**Design principle:** Clean Minimal Philosophy.

### Required Pattern

```tsx
<p>No items found.</p>
```

or, if containment is useful:

```tsx
<Card>
  <Card.Content>No items found.</Card.Content>
</Card>
```

### Rules

1. Keep empty states plain and helpful.
2. Explain what happened.
3. Offer one next action when there is a natural next action.
4. Do not use decorative illustrations, custom glass panels, gradients, or dramatic empty-state visuals.

---

## Badge / Small Status

**Feeling:** Successful, blocked, warned, oriented.

**Design principle:** State Feedback Philosophy / Color Philosophy.

### Required Pattern

Use a framework-provided status/badge/chip component when available for the exact use case. If a framework component is not available, prefer concise text or Alert depending on severity.

### Rules

1. Do not keep a local `StatusBadge` primitive just to preserve custom visual status treatment.
2. Status labels must be concise.
3. Important status must be understandable without relying only on color.
4. Do not add custom status dots, custom pill backgrounds, or custom color tokens.

---

## Structural Layout Classes

Structural classes are allowed when they do not restyle HeroUI components.

Allowed purposes:

- flow,
- grouping,
- responsive layout,
- width constraints,
- alignment,
- overflow behavior,
- page structure.

Forbidden purposes:

- custom color,
- custom shadow,
- custom radius,
- custom blur,
- custom gradient,
- custom border,
- custom typography voice,
- custom hover/focus/active visual state,
- custom dark-mode appearance,
- custom component anatomy spacing.

---

## Wrapper Components

Wrapper components are allowed only when they do not change framework visual appearance.

Allowed wrapper responsibilities:

- data mapping,
- feature composition,
- accessibility wiring,
- form integration,
- routing,
- error normalization,
- conditional rendering.

Forbidden wrapper responsibilities:

- adding default visual classes,
- adding custom visual variants,
- applying custom theme tokens,
- rebranding framework components,
- recreating a local design system over HeroUI.

---

## Required Review Checks

A component pattern passes when:

1. The journey feeling is named or clearly implied.
2. The component choice matches the feeling.
3. The implementation uses HeroUI or semantic HTML.
4. The visual appearance remains off-the-shelf.
5. Any custom styling has an explicit exemption.
6. Accessibility behavior is preserved.
7. The implementation does not reintroduce the previous custom visual layer.
