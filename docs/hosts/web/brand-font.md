# Web Brand Font

Status: Active  
Owner: Platform host: Web
Scope: Web hosts that render Persian (RTL) text in Ahang
Last reviewed: 2026-10-25

Ahang is the Vitarish brand typeface for Persian text. It is a licensed
FontIran font and this repository is public, so the font file is never
committed, published as a CI artifact, or built into an image.

## Use in a host

`@vitarish/ui/typography.css` owns the `@font-face`, the `--font-ahang` role,
and the tone utilities. A host that uses Ahang:

1. Imports `@vitarish/ui/typography.css` in `src/app/globals.css` and maps
   `--font-sans` (or another role) to `var(--font-ahang)` under
   `html[dir="rtl"]`.
2. Calls `syncBrandFonts` from `scripts/sync-brand-fonts.mjs` in its
   `next.config.ts`, which copies the font into the gitignored
   `public/fonts/ahang/` for local runs.
3. Imports the `brand_fonts` snippet in its site blocks in
   `src/Hosts/deployment/Caddyfile`, which serves the font on deployed hosts.

Landing, owner, and platform-admin use it today.

## Tone

The variable font has two axes: weight (`wght` 100–900, through `font-weight`)
and sharpness (`shar` 0 soft – 100 sharp). Running text is soft. Pick a named
tone by what the text should feel like:

| Utility | Weight | Sharpness |
|---|---|---|
| `font-tone-calm` | 300 | 0 |
| `font-tone-warm` | 450 | 0 |
| `font-tone-confident` | 650 | 60 |
| `font-tone-urgent` | 800 | 100 |
| `font-tone-celebratory` | 900 | 30 |

`font-sharp-0` … `font-sharp-100` with the normal weight utilities are for
experimenting. Animate a change with
`transition-[font-weight,font-variation-settings]`.

## Getting the font

- **Local:** set `VITARISH_BRAND_FONT_DIR` to the folder holding
  `AhangVariableGX.woff` from the licensed Ahang webfont package. Without it,
  hosts log a warning and fall back to system fonts.
- **Server:** the ingress mounts the folder that `.env.shared` names as
  `VITARISH_BRAND_FONT_DIR`; see
  [server deployment bundles](../server-deployment-bundles.md).
