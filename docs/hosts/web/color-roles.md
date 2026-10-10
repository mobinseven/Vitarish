# Web Color Roles

Status: Active  
Owner: Platform host: Web
Scope: Every Web app and shared package under `src/Hosts/Web`
Last reviewed: 2026-10-10

Decision and rationale:
[ADR-0014](../../architecture/decisions/0014-semantic-color-roles.md).

Every color in Web feature code expresses exactly one registered meaning.
Components ask `@vitarish/ui` for a role, never for a hue. The vocabularies
and values live in code; this page says how to use them.

## Axes

| Axis | Meaning | Vocabulary | Where it appears |
|---|---|---|---|
| Brand | This product; where you are; what to do next | HeroUI `accent` | Current nav item, the single primary action, focus ring |
| Status | Outcome or risk | HeroUI `success`, `warning`, `danger`, `default` | `Alert`, status chips, validation |
| Action role | What a button does | [`ActionRole`](../../../src/Hosts/Web/shared/ui/src/actions/actionRole.ts) | Buttons, through `actionRoleClassName` |
| Business context | Which part of the business a thing belongs to | [`BusinessContext`](../../../src/Hosts/Web/shared/ui/src/color/businessContext.ts) | Sidebar icons, page and section `HeaderIcon`, entity tiles, cross-link chips, card accent stripe |
| Identity | Telling records or options apart; no meaning | [`IdentitySlot`](../../../src/Hosts/Web/shared/ui/src/color/identityColor.ts) | Per-record avatars, sign-in method tiles, user tags, chart series |

Hue values for business contexts and identity slots are in
[`hue-roles.css`](../../../src/Hosts/Web/shared/ui/src/hue-roles.css).

## Using a hue role

`contextColorClassName(context)` or `identityColorClassName(slot)` sets the
tint on an element. That element and its descendants paint with the `tint`
utilities: `bg-tint`, `text-tint-foreground`, `bg-tint-soft`,
`text-tint-soft-foreground`, and `text-tint` for icons.

## Usage rules

- Feature code consumes roles only. No color literals, Tailwind palette color
  utilities (`bg-teal-500`), or accent tokens outside the brand role.
- Large surfaces (page, cards, sidebar background) stay neutral. Color goes on
  small, high-signal elements: icon tiles, nav icons, chips, header icons, and
  accent stripes.
- Color never carries meaning alone: pair every colored role with an icon or
  text label.
- Status colors are reserved for status. A context or identity hue never
  signals success, warning, or failure.
- A business context is chosen by what the thing is, not by which navigation
  entry shows it.
- Host theme files may retune role values; they may not add, remove, or
  rename roles.

## Checks

- `npm run lint:layout` rejects Tailwind palette colors, arbitrary color
  values, and color literals in the feature code of migrated packages. The
  rule's `colorIncludeRoots` lists them; add a package when it migrates. Host
  theme files are exempt.
- `npm run screenshots:private-practice-owner` captures `/color-legend`, which
  renders every role in light and dark mode.

## Adding a role

Add the entry to its vocabulary in `@vitarish/ui` and give it a value in the
owning stylesheet; `hue-roles.test.ts` fails until the two agree, the hue is
outside the reserved bands, and every family member passes contrast. A new
axis, or a change to who owns roles, is an amendment to ADR-0014.
