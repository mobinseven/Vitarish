# Journey Feeling to HeroUI Component Mapping

This reference maps user-journey feelings to off-the-shelf HeroUI component choices.

The mapping must never be used to justify custom styling. The purpose is to choose the correct framework component, state, prop, variant, copy structure, layout structure, or accessibility behavior while keeping HeroUI's default visual appearance.

```txt
journey state -> user feeling -> design principle -> HeroUI component/prop -> off-the-shelf styling
```

## Mapping Rules

1. Start with the user journey state.
2. Identify the user's feeling in that state.
3. Select the closest design principle from `design-rules.md`.
4. Select the closest HeroUI component or prop from `component-patterns.md` or `heroui-react`.
5. Keep default HeroUI styling.
6. Use layout, copy, component choice, state, and accessibility before visual styling.
7. If no HeroUI component fits, use semantic HTML with structural classes only.
8. If custom visual styling is proposed, it needs an explicit `UI-DESIGN-EXEMPTION` comment and must not become a reusable visual language.

## Core Feeling Map

| Journey feeling | Design intent | Preferred HeroUI/off-the-shelf response | Avoid |
| --- | --- | --- | --- |
| Safe | The user needs trust and predictability | Plain `Card`, `Alert`, visible labels, disabled states, clear copy | Glass cards, custom reassurance colors, decorative trust badges |
| Oriented | The user needs to know where they are | Semantic headings, `Breadcrumbs` when available, simple navigation, clear page title | Custom app shells, hero gradients, decorative navigation chrome |
| In control | The user needs clear available actions | `Button`, `Menu`, `Tabs`, `Select`, explicit disabled/pending states | Multiple custom primary actions, custom button colors, hover effects |
| Confident | The user is ready to proceed | Primary action through HeroUI `Button`, concise action copy, validation feedback | Custom CTA gradients, oversized hero panels, visual exaggeration |
| Warned | The user must understand risk | HeroUI `Alert` with warning/danger status, confirm `Modal` for destructive actions | Custom callout classes, status pills, red borders, custom icon colors |
| Recovered | The user needs a path after failure | HeroUI `Alert`, retry `Button`, field-level `isInvalid` and `errorMessage` | Custom error panels, raw red text, unstructured error blocks |
| Patient | The user is waiting | `Spinner`, `Button isPending`, skeleton if supported by HeroUI, clear loading copy | Custom loading cards, glow effects, animated decoration |
| Empty but not blocked | The user has no data yet | Plain text empty state, simple `Card` only if a container is useful, one next action | Decorative empty-state illustrations, glass panels, gradient prompts |
| Focused | The user must complete one task | Minimal structure, one primary action, simple form fields | Competing cards, decorative side panels, multiple primary buttons |
| Comparing | The user needs to inspect alternatives | HeroUI `Table`, `Tabs`, `Select`, or list patterns with semantic labels | Card-as-table grids, custom dividers, custom hover row colors |
| Exploring | The user is browsing possibilities | `Tabs`, `Accordion`, `Menu`, `Link`, simple cards | Custom discovery panels, decorative gradients, visual noise |
| Confirming | The user needs to verify before action | `Modal`, `Alert`, summary text, primary/secondary `Button` pair | Custom backdrop, custom glass modal, custom button styling |
| Correcting | The user must fix input | HeroUI form invalid states, error messages, descriptions, focused field | Placeholder-only guidance, raw red text, custom error classes |
| Successful | The user needs confirmation | HeroUI `Alert` success status or concise plain success text | Custom green badges, status dots, decorative success panels |
| Blocked | The user cannot continue | Disabled controls, `Alert`, explanation, one resolution path | Hidden disabled state, custom opacity rules, unexplained blocked UI |
| Detail-seeking | The user wants more context | `Tooltip`, `Accordion`, `Modal`, inline description | Custom tooltip implementation, custom info badges |
| Returning | The user resumes a known path | Stable navigation, preserved state, simple headings | New custom page chrome, visually surprising layout changes |

## Component Selection Guidance

### Choose `Button` when the feeling is action-oriented

Use for: proceed, save, cancel, retry, delete, confirm, sign in, ask, submit.

Use HeroUI state props before styling:

- `isPending` for waiting.
- `isDisabled` for unavailable actions.
- Built-in variants/status where appropriate.
- `onPress` for interaction.

Do not add custom visual classes to make an action feel stronger. Strength comes from placement, copy, and single-primary-action discipline.

### Choose form components when the feeling is correction or input confidence

Use HeroUI form controls, labels, descriptions, invalid states, and error messages. The feeling of confidence comes from accessible structure, not custom borders or focus rings.

### Choose `Alert` when the feeling is warning, error, success, or important context

Use HeroUI statuses. Do not create custom callouts, pills, status dots, or color systems.

### Choose `Table` when the feeling is comparison or review of structured data

Use semantic tabular components with `aria-label`. Do not use cards and div grids to simulate a table when the content is tabular.

### Choose `Modal` when the feeling is confirmation or interruption

Use HeroUI modal behavior for focus, dismissal, and hierarchy. Do not create custom overlays or glass backdrops.

### Choose plain semantic HTML when the feeling is simple reading or orientation

Use headings, paragraphs, lists, and structural wrappers. Do not introduce decorative panels because a screen feels sparse.

## Layout Guidance

Journey feelings may influence layout, but not visual identity.

Allowed layout responses:

- Use a narrower container for focused tasks.
- Use a two-column layout for compare/review states.
- Use a simple vertical stack for forms and step-by-step flows.
- Use one clear primary action per decision point.
- Place errors close to the fields or actions they affect.
- Use copy and hierarchy to reduce uncertainty.

Forbidden layout responses:

- Adding custom glass surfaces.
- Adding decorative gradients or orbs.
- Adding custom shadows/radius to create hierarchy.
- Adding custom active-state colors.
- Creating a new wrapper component whose main job is visual styling.

## Review Checklist

For any UI created from a journey map:

1. The journey state is named.
2. The user feeling is named.
3. The design principle is selected.
4. The HeroUI component/prop is selected.
5. The styling remains off-the-shelf.
6. Any custom visual styling has an explicit exemption.
7. Accessibility is preserved.
