# Web Color Convention

Status: Active  
Owner: Platform host: Web
Scope: Every Web app and shared package under `src/Hosts/Web`
Last reviewed: 2026-10-11

Decision and rationale:
[ADR-0014](../../architecture/decisions/0014-app-color-palette.md).

## Palette

| Color | Token | Use |
|---|---|---|
| Neutral | `surface`, `default`, `foreground`, `muted` | Surfaces, text, header and sidebar icons, avatars, tiles, generic buttons |
| Accent | `accent` | Current navigation item, focus, and the save button of forms that are not a clinical operation |
| Chroma | `chroma-primary` to `chroma-senary` | Colors a feature package gives a fixed meaning, such as clinical operations and sign-in methods |
| Status | `success`, `warning`, `danger` | Only where something has that status |

Each host theme owns the values; change them there, never in feature code.
A host without chroma tokens falls back to neutral.

The owner app's chroma colors come from
[this palette](https://coolors.co/palette/ff99c8-fec8c3-fcf6bd-d0f4de-a9def9-e4c1f9).
They are pastels, so one set serves light and dark mode. Each color's text is
its own hue, as light and as chromatic as sRGB allows while keeping 4.8:1
contrast. Every pair has the same contrast, and the text is clearly tinted.

| Token | Color | Assigned to |
|---|---|---|
| `chroma-primary` | Sky `#A9DEF9` | Visit |
| `chroma-secondary` | Peach `#FEC8C3` | Procedure |
| `chroma-tertiary` | Lavender `#E4C1F9` | Prescription |
| `chroma-quaternary` | Pink `#FF99C8` | Appointment |
| `chroma-quinary` | Mint `#D0F4DE` | Unassigned |
| `chroma-senary` | Cream `#FCF6BD` | Unassigned |

## Buttons

| Kind | Examples | Color | Class |
|---|---|---|---|
| Generic | Open, view, back, cancel, add, edit, filter | Neutral (`tertiary`, `outline`, or `ghost` variant; HeroUI `secondary` has accent text) | none |
| Delete | Remove, delete | Soft danger | `removeButtonClassName` from `@vitarish/ui` |
| Clinical operation | Starting the operation and its form's commit button | The operation's chroma color | `clinicOperationButtonClassName` from `@vitarish/clinic` |
| Other save | Save in patient edit, settings, templates, services | Accent (`primary` variant) | none |

Clinical operations, owned by `@vitarish/clinic`:

| Operation | Covers | Chroma |
|---|---|---|
| `visit` | Visits and notes | `chroma-primary` |
| `procedure` | Procedures, procedure events, procedure templates, surgeries | `chroma-secondary` |
| `prescription` | Prescriptions | `chroma-tertiary` |
| `appointment` | Reservations, check-in, shifts, booking setup | `chroma-quaternary` |

An operation keeps its color on every page, so repeat visitors find it by
color on hub pages such as the dashboard and patient profile.

## Rules

- Use only the tokens above. No Tailwind palette colors (`bg-teal-500`),
  arbitrary color values, or color literals. White and black are allowed for
  text and scrims over images.
- Chroma colors take their meaning from one owner per meaning. Do not paint a
  chroma color directly in feature code; ask its owner for a class.
- Header icons and sidebar icons are neutral. `HeaderIcon` takes a status
  `tone` only when the section's content has that status.
- Avatars and record tiles are neutral, and show their record's status color
  when it has one.
- A colored element never carries meaning alone: pair it with an icon or text.
- A page that serves several purposes separates them by layout (sections,
  headings, panes), not by color.

## Adding a meaning

A new clinical operation or another fixed set of meanings takes the existing
chroma colors through its own owner, as sign-in methods do. A meaning that
appears on the same page as the clinical operations takes an unassigned
color (mint or cream) and is added to the table above.

## Checks

- `npm run lint:layout` rejects palette colors, arbitrary color values, and
  color literals in the feature code of migrated packages. The rule's
  `colorIncludeRoots` lists them; add a package when it migrates. Host theme
  files are exempt.
- The owner app's chroma contrast test checks every chroma color against its
  `-foreground` token at WCAG AA (4.5:1). Retuning a color or its text must
  keep it passing.
- `npm run screenshots:private-practice-owner` captures `/color-legend`, which
  renders the palette, button kinds, operation colors, status colors, and
  header icons in light and dark mode.
