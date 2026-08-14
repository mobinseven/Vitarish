# Web host routing

- Route topology, feature entry points, navigation, and intent contracts: update every applicable route tree, registry, mapper, journey default, localization key, test, and generated transport source; never edit generated API output manually.
- Route/page shells, main landmark, viewport geometry, outer scrolling, shared gutters, sidebar, and aside: classify the surface and reuse the matching `@vitarish/web-ui` capability; feature-internal grids and intentional workspace-pane scrolling remain local.

## Named values

- **[MUST]** Reuse the authoritative owner for route and intent values, API
  operation IDs, localization keys, query/cache keys, feature flags, and action
  IDs. Keep these values in the existing route, contract, localization, query
  key, or feature-configuration mechanism; do not repeat magic strings across
  route trees, mappers, hooks, components, tests, or app registration. Inline
  framework syntax, CSS utility classes, and exact-value tests remain allowed
  when they are not application contracts or business rules.

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
