# Component Visual Patterns — Framework Implementation

This file translates `design-rules.md` into concrete HeroUI v3.2.1 component choices.

Current mapping:

```txt
journey state -> user feeling -> design principle -> HeroUI component/prop -> off-the-shelf styling
```

The goal is to choose the correct HeroUI component, state, prop, variant, accessibility behavior, layout structure, and copy while preserving HeroUI's default visual appearance. Detailed old-style removal rules live in `heroui-v3.2.1-off-the-shelf-reset.md`. Non-conflicting legacy accessibility, interaction, RTL/LTR, copy, and layout rules live in `preserved-pattern-rules.md`.

## Official Framework Baseline

HeroUI v3.2.1 documentation checked on 2026-07-01 establishes this baseline:

- React 19+.
- Tailwind CSS v4.
- `@heroui/styles` and `@heroui/react`.
- Main CSS imports Tailwind first, then HeroUI styles.
- HeroUI v3 styling is CSS-driven.
- No HeroUI provider is required for styling.
- Built-in light/dark themes are controlled from the root element.
- Next.js App Router uses `next-themes` for persisted/system theme selection.
- The page canvas uses HeroUI's built-in background and foreground semantics.
- `className`, inline style, BEM/global overrides, custom variables, wrapper components, and CSS-in-JS are customization mechanisms. Visual use is not allowed unless explicitly exempted.

## Feeling-to-Pattern Reference

| Journey feeling | Design principle | Framework response | Off-the-shelf styling rule |
| --- | --- | --- | --- |
| Safe | Surface / Interaction | Card, Alert, clear labels, disabled states | Use default component appearance |
| Oriented | Typography / Space | Heading, Breadcrumbs when available, Link, simple navigation | No custom app shell or visual chrome |
| In control | Interaction | Button, Menu, Tabs, Select, pending/disabled states | Use built-in states and variants |
| Confident | Interaction / Clean Minimal | One primary action, concise copy, validation feedback | No custom CTA gradients or shadows |
| Warned | State Feedback | Alert with warning/danger status, confirmation Modal | No custom callout classes |
| Recovered | State Feedback / Interaction | Alert, retry Button, field invalid state | No raw custom error panels |
| Patient | Motion / State Feedback | Spinner, Button pending state, loading copy | No custom loading decoration |
| Empty but not blocked | Clean Minimal | Plain empty text or simple Card if needed | No decorative empty-state panel |
| Focused | Space / Clean Minimal | Narrow flow, one primary action, simple form | No competing visual panels |
| Comparing | Space / Hierarchy | Table, Tabs, Select, list pattern | No card-as-table visual styling |
| Exploring | Interaction / Space | Tabs, Accordion, Menu, Link, Card | No decorative discovery surfaces |
| Confirming | Interaction / State Feedback | Modal, Alert, primary/secondary Button pair | No custom backdrop or modal style |
| Correcting | State Feedback | Form invalid state, error message, description | No placeholder-only guidance |
| Successful | State Feedback | Alert success or concise success text | No custom status pill/dot system |
| Blocked | Interaction / State Feedback | Disabled controls, Alert, explanation, next step | No hidden or unexplained blocked state |
| Detail-seeking | Interaction | Tooltip, Accordion, Modal, inline description | No custom tooltip implementation |

## General Pattern Rules

1. Start from the journey feeling.
2. Select a HeroUI component before local wrappers.
3. Use built-in pending, disabled, invalid, selected, warning, danger, and success states before styling.
4. Use copy and structure before visual treatment.
5. Keep HeroUI default appearance.
6. Use structural layout only.
7. Preserve accessibility.
8. Apply the preserved rules in `preserved-pattern-rules.md`.

## Global App Baseline

Use the official HeroUI CSS baseline, root theme behavior, and page canvas described in the framework docs. Keep the app-level visual baseline minimal.

Rules:

1. Main CSS imports Tailwind first and HeroUI styles second.
2. Do not define a parallel app-owned visual theme.
3. Do not override HeroUI visual variables for brand appearance.
4. The root page canvas uses HeroUI background/foreground semantics.
5. Next.js theme control uses a single root controller for light/dark/system.
6. Providers may provide data, theme selection, and framework context. They must not create custom visuals.

## Page Layout

Use semantic page structure and structural layout wrappers. Keep wrappers visually neutral.

```tsx
<main>
  <section>
    <h1>Page title</h1>
    {/* HeroUI components */}
  </section>
</main>
```

## Navigation

Use framework links/navigation components or semantic navigation with neutral layout.

```tsx
<nav aria-label="Main navigation">
  {/* HeroUI Link or framework navigation component */}
</nav>
```

Navigation must expose available routes and current location accessibly. Do not create custom fixed pill app bars, glass navigation, or custom sidebar visual systems.

## Card

Use Card when content needs containment, grouping, or review.

```tsx
import { Card } from "@heroui/react";

<Card>
  <Card.Header>
    <Card.Title>Title</Card.Title>
  </Card.Header>
  <Card.Content>
    Content
  </Card.Content>
</Card>
```

Rules:

1. Use plain HeroUI Card appearance.
2. Use HeroUI Card compound anatomy, including `Card.Title` for titles inside `Card.Header`.
3. Do not add custom surface classes, glass, blur, gradient, shadow, border, radius, or hover-lift styling.
4. If content does not need a surface, use semantic HTML instead.

## Button

Use Button for actions.

```tsx
import { Button } from "@heroui/react";

<Button onPress={save}>Save</Button>
<Button variant="secondary" onPress={cancel}>Cancel</Button>
<Button variant="danger" onPress={deleteItem}>Delete</Button>
<Button isPending={isSaving} onPress={save}>Save</Button>
```

Rules:

1. Use `onPress`, not `onClick`.
2. Use built-in action semantics and state props.
3. Keep one primary action per decision context.
4. Use `isPending` and `isDisabled` for action state.
5. Icon-only actions require accessible names and may need Tooltip.
6. Do not make buttons stronger with custom color, gradient, radius, shadow, hover, or transition classes.

## Form Fields

Use framework form components and their accessibility model.

```tsx
import { Input, Label, TextField } from "@heroui/react";

<TextField name="email" isRequired>
  <Label>Email</Label>
  <Input value={email} onChange={handleEmailChange} />
</TextField>
```

Rules:

1. Always provide a visible label or accessible label.
2. Never use placeholder as the only label.
3. Use built-in invalid/error APIs.
4. Use descriptions/help text through framework-supported patterns.
5. Place error feedback close to the field it explains.
6. Do not add custom backgrounds, borders, focus rings, shadows, or glass wrappers.

## Alert / Status Feedback

Use Alert for success, danger, warning, information, recovery, and blocked states that require user attention.

```tsx
import { Alert } from "@heroui/react";

<Alert status="danger">
  <Alert.Indicator />
  <Alert.Content>
    <Alert.Title>Something went wrong.</Alert.Title>
  </Alert.Content>
</Alert>
```

Rules:

1. Status feedback must tell the user what happened and what to do next.
2. Use HeroUI Alert compound anatomy, including `Alert.Title` and `Alert.Description` inside `Alert.Content` when the alert has title/description text.
3. Use status close to the relevant action or content.
4. Do not invent custom callout classes, status badges, pills, dots, or color tokens.
5. Plain semantic text is acceptable when the state is minor and does not need a full status component.

## Table / Data Grid

Use Table for tabular data.

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

1. Always include an accessible label.
2. Use framework sorting/selection/pagination patterns when available.
3. Do not build table-like card grids when the content is tabular.
4. Do not add custom row borders, hover colors, muted text systems, or glass wrappers.

## Modal / Dialog

Use Modal for interruptions, confirmation, and focused details.

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

Rules:

1. Use the framework's focus and dismissal behavior.
2. Do not create custom modal overlays, backdrops, blur, glass, or slot visuals.
3. Confirmation dialogs must state the consequence and provide clear actions.
4. Avoid stacked modals.

## Tooltips

Use Tooltip for concise contextual explanation.

```tsx
import { Tooltip } from "@heroui/react";

<Tooltip content="What this action does">
  <Button aria-label="More information">?</Button>
</Tooltip>
```

Rules:

1. **All components that convey meaning must include a tooltip** to ensure accessibility and clarity when their meaning is not already explicit from visible text.
2. Use HeroUI's `Tooltip` component to wrap elements that need explanation.
3. Apply tooltips to icons, status badges, abbreviations, buttons with icon-only content, and any component where the meaning may not be immediately clear.
4. Tooltip text should be concise and describe the component's purpose or state.
5. Use Accordion or Modal for longer detail.
6. Tooltip must not hide required information.
7. Do not create a custom tooltip implementation.

## Loading State

Use Spinner for general waiting states and Button pending state for action-owned waiting.

```tsx
import { Spinner } from "@heroui/react";

<Spinner />
<Button isPending={isSaving} onPress={save}>Save</Button>
```

Loading copy should explain what is happening when the wait is meaningful. Do not add custom loading cards, glow, animated decoration, or custom spinners.

## Empty State

Keep empty states plain and helpful.

```tsx
<p>No items found.</p>
```

or, if containment is useful:

```tsx
<Card>
  <Card.Content>No items found.</Card.Content>
</Card>
```

Rules:

1. Explain what happened.
2. Offer one next action when there is a natural next action.
3. Do not show a blank table/grid.
4. Do not use decorative illustrations, custom glass panels, gradients, or dramatic empty-state visuals.

## Badge / Small Status

Use a framework-provided status/badge/chip component when available for the exact use case. If a framework component is not available, prefer concise text or Alert depending on severity.

Rules:

1. Status labels must be concise.
2. Important status must be understandable without relying only on color.
3. Status badges or badge-like status indicators must include a tooltip when their meaning is not fully obvious from visible text.
4. Do not keep local custom status visuals just to preserve old styling.

## Structural Layout Classes

Structural classes are allowed for flow, grouping, responsive layout, width constraints, alignment, overflow, and page structure.

They are not allowed for custom color, shadow, radius, blur, gradient, border, typography voice, hover/focus/active visual state, dark-mode appearance, or component anatomy spacing.

## Wrapper Components

Wrapper components are allowed for data mapping, feature composition, accessibility wiring, form integration, routing, error normalization, and conditional rendering.

They must not add default visual classes, custom visual variants, theme tokens, rebranding, or a local design system over HeroUI.

## Required Review Checks

A component pattern passes when:

1. The journey feeling is named or clearly implied.
2. The component choice matches the feeling.
3. The implementation uses HeroUI or semantic HTML.
4. The visual appearance remains off-the-shelf.
5. Any custom styling has an explicit exemption.
6. Accessibility behavior is preserved.
7. The implementation does not reintroduce the previous custom visual layer.
8. Meaningful icons, badges, abbreviations, icon-only buttons, and unclear state indicators have concise HeroUI tooltips.
