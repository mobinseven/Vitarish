# Web host instructions

These instructions apply to all artifacts under `src/Hosts/Web` and specialize the repository-wide rules in [`AGENTS.md`](../../../AGENTS.md).

## Icon conventions

- **[MUST]** Render interface icons with Font Awesome CSS classes. The actual Font Awesome icon name must be recognizable in the source, such as `users` / `fa-users`; do not introduce aliases that hide which icon is rendered.
- **[MUST]** Do not embed `<svg>`, `<path>`, copied path data, SVG icon components, or SVG-based icon-library imports for interface icons.
- Prefer `FaIcon` from `@vitarish/web-ui` for shared behavior. A plain `<i>` with explicit Font Awesome classes is acceptable for simple local markup.
- SVG is permitted only for non-icon artwork such as logos, illustrations, diagrams, and data visualizations. Register each source-code exception with its reason in `scripts/validate-icon-conventions.mjs`.
- Decorative icons must use `aria-hidden="true"`. Icon-only controls must expose an accessible name through the control, not through decorative icon content.
- Run `npm run validate:icons` from `src/Hosts/Web` after changing Web presentation code.
