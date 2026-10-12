# Web Color Convention

Status: Active  
Owner: Platform host: Web
Scope: Every Web app and shared package under `src/Hosts/Web`
Last reviewed: 2026-10-12

Decision and rationale:
[ADR-0014](../../architecture/decisions/0014-app-color-palette.md).

## Palette

| Color | Token | Use |
|---|---|---|
| Neutral | `surface`, `default`, `foreground`, `muted` | Surfaces, text, header and sidebar icons, avatars, tiles, generic buttons |
| Accent | `accent` | Current navigation item, focus, the save button of forms that are not a clinical operation, and a control whose state narrows what a list shows (soft accent) |
| Chroma | `chroma-primary` to `chroma-senary` | Colors a feature package gives a fixed meaning, such as clinical operations and sign-in methods |
| Status | `success`, `warning`, `danger` | Only where something has that status |

A control whose state narrows what a list shows, such as active filters, a
non-default sort, or a day other than today, takes the soft accent while that
state is on, and is neutral otherwise. Its count badge is solid accent.

Each host theme owns the values; change them there, never in feature code.
A host without chroma tokens falls back to neutral.

The owner app's chroma colors are deep tones at the accent's OKLCH lightness
(52%), each as chromatic as sRGB allows up to a shared cap, so they fill a
button as the accent does and none outweighs another. They carry white text,
so one set serves light and dark mode, as the accent does.

| Token | Color | Assigned to |
|---|---|---|
| `chroma-primary` | Blue `oklch(52% 0.14 250)` | Visit |
| `chroma-secondary` | Burnt orange `oklch(52% 0.14 45)` | Procedure |
| `chroma-tertiary` | Violet `oklch(52% 0.16 300)` | Prescription |
| `chroma-quaternary` | Rose `oklch(52% 0.16 350)` | Unassigned (too close to the pink accent) |
| `chroma-quinary` | Green `oklch(52% 0.11 160)` | Appointment |
| `chroma-senary` | Ochre `oklch(52% 0.1 85)` | Unassigned |

Patient tags are the exception to one meaning per color: tags are defined by
users, so `patientTagClassName` in `@vitarish/clinic` picks one of all six
chroma colors from the tag text. A tag keeps its color on every page, which
helps users spot it; the color carries no other meaning, and the tag text is
always shown. Tags are soft chips, mixed as HeroUI mixes its soft status chips
(15% fill, text in the chip's own hue), so tags and status chips look alike.

Reservation status chips are soft chips colored through
`reservationStatusChipProps` in `@vitarish/clinic`. Each status a user acts on
has its own color, so statuses shown together never share one; statuses that
need no attention stay neutral. Statuses reuse operation chroma colors because
a soft chip and a filled button are different kinds of element.

| Status | Color |
|---|---|
| Pending | `warning` |
| Confirmed | `success` |
| Present | `accent` |
| Completed | `chroma-primary` |
| Waitlisted | `chroma-tertiary` |
| Expired | `danger` |
| Cancelled, rejected | Neutral |

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
| `appointment` | Reservations, check-in, shifts, booking setup | `chroma-quinary` |

In dense repeated rows, such as the dashboard visit list, give the operation
button `variant="secondary"`: it keeps the neutral fill and only the text takes
the operation's chroma. The dashboard's sort and filter controls likewise use
`secondary` for accent text.

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
- Patient and user avatars are neutral and show an icon, never initials: two
  letters of a name can spell an unintended word. A record's status shows in
  its status chip. Record tiles are neutral, and show their record's status
  color when it has one.
- Elements of the same kind share one look whatever their source: every soft
  chip uses HeroUI's soft recipe, and every filled button follows the accent
  button.
- A colored element never carries meaning alone: pair it with an icon or text.
- A page that serves several purposes separates them by layout (sections,
  headings, panes), not by color.

## Adding a meaning

A new clinical operation or another fixed set of meanings takes the existing
chroma colors through its own owner, as sign-in methods do. A meaning that
appears on the same page as the clinical operations takes an unassigned
color (cream) and is added to the table above.

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
