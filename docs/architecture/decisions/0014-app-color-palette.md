# ADR-0014: App Color Palette

Date: 2026-10-10
Status: Proposed
Scope: Every Web app and shared package under `src/Hosts/Web`
Owner: Platform Architecture
Supersedes:
Superseded by:
Related: [ADR-0008](0008-standardize-app-experience-boundaries.md)

## Context

Dashboard hosts paint almost every element with the theme accent: sidebar
background and icons, cards, avatar tiles, header icons, and buttons. With one
color meaning everything, it means nothing. Other colors are one-off: Tailwind
palette utilities, hex values, and host tokens added for a single widget.

Hub pages such as the dashboard and patient profile are visited many times a
session and start many operations. Routine buttons (open, edit, add) are as
loud there as the clinical operations the page exists for.

## Decision

Each app has one small palette, used the same way on every page:

- **Neutral** for surfaces, text, icons, and generic buttons.
- **Accent** for the current navigation item, focus, and the save button of
  forms that are not a clinical operation.
- **Six chroma colors** (`chroma-primary` to `chroma-senary`) that a
  feature package gives a fixed meaning. `@vitarish/clinic` assigns one to
  each clinical operation (visit and note, procedure, prescription,
  appointment). An operation keeps its color on every page, on both the
  button that starts it and its form's commit button.
- **Status** (success, warning, danger) only where something has that status.
  Delete buttons are soft danger.

Color does not tell areas of the product apart. A page that serves several
purposes separates them by layout: sections, headings, and panes.

- The host theme owns all values and may change them at any time.
- `@vitarish/ui` owns shared chrome and the chroma button class, and paints
  them only with palette and status tokens.
- Each meaning of a chroma color has one owner that exposes a class; feature
  code asks that owner and never paints a chroma color directly.
- Usage rules live in the
  [Web color convention](../../hosts/web/color-roles.md).

## Consequences

- Pages use only neutral, accent, the chroma colors, and status, and the
  palette can be retuned in one theme file. Four chroma colors are assigned;
  two stay free for future meanings.
- Generic buttons become neutral, so the operations on hub pages stand out and
  users learn them by color.
- The owner app's chroma colors are pastels whose text is the same hue,
  darkened, in both modes. One value set reads on light and dark backgrounds.
- Header icons, sidebar icons, and avatars become neutral; avatars show a
  status color when their record has a status.
- Owner app and `shared/clinic` adopt first. Other hosts keep their current
  colors until migrated; the lint rule covers only migrated packages.

## Alternatives Considered

### Option 1: Neutral palette plus operation colors (chosen)

Keeps accent meaningful and gives hub pages a small, learnable set of colors.

### Option 2: A hue per business area

Tried first. Ten areas filled the usable hue wheel, neighbouring hues were hard
to tell apart, and pages showed many colors at once.

### Option 3: A color per generic action kind (open, edit, create)

Colored the most frequent, least important buttons, so routine actions
competed with clinical ones.

### Option 4: Do nothing

Accent keeps meaning everything, and each new color stays a one-off decision.

## Validation / Enforcement

- `npm run lint:layout` rejects Tailwind palette color utilities and color
  literals in the feature code of migrated packages. Correct use of accent and
  chroma classes is checked in review.
- A contrast test checks every chroma color against its text color at WCAG
  AA (4.5:1).
- The owner screenshot suite captures `/color-legend`, which renders the
  palette, button kinds, operation colors, status colors, and header icons in
  both modes.
- The Web `AGENTS.md` color and button sections point to the convention.

## Notes

- Follow-up: migrate platform-admin, patients, vitarish-pm, vitarish-landing,
  and agent-runtime, adding each to the lint rule's scope.
