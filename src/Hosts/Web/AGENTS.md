# Web agent routing

This is the React/Next.js workspace. The repository and scoped `AGENTS.md`
instructions own React feature, transport, trust, and placement guidance.

Add skills only for the decision actually touched:

| Touched decision | Load |
| --- | --- |
| Route topology, feature entry points, navigation, or intent contracts | `platform-route-change-audit` |
| Route/page shells, main landmark, viewport geometry, outer scrolling, shared gutters, sidebar, or aside | `platform-web-layout-policy` |

For Web-only work, add no other skill. A backend change made alongside Web work
follows that backend subtree's `AGENTS.md` too. Never edit generated API output
manually.

## Localization convention

- **[MUST]** All localization keys used in components must be defined.

## Icon conventions

- **[MUST]** Render interface icons using Font Awesome CSS classes. The icon name must be recognizable directly from the JSX, for example `fa-light fa-users`.
- **[MUST]** Do not embed `<svg>`, `<path>`, copied path data, or SVG-based icon components for interface icons.
- Use `FaIcon` from `@vitarish/web-ui` when its shared behavior is useful; otherwise, a plain semantic `<i>` with Font Awesome classes is acceptable.
- SVG remains allowed for non-icon artwork such as logos, illustrations, diagrams, and data visualizations.
- Decorative icons must be hidden from assistive technology. Icon-only controls must have an accessible label.

## Button conventions

- **[MUST]** All buttons must have localized tooltips and icons. so in mobile view they can be shrunk to icon and tooltips to avoid confusing users.

## Date conventions

- **[MUST]** All dates must be displayed in localized formats. 
- **[MUST]** All date conversions must be used by central conventions.
