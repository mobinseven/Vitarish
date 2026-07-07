# Landing Page Integration Plan

## Goal
Integrate the template at `docs/templates/landing-page/` into the Next.js 15 App Router project at `src/Hosts/Web/`, adapting it from a standalone React Router + Vite SPA into a feature module within the existing application.

---

## Phase 0: Dependency Installation

```bash
pnpm add @iconify/react
```

`motion` and `react-aria-components` are already in `package.json`. The gravity-ui icon set is bundled as JSON (2353 lines), not fetched at runtime.

---

## Phase 1: CSS Integration

**Target:** `src/Hosts/Web/src/app/globals.css`

Append (do not replace) the following blocks, in order, after the existing `@custom-variant` line:

1. **HeroUI theme variables** — Copy the oklch color definitions from `heroui-theme.css` into `:root, .light, [data-theme="light"]` and `.dark, [data-theme="dark"]` selectors. The existing `globals.css` already has Tailwind + HeroUI Pro theme imports; the template's theme provides the specific oklch values for the blueprint aesthetic (accent at `oklch(54.76% 0.2500 264.09)`).

2. **Brand tokens** — Copy the `:root` block from `brand/tokens.css` (13 lines of CSS custom properties mapping brand aliases to design system variables).

3. **Font stacks** — Copy the `:root` block defining `--font-sans` (Inter), `--font-display` (Space Grotesk), `--font-mono` (JetBrains Mono).

4. **Design tokens (light/dark)** — Copy the oklch surface, muted, border, separator, background, foreground variables from `index.css` lines 14-40. These define the blueprint color palette.

5. **Loading shimmer animation** — Copy the `.loading-shimmer` class and `@keyframes loading-shimmer` block.

6. **Base layer typography** — Copy the `@layer base` block that sets `font-family: var(--font-sans)` on body and `font-family: var(--font-display)` on headings.

7. **Blueprint utilities** — Copy the entire `@layer utilities` block containing `.bp-label`, `.bp-label-accent`, `.bp-grid`, `.bp-grid-fine`, `.bp-grid-mask`, `.bp-ruler`, `.bp-ruler-accent`, `.bp-dotted-x`.

8. **Component utilities** — Copy the `@layer components` block with `.button` and `.navbar` classes.

**Fonts:** Add Google Fonts links for Inter, Space Grotesg, and JetBrains Mono to `app/layout.tsx` in the `<head>` via `next/font/google` or a `<link>` tag.

---

## Phase 2: Shared Blueprint Components

**New file:** `src/Hosts/Web/src/components/blueprint/index.tsx`

Copy the 7 exported components from `docs/templates/landing-page/components/blueprint/index.tsx`:

- `Reveal` — scroll-triggered fade-up using `motion/react`
- `DrawLineX` — animated horizontal dotted line
- `SectionLabel` — section header with colored dot and figure index
- `RulerLine` — full-width ruler with tick marks
- `CornerMarks` — four corner bracket decorations
- `Figure` — image container with grid overlay and tooltip
- `Annotation` — monospaced uppercase label

Changes needed during copy:
- No import path changes required (all imports are from `@heroui/react` and `motion/react`, both installed).
- If the `Tooltip` import from `@heroui/react` causes SSR issues in the Figure component, lazy-load it with `next/dynamic`.

---

## Phase 3: Landing Feature Module

### 3a: Brand Assets

**New file:** `src/Hosts/Web/src/features/landing/brand/metadata.json`

Adapt the template's metadata:
```json
{
  "name": "Vitarish",
  "shortName": "Vitarish",
  "tagline": "Clinic management, reimagined",
  "description": "Modern clinic management platform for appointments, patients, and workflows."
}
```

Remove the `assets` section (CDN-hosted Axon Grid isotipo) — the brand components have SVG fallback rendering that draws a symbol with the brand name.

**New file:** `src/Hosts/Web/src/features/landing/brand/tokens.css` — Copy as-is (13 lines, delegates to design system variables).

**New file:** `src/Hosts/Web/src/features/landing/brand/index.tsx` — Copy the `BrandLogotipo`, `BrandIsotipo`, `BrandLogo`, `BrandMark` components and the `brand` metadata export. Update the import path for `metadata.json`. The inline SVG fallback (circle + lightning bolt) will render since we removed the CDN asset references.

### 3b: Data Layer

**New file:** `src/Hosts/Web/src/features/landing/data/figures.ts`

Copy `data/site.ts` and:
- Rename the export from `figures` (keep same name, just change import path in consumers)
- Keep the `STYLE_BRIEF` constant and `Figure` type
- Keep all 8 CDN-hosted image URLs as-is (they're public assets)
- Remove the `prompt` field from each figure if desired, or keep for tooltip display

### 3c: Icon System

Copy the entire `icons/` directory from the template:
- `icons/iconify.tsx` — wraps `@iconify/react` with gravity-ui prefix resolution
- `icons/gravity-ui/icons.json` — bundled icon set (~2353 lines)
- `icons/gravity-ui/index.js` — re-exports metadata

All imports use `@iconify/react` which we installed in Phase 0.

### 3d: Section Components

Copy all 12 section files from `components/sections/`. For each, apply these transformations:

**Import path changes:**
| Old | New |
|---|---|
| `../../data/site` | `../data/figures` |
| `../../icons/iconify` | `../icons/iconify` |
| `../blueprint` | `@/components/blueprint` |
| `../../brand` | `../brand` |
| `@heroui/react` | `@heroui/react` (unchanged) |
| `@heroui-pro/react` | `@heroui-pro/react` (unchanged) |
| `motion/react` | `motion/react` (unchanged) |

**Localization:** Every hardcoded string becomes a `t("landing.section.key")` call. The `useLocalizedText()` hook must be added to each section component that contains user-facing text. Components that are purely structural (like `theme-toggle.tsx`) need no localization.

**Auth buttons in header.tsx:** Add two buttons using `useAuthSession()`:
- If not authenticated: show "Login" and "Sign Up" buttons linking to `Routes.Login.Index` and a future signup route
- If authenticated: the page won't render (handled by root page gate), but include a fallback link to `Routes.Home`

**Router links:** The template uses `<a href="#hash">` for anchor navigation, which works in Next.js without changes. No `next/link` conversion needed for hash anchors. Only convert to `<Link>` if any navigation targets actual routes (login/signup).

### 3e: Landing Page Orchestrator

**New file:** `src/Hosts/Web/src/features/landing/landing-page.tsx`

This is the `"use client"` component that composes all 11 sections:

```tsx
import { Header } from "./sections/header";
import { Hero } from "./sections/hero";
import { TrustStrip } from "./sections/trust-strip";
import { Pillars } from "./sections/pillars";
import { Architecture } from "./sections/architecture";
import { Metrics } from "./sections/metrics";
import { CodePreview } from "./sections/code-preview";
import { FeatureRows } from "./sections/feature-rows";
import { Pricing } from "./sections/pricing";
import { FinalCta } from "./sections/final-cta";
import { Footer } from "./sections/footer";

export function LandingPage() {
  return (
    <div className="bg-background text-foreground min-h-dvh">
      <Header />
      <main>
        <Hero />
        <TrustStrip />
        <Pillars />
        <Architecture />
        <Metrics />
        <CodePreview />
        <FeatureRows />
        <Pricing />
        <FinalCta />
      </main>
      <Footer />
    </div>
  );
}
```

---

## Phase 4: Localization Catalog Expansion

### New keys structure

Add a `landing` top-level namespace to both `fa.ts` and `en.ts`. The structure mirrors the section hierarchy:

```
landing.meta.title
landing.meta.description
landing.header.navPlatform
landing.header.ctaStartBuilding
landing.header.loginAction
landing.header.signupAction
landing.hero.headline
landing.hero.subheading
landing.hero.ctaPrimary
landing.hero.ctaSecondary
landing.trustStrip.eyebrow
landing.trustStrip.companies.{n}.mark
landing.trustStrip.companies.{n}.name
landing.pillars.sectionLabel
landing.pillars.heading
landing.pillars.subheading
landing.pillars.items.{n}.title
landing.pillars.items.{n}.body
landing.pillars.items.{n}.tags.{m}
landing.architecture.sectionLabel
landing.architecture.heading
landing.architecture.nodes.{n}.title
landing.architecture.nodes.{n}.body
landing.metrics.sectionLabel
landing.metrics.items.{n}.label
landing.metrics.items.{n}.note
landing.codePreview.sectionLabel
landing.codePreview.heading
landing.featureRows.sectionLabel
landing.featureRows.heading
landing.featureRows.items.{n}.title
landing.featureRows.items.{n}.body
landing.featureRows.items.{n}.points.{m}
landing.pricing.sectionLabel
landing.pricing.heading
landing.pricing.subheading
landing.pricing.plans.{n}.name
landing.pricing.plans.{n}.desc
landing.pricing.plans.{n}.features.{m}
landing.pricing.plans.{n}.cta
landing.finalCta.sectionLabel
landing.finalCta.headline
landing.finalCta.body
landing.finalCta.ctaPrimary
landing.finalCta.ctaSecondary
landing.footer.productColumn.*.label
landing.footer.resourcesColumn.*.label
landing.footer.companyColumn.*.label
landing.footer.brandDescription
landing.footer.copyright
```

**Total:** ~120 keys across both locales.

**Approach for data arrays (pillars, architecture nodes, metrics, etc.):** Two valid strategies:
- **Option A:** Localize the data arrays directly in the catalog (each item is a key). Components consume via `t()`.
- **Option B:** Keep data arrays as code, localize individual fields.

**Recommendation:** Option A — put the full arrays in the catalog. This keeps all translatable content in one place and follows the existing pattern where business data lives in the localization system.

### Catalog update pattern

Both `fa.ts` and `en.ts` get the same `landing: { ... }` block added at the top level. The `MessageKey` type from `messageTypes.ts` automatically derives the dot-notation keys from the catalog shape, so no manual type updates are needed.

---

## Phase 5: Root Page Integration

**Modified file:** `src/Hosts/Web/src/app/page.tsx`

Replace the current content with a client-side gate:

```tsx
"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "./routes";
import { LandingPage } from "@/features/landing/landing-page";

export default function RootPage() {
  const router = useRouter();
  const { isAuthenticated, isLoading } = useAuthSession();

  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      router.replace(Routes.Home);
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return (
      <main className="grid min-h-screen place-items-center">
        <p className="text-muted">Loading...</p>
      </main>
    );
  }

  if (isAuthenticated) {
    return (
      <main className="grid min-h-screen place-items-center">
        <p className="text-muted">Redirecting...</p>
      </main>
    );
  }

  return <LandingPage />;
}
```

The `SessionRedirect` component and `AuthenticatedRedirectBoundary` remain untouched — they're used by other routes.

### Root layout font integration

**Modified file:** `src/Hosts/Web/src/app/layout.tsx`

Add font imports for Inter, Space Grotesk, and JetBrains Mono. Use `next/font/google`:

```tsx
import { Inter, Space_Grotesk, JetBrains_Mono } from "next/font/google";

const inter = Inter({ subsets: ["latin"], variable: "--font-sans" });
const spaceGrotesk = Space_Grotesk({ subsets: ["latin"], variable: "--font-display" });
const jetbrainsMono = JetBrains_Mono({ subsets: ["latin"], variable: "--font-mono" });
```

Apply the font CSS variables to the `<html>` element alongside the existing `bg-background text-foreground` classes.

---

## Phase 6: Route Definitions Update

**Modified file:** `src/Hosts/Web/src/app/routes.ts`

No changes needed. The root `/` path is already defined as `Root: "/"`. The landing page lives at this existing route.

---

## Implementation Order

| Step | Description | Files | Risk |
|---|---|---|---|
| 1 | Install `@iconify/react` | `package.json` | Low — single dependency |
| 2 | Merge CSS into `globals.css` | `globals.css` | Medium — must not break existing styles |
| 3 | Add fonts to layout | `layout.tsx` | Low |
| 4 | Create blueprint components | `components/blueprint/index.tsx` | Low — isolated, no dependencies on existing code |
| 5 | Create landing feature directory structure | All new files | Low — no existing code touched |
| 6 | Copy brand assets with Vitarish metadata | `features/landing/brand/*` | Low |
| 7 | Copy figures data | `features/landing/data/figures.ts` | Low |
| 8 | Copy icon system | `features/landing/icons/*` | Low |
| 9 | Add localization keys to fa.ts | `localization/messages/fa.ts` | Medium — ~120 keys, must maintain structure parity |
| 10 | Add localization keys to en.ts | `localization/messages/en.ts` | Medium — must match fa.ts keys exactly |
| 11 | Copy and adapt section components | All 12 section files | High — most complex step, many import changes + localization |
| 12 | Create landing page orchestrator | `features/landing/landing-page.tsx` | Low |
| 13 | Modify root page to gate on auth | `app/page.tsx` | Medium — must preserve existing redirect behavior |
| 14 | Verify build compiles | Run `pnpm build` | Validation step |
| 15 | Verify both locales render | Manual browser test | Validation step |
| 16 | Verify RTL layout for Farsi | Manual browser test | Validation step |
| 17 | Verify authenticated redirect still works | Manual browser test | Validation step |

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| CSS variable conflicts between template tokens and existing HeroUI theme | The template's `heroui-theme.css` overrides the same CSS custom properties that HeroUI uses. If the blueprint palette clashes with the existing dashboard palette, scope the blueprint theme under a `.landing` class and add it only to the landing page wrapper. |
| `@iconify/react` SSR hydration issues | The bundled gravity-ui icons are static JSON — no async fetching. SSR should work. If hydration mismatches occur, wrap the Iconify component in a `next/dynamic` with `ssr: false`. |
| `motion/react` (Framer Motion) SSR | The `Reveal` component uses `whileInView` which is client-only. Since the landing page is already `"use client"`, this should work. The `useReducedMotion` hook is also client-only. |
| Large bundle size from 2353-line icons.json | This is a one-time cost. The gravity-ui set is already minimal (SVG paths). Consider tree-shaking unused icons later via a build-time script. |
| RTL layout for Farsi | The template was designed for LTR. Blueprint annotations (measurement ticks, corner marks) should still work in RTL since they use absolute positioning. Test the navbar and pricing cards for RTL text alignment. |

---

## Acceptance Criteria

1. Anonymous visitors to `/` see the full 11-section landing page
2. Authenticated visitors to `/` are redirected to `/home` within one render cycle
3. All text in the landing page uses `t()` from `useLocalizedText()`
4. Switching between `fa` and `en` locales updates all landing page text
5. Farsi renders correctly in RTL mode
6. Login button in header navigates to `/login`
7. Sign Up button is a placeholder (href="#" or future route)
8. Hash anchors (`#pillars`, `#pricing`, etc.) scroll to sections correctly
9. Dark/light theme toggle works via the existing HeroUI theme system
10. `pnpm build` succeeds without errors
11. Existing dashboard routes (`/home`, `/owner/*`, `/user/*`, etc.) are unaffected
