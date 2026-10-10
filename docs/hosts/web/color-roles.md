# Web Color Convention

Status: Active  
Owner: Platform host: Web
Scope: Every Web app and shared package under `src/Hosts/Web`
Last reviewed: 2026-10-10

Decision and rationale:
[ADR-0014](../../architecture/decisions/0014-app-color-palette.md).

## Palette

| Color | Token | Use |
|---|---|---|
| Neutral | `surface`, `default`, `foreground`, `muted` | Surfaces, text, header and sidebar icons, avatars, tiles |
| Accent | `accent` | Current item, the single primary action, focus |
| Secondary | `highlight` | Action kinds and emphasis |
| Tertiary | `support` | Action kinds and emphasis |
| Status | `success`, `warning`, `danger` | Only where something has that status |

Each host theme owns the values; change them there, never in feature code.

## Rules

- Use only the tokens above. No Tailwind palette colors (`bg-teal-500`),
  arbitrary color values, or color literals. White and black are allowed for
  text and scrims over images.
- Buttons take their color from their action kind through
  `actionRoleClassName` (`open`, `edit`, `create`, `remove`).
- Header icons and sidebar icons are neutral. `HeaderIcon` takes a status
  `tone` only when the section's content has that status.
- Avatars and record tiles are neutral, and show their record's status color
  when it has one.
- A colored element never carries meaning alone: pair it with an icon or text.
- A page that serves several purposes separates them by layout (sections,
  headings, panes), not by color.

## Checks

- `npm run lint:layout` rejects palette colors, arbitrary color values, and
  color literals in the feature code of migrated packages. The rule's
  `colorIncludeRoots` lists them; add a package when it migrates. Host theme
  files are exempt.
- `npm run screenshots:private-practice-owner` captures `/color-legend`, which
  renders the palette, action roles, status colors, and header icons in light
  and dark mode.
