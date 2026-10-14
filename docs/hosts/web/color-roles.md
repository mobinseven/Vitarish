# Web Color Convention

Status: Active  
Owner: Platform host: Web
Scope: Every Web app and shared package under `src/Hosts/Web`
Last reviewed: 2026-10-14

Decision and rationale:
[ADR-0014](../../architecture/decisions/0014-app-color-palette.md).

## Palette

| Color | Token | Use |
|---|---|---|
| Neutral | `surface`, `default`, `foreground`, `muted` | Surfaces, text, header and sidebar icons, avatars, tiles, generic buttons |
| Accent | `accent` | Current navigation item, focus, a page's one lead action, every form's commit button, a control whose state narrows what a list shows (soft accent) |
| Anchor | `anchor`, `anchor-foreground` | The patient hero band, the tint of section header bands, and in light mode the hue of `foreground` |
| Chroma | `chroma-primary` to `chroma-senary` | Colors a feature package gives a fixed meaning, such as status chips, patient tags, and sign-in methods |
| Status | `success`, `warning`, `danger` | Only where something has that status |

A control whose state narrows what a list shows, such as active filters, a
non-default sort, or a day other than today, takes the soft accent while that
state is on, and is neutral otherwise. Its count badge is solid accent.

Each host theme owns the values; change them there, never in feature code.
A host without chroma tokens falls back to neutral.

The owner app pairs a deep magenta accent with a deep navy anchor
(`oklch(27.9% 0.082 275)`) on a cool off-white canvas: one strong hue, one
deep ground, and white surfaces, so nothing competes with the lead action.
Danger is a vermilion (hue 38) kept away from the magenta accent, so errors
never read as brand.

### Design-system override

The canvas, ink, muted text, and danger values above are not yet in the saved
HeroUI design system that `theme.css` exports. The owner approved overriding
`--background`, `--foreground`, `--muted`, and `--danger` in the owner app's
`globals.css` on 2026-10-14. Remove that override block once the saved design
system carries these values and `theme.css` is re-exported from it.

The owner app's chroma colors are deep tones at the accent's OKLCH lightness
(52%), each as chromatic as sRGB allows up to a shared cap, so none outweighs
another. They appear as soft chips and sign-in tiles, never as filled
buttons. They carry white text, so one set serves light and dark mode.

| Token | Color | Used by |
|---|---|---|
| `chroma-primary` | Blue `oklch(52% 0.14 250)` | Completed status, tags, password sign-in |
| `chroma-secondary` | Burnt orange `oklch(52% 0.14 45)` | Tags, phone sign-in |
| `chroma-tertiary` | Violet `oklch(52% 0.16 300)` | Waitlisted status, tags, email sign-in |
| `chroma-quaternary` | Rose `oklch(52% 0.16 350)` | Tags, passkey sign-in |
| `chroma-quinary` | Green `oklch(52% 0.11 160)` | Tags |
| `chroma-senary` | Ochre `oklch(52% 0.1 85)` | Tags |

Patient tags are the exception to one meaning per color: tags are defined by
users, so `patientTagClassName` in `@vitarish/clinic` picks one of all six
chroma colors from the tag text. A tag keeps its color on every page, which
helps users spot it; the color carries no other meaning, and the tag text is
always shown. Tags are soft chips, mixed as HeroUI mixes its soft status chips
(15% fill, text in the chip's own hue), so tags and status chips look alike.

Reservation status chips are soft chips colored through
`reservationStatusChipProps` in `@vitarish/clinic`. Each status a user acts on
has its own color, so statuses shown together never share one; statuses that
need no attention stay neutral.

| Status | Color |
|---|---|
| Pending | `warning` |
| Confirmed | `success` |
| Present | `accent` |
| Completed | `chroma-primary` |
| Waitlisted | `chroma-tertiary` |
| Expired | `danger` |
| Cancelled, rejected | Neutral |

Surgery status chips follow the same rule through `surgeryStatusChipProps` in
`@vitarish/clinic`, matching the reservation statuses of the same meaning.

| Status | Color |
|---|---|
| Planned | `success` |
| Completed | `chroma-primary` |
| Cancelled | Neutral |

## Buttons

| Kind | Examples | Color | Class |
|---|---|---|---|
| Generic | Open, view, back, cancel, add, edit, filter | Neutral (`tertiary`, `outline`, or `ghost` variant; HeroUI `secondary` has accent text) | none |
| Delete | Remove, delete | Soft danger | `removeButtonClassName` from `@vitarish/ui` |
| Lead action | The one action a page exists for, such as Create prescription on the patient page | Accent (`primary` variant) | none |
| Commit | The submit button of every form, sheet, or dialog, clinical or not | Accent (`primary` variant) | none |
| Other operation starters | New visit, check in, plan surgery, create reservation, dashboard row actions | Neutral (`tertiary` or `outline`) with the operation's icon | none |

A page has at most one filled lead action. Operations are told apart by their
icon and label, not by color: the icon stays visible when the button shrinks
to icon-only on narrow screens.

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
  chip uses HeroUI's soft recipe, and every filled button is the accent
  button.
- Section widgets take their header band from `@vitarish/ui`: the widget frame
  is a light anchor tint and the content stays on the plain surface. Do not
  recolor a section per page.
- A colored element never carries meaning alone: pair it with an icon or text.
- A page that serves several purposes separates them by layout (sections,
  headings, panes), not by color.

## Adding a meaning

A new clinical operation takes no color: give its buttons an icon and follow
the button table above. Another fixed set of meanings shown as chips or tiles
takes the existing chroma colors through its own owner, as sign-in methods
do, and is added to the table above.

## Checks

- `npm run lint:layout` rejects palette colors, arbitrary color values, and
  color literals in the feature code of migrated packages. The rule's
  `colorIncludeRoots` lists them; add a package when it migrates. Host theme
  files are exempt.
- The owner app's chroma contrast test checks every chroma color and the
  anchor against their `-foreground` tokens at WCAG AA (4.5:1). Retuning a
  color or its text must keep it passing.
- `npm run screenshots:private-practice-owner` captures `/color-legend`, which
  renders the palette, button kinds, status colors, and header icons in light and dark mode.
