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
- **Accent** for the current navigation item, focus, a page's one lead
  action, and every form's commit button.
- **Anchor**, a deep navy that grounds the accent: it fills the patient hero
  band, lightly tints section header bands, and is the hue of light-mode
  text.
- **Six chroma colors** (`chroma-primary` to `chroma-senary`) that a
  feature package gives a fixed meaning on chips and tiles, such as status
  chips, patient tags, and sign-in methods. Clinical operations take no
  color: their buttons are told apart by icon and label.
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
- Generic buttons and operation starters are neutral, so a page's one filled
  accent action leads and nothing competes with it.
- A first revision filled each operation button with its own chroma color.
  On hub pages four unrelated hues sat in one row beside the accent, and the
  owner judged the result colorless around the edges and loud without
  harmony. The palette now follows one strong hue (magenta accent), one deep
  ground (navy anchor), and white surfaces. A first pastel set had already
  made filled buttons look cheap and glared in dark mode.
- Chips of every kind (status, patient tag) use HeroUI's soft recipe, so
  elements of the same kind look alike whatever their source.
- Header icons, sidebar icons, and avatars become neutral. Avatars show an
  icon, never initials, which can spell an unintended word.
- Owner app and `shared/clinic` adopt first. Other hosts keep their current
  colors until migrated; the lint rule covers only migrated packages.

## Alternatives Considered

### Option 1: Accent lead action, navy anchor, icon-identified operations (chosen)

One filled action per page and a deep anchor give the page a focal point
and a calm hierarchy; operations stay recognisable by icon.

### Option 1a: Neutral palette plus a chroma fill per operation

Used first. Users could learn operations by color, but several unrelated
saturated fills on one page clashed with each other and with the accent.

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
- A contrast test checks every chroma color and the anchor against their
  text colors at WCAG AA (4.5:1).
- The owner screenshot suite captures `/color-legend`, which renders the
  palette, button kinds, status colors, and header icons in both modes.
- The Web `AGENTS.md` color and button sections point to the convention.

## Notes

- Follow-up: migrate platform-admin, patients, vitarish-pm, vitarish-landing,
  and agent-runtime, adding each to the lint rule's scope.
