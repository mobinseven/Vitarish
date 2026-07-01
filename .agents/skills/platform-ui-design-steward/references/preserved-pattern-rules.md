# Preserved Component Pattern Rules

This reference records non-conflicting rules from the previous `platform-ui-design-steward` references that must be preserved while moving the Web host to HeroUI v3.2.1 off-the-shelf styling.

These rules are not custom visual styling. They are accessibility, interaction, layout, component-choice, RTL/LTR, copy, and state-management rules. They must survive the off-the-shelf reset.

The scan compared the previous `design-rules.md` and `component-patterns.md` against the current HeroUI off-the-shelf policy. Rules that only required the old glassmorphism/custom CSS layer were not restored. Rules that remain valid under off-the-shelf HeroUI were restored here and should be reflected in `component-patterns.md` when the affected section is edited.

## Scan Result

I found **16 non-conflicting rule groups** that should not have been removed or weakened.

## Restored Rule Groups

### 1. Responsive feedback

Preserve the rule that interactive components must expose clear feedback for hover, press, focus, pending, disabled, selected, and active states.

Use HeroUI's built-in states, props, render props, data attributes, or accessibility behavior before adding local styling.

Examples of preserved concepts:

- `isPressed`
- `isHovered`
- `isFocusVisible`
- `isPending`
- active/pressed state
- visible focus state

### 2. Loading awareness

Preserve the rule that loading must be visible and local to the interaction when possible.

Use:

- `Button isPending` for action-owned loading,
- `Spinner` for general waiting,
- clear loading copy when the user needs context.

Do not replace this with custom loading panels, glow effects, or animated decoration.

### 3. Contextual clarity

Preserve the rule that unclear meaning must be explained near the element that carries the meaning.

Use HeroUI `Tooltip` for short clarification, and use Accordion or Modal for longer details.

### 4. Accessibility safety

Preserve the rule that focus indicators, labels, descriptions, error messages, disabled states, pending states, and semantic structure must remain visible and predictable.

A visual reset must not remove accessibility affordances.

### 5. Anti-feelings

Preserve the old anti-feeling checks, adapted to the new off-the-shelf policy:

| Prohibited feeling | What to avoid now |
| --- | --- |
| Industrial | harsh borders, cramped layouts, dense tables without breathing room |
| Playful | bouncy animations, bright multi-color decoration, whimsical effects |
| Flat | no hierarchy, no grouping, no state distinction |
| Heavy | oppressive surfaces, thick borders, dense visual weight |

Use framework component choice and layout to avoid these. Do not create custom visuals to solve them.

### 6. Card meaning clarity

Preserve the rule that cards conveying status or non-obvious meaning need clarification.

Use concise visible text first. Use HeroUI Tooltip when the card, icon, badge, or state is not self-explanatory.

### 7. Button discipline

Preserve these button rules:

- Use `onPress`, not `onClick`, for HeroUI Button interaction.
- Use semantic built-in variants/statuses when appropriate.
- Keep one primary action per decision context.
- Button labels should be concise: verb or verb+noun, not sentences.
- Icon-only buttons need an accessible name and a HeroUI Tooltip when meaning is not obvious.
- Use built-in icon placement (`startContent` / `endContent`) or direction-aware ordering rather than hard-coded left/right placement.

### 8. Form structure

Preserve these form rules:

- Always include a label; never use placeholder as the only label.
- Use `w-full` when a field must fill its form column/container.
- Use consistent structural gaps between fields.
- Use framework-supported description/helper text rather than detached custom paragraphs when the component supports it.
- Use built-in invalid/error APIs such as `isInvalid` and `errorMessage` where available.
- Do not hard-code left/right label alignment.
- Use `text-start` / `text-end` for helper/error alignment when alignment is needed.

### 9. Modal/dialog structure

Preserve these modal/dialog rules:

- Use clear title, body, and footer structure.
- Footer actions should be grouped and aligned consistently.
- Use logical spacing and direction-aware ordering.
- Use one modal at a time; do not stack modals.
- Do not build custom backdrops; use the framework's overlay/focus behavior.
- Confirmation dialogs must state the consequence and provide clear actions.

### 10. Table/data-grid semantics

Preserve these table rules:

- Always include `aria-label`.
- Use Table for tabular data.
- Column headers should be visually and semantically distinct through the framework pattern.
- Empty state must not be a blank table/grid; show a clear empty message.
- Use `text-start` instead of `text-left` where alignment is needed.
- Let the framework handle RTL column behavior; do not override column order manually.

### 11. Navigation directionality

Preserve these navigation rules:

- Navigation must clearly indicate where the user can go and, when possible, where the user currently is.
- Active/current state must be accessible, not just visual.
- Do not hard-code left/right directional layout.
- Use logical properties and direction-aware icon behavior.

### 12. Page layout discipline

Preserve these page layout rules:

- Pages need natural margins; content should not touch viewport edges.
- Related elements should be grouped by proximity.
- Unrelated elements should be separated by space, not by decorative borders.
- Use consistent structural spacing for page sections.
- Use semantic heading hierarchy.
- Do not hard-code directional padding/margin.

### 13. Status text and badges

Preserve these status rules:

- Status text must be concise: one word or short phrase.
- Important status must not rely only on color.
- Status badges or badge-like indicators need a tooltip when their meaning is not fully obvious from visible text.
- Do not duplicate framework status behavior with local custom badge visuals.

### 14. Empty-state behavior

Preserve these empty-state rules:

- Empty states should feel calm, not broken.
- Show a primary message.
- Show secondary guidance when it helps the user recover or continue.
- Do not show a blank table/grid.
- Centering is acceptable as structural layout when it improves comprehension.

### 15. Tooltip requirements

Preserve the tooltip rules exactly in intent:

- All components that convey meaning must include a tooltip when the meaning is not already explicit.
- Use HeroUI's `Tooltip` component to wrap elements that need explanation.
- Apply tooltips to icons, status badges, abbreviations, buttons with icon-only content, and any component where meaning may not be immediately clear.
- Tooltip text should be concise and describe the component's purpose or state.
- Wrap the meaningful/interactive element, not an unrelated container.
- Use the `placement` prop when placement matters; HeroUI handles RTL flipping.
- Never use tooltips for required information; use visible text, Alert, Accordion, or Modal for required content.
- Never create a custom tooltip implementation.

### 16. RTL/LTR and translation safety

Preserve all directionality and translation-safety rules:

- Use logical properties instead of hard-coded left/right.
- Use `ms-*` instead of `ml-*`.
- Use `me-*` instead of `mr-*`.
- Use `ps-*` instead of `pl-*`.
- Use `pe-*` instead of `pr-*`.
- Use `start-*` instead of `left-*`.
- Use `end-*` instead of `right-*`.
- Use `border-s-*` instead of `border-l-*`.
- Use `border-e-*` instead of `border-r-*`.
- Use `text-start` / `text-end` instead of `text-left` / `text-right`.
- Use `justify-start` / `justify-end` instead of `justify-left` / `justify-right`.
- Place icons through framework slots or direction-aware ordering.
- Directional icons such as arrows and chevrons must flip in RTL mode.
- Assume all user-facing text will be translated.
- Do not truncate or size content based on English-only assumptions.

## Rules Not Restored

The following previous rules were intentionally not restored because they directly depend on the old custom visual layer:

- Always apply `glass-card`.
- Use `ds-surface-card` for tone variants.
- Use `ds-app-bar`, `sidebar-nav-item`, or `sidebar-nav-item-active`.
- Use `AppShell`, `SectionHeader`, or `StatusBadge` as visual primitives.
- Use custom `--accent`, `--border`, `--muted`, `--shadow-*`, or `--radius-*` tokens.
- Use custom glass backdrops, gradients, row hover colors, or local badge/callout styles.

Those rules are replaced by HeroUI off-the-shelf component selection and the reset inventory.
