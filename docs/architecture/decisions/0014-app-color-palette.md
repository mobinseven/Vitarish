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
background and icons, cards, avatar tiles, header icons, and buttons. Other
colors are one-off: Tailwind palette utilities, hex values, and host tokens
added for a single widget. Nothing limits how many colors a page uses or says
what a color means.

## Decision

Each app has one small palette, used the same way on every page:

- **Neutral** for surfaces, text, and icons.
- **Accent** for the current item, the single primary action, and focus.
- **Secondary and tertiary**, the host theme's `highlight` and `support`
  tokens, for action kinds and for emphasis.
- **Status** (success, warning, danger) only where something has that status.
  Status may appear in addition to the palette.

Color does not tell areas of the product apart. A page that serves several
purposes separates them by layout: sections, headings, and panes.

- The host theme owns the palette values and may change them at any time.
- `@vitarish/ui` owns shared chrome (sidebar, header icons, action roles) and
  paints it only with palette and status tokens.
- Feature code uses palette and status tokens only; no other colors.
- Usage rules live in the
  [Web color convention](../../hosts/web/color-roles.md).

## Consequences

- Pages use at most four colors plus status, and the palette can be retuned in
  one theme file.
- Header icons, sidebar icons, avatars, and sign-in tiles become neutral;
  avatars show a status color when their record has a status.
- Owner app and `shared/clinic` adopt first. Other hosts keep their current
  colors until migrated; the lint rule covers only migrated packages.

## Alternatives Considered

### Option 1: One app-wide palette (chosen)

Small, predictable, and easy to retune.

### Option 2: A hue per business area

Tried first. Ten areas filled the usable hue wheel, neighbouring hues were hard
to tell apart, and pages showed many colors at once.

### Option 3: Do nothing

Accent keeps meaning everything, and each new color stays a one-off decision.

## Validation / Enforcement

- `npm run lint:layout` rejects Tailwind palette color utilities and color
  literals in the feature code of migrated packages. Whether an accent use is
  the current item, primary action, or focus is checked in review.
- The owner screenshot suite captures `/color-legend`, which renders the
  palette, action roles, status colors, and header icons in both modes.
- The Web `AGENTS.md` color section points to the convention.

## Notes

- Follow-up: migrate platform-admin, patients, vitarish-pm, vitarish-landing,
  and agent-runtime, adding each to the lint rule's scope.
