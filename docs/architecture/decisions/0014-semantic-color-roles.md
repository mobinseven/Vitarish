# ADR-0014: Semantic Color Roles

Date: 2026-10-10
Status: Proposed
Scope: Every Web app and shared package under `src/Hosts/Web`
Owner: Platform Architecture
Supersedes:
Superseded by:
Related: [ADR-0008](0008-standardize-app-experience-boundaries.md)

## Context

Dashboard hosts paint almost every element with the theme accent: sidebar
background and icons, cards, avatar tiles, header icons, and primary buttons.
With one hue meaning everything, pages read as monochromatic, nothing stands
out, and color cannot help users tell areas or records apart.

Color meaning is currently partial and scattered. `accent.css` owns accent
chrome and the action roles (`actionRoleClassName`), HeroUI owns status colors,
the owner host defines its own `--highlight`/`--support` tokens, and the Web
`AGENTS.md` treats any new color as exceptional. There is no single place that
answers "what color is reminders?" and no check that stops a feature from
inventing one.

## Decision

Every color in Web feature code expresses exactly one registered meaning,
drawn from five axes with closed vocabularies: brand, status, action role,
business context, and identity. Components ask for a role, never for a hue.

Business-context colors are keyed by business context, not by navigation
entry, so a context keeps its hue in every app and across navigation changes.

- `@vitarish/ui` owns the role vocabulary, the token table, and typed
  accessors. Color roles are design tokens, which ADR-0008 places in
  `@vitarish/ui`.
- Host themes may retune role values but not add, remove, or rename roles.
- Feature code consumes roles only.
- Every role is a light/dark token family whose hues share fixed lightness and
  chroma, so all hues carry the same visual weight and contrast.

The axes, the context list, token shape, and usage rules are maintained in the
[Web color roles convention](../../hosts/web/color-roles.md). Adding a context
or role is a convention change; adding an axis or changing role ownership
amends this ADR.

## Consequences

- Pages become colorful and consistent: a given context has the same hue in
  every app, and users learn areas by color.
- The sidebar no longer takes an accent tint; the active item remains the only
  accent-filled nav element.
- Context hues plus status and brand use most of the distinguishable hue
  wheel, so adjacent context hues sit close together and icon pairing is
  mandatory. Growing the context list means accepting closer hues or merging
  contexts.
- Owner app and `shared/clinic` adopt first. Other hosts keep their current
  colors until migrated; the lint rule covers only migrated packages, and each
  package joins it when it migrates.
- Sign-in method tiles move from bespoke hues to identity slots.
- The owner host's `--highlight`/`--support` tokens stay as the action-role
  tokens for `open` and `edit`.

## Alternatives Considered

### Option 1: Semantic roles keyed by business context (chosen)

Gives the product a stable color vocabulary that survives navigation changes
and is shared across apps.

### Option 2: One hue per navigation entry

Simpler to map, but the same concept would drift to different hues across
apps and change whenever navigation is reorganized.

### Option 3: Feature packages own their context colors

Keeps domain names out of `@vitarish/ui`, but scatters the map, so nothing
guarantees uniqueness or spacing between hues. Rejected for a single
authoritative table.

### Option 4: Do nothing

Pages stay monochromatic, and each new color stays a one-off decision with no
shared meaning.

## Validation / Enforcement

- `npm run lint:layout` rejects color literals and Tailwind palette color
  utilities in the feature code of migrated packages. A rule cannot tell
  whether an accent use is the brand role, so code review checks that.
- A test checks that the registry and stylesheet agree, that context hues
  avoid the status and brand bands, and every role family's `foreground` on
  `base` and `soft-foreground` on `soft` in both modes (WCAG AA, 4.5:1 for
  text).
- A color legend page in the owner screenshot suite renders every role in both
  modes for visual review.
- The Web `AGENTS.md` color section points to the convention.

## Notes

- Exact token values are tuned during implementation against the contrast
  test.
- Follow-up: migrate platform-admin, patients, vitarish-pm, vitarish-landing,
  and agent-runtime, adding each to the lint rule's scope.
